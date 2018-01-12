using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.OperationServiceModels;
using SEIDR.DataBase;
using SEIDR.JobBase;

namespace SEIDR.JobExecutor
{
    public class Queue : Executor
    {
        const string GRAB_WORK = "SEIDR.usp_JobProfile_sl_FileWatch";
        const string INVALID = "SEIDR.usp_JobProfile_u_InvalidPath";
        object map;        
        List<JobBase.JobProfile> work;
        public Queue(int ID, DatabaseManager db, JobExecutorService caller)
            : base(ID, db, caller, ExecutorType.Maintenance)
        {
            
            object map = new
            {
                ThreadID = ID,
                ThreadCount = caller.QueueThreadCount
            };            
        }

        public override int Workload => work.Count;

        protected override string HandleAbort()
        {
            work.Clear();
            return null;
        }

        static void CheckFolders(params string[] directories)
        {
            foreach (var f in directories)
            {                
                Directory.CreateDirectory(f); //Also checks if it already exists first.
            }
        }
        protected override void Work()
        {
            if(Workload == 0)
            {
                using (var h = _Manager.GetBasicHelper(map))
                {
                    h.QualifiedProcedure = GRAB_WORK;
                    h.RetryOnDeadlock = true;
                    h.ExpectedReturnValue = 0;
                    work = _Manager.Execute(h).ToContentList<JobBase.JobProfile>();
                }                    
                if(Workload == 0)
                {
                    Wait(sleepSeconds: 2 * FAILURE_SLEEPTIME, logReason: "No Queue work found");
                    return;
                }
            }
            var profile = work[0];
            work.RemoveAt(0);
            bool invalid = false;
            DirectoryInfo di = new DirectoryInfo(profile.RegistrationFolder);
            if (!di.Root.Exists)
            {
                string root = di.Root.Name;
                if(root.Like("%:%") && !root.Like(@"\\%"))
                {
                    invalid = true;
                    //Should be a drive. on the local machine.
                }
                else
                    return; //Root doesn't exist, but it's a UNC path. May just be connection issues.
            }
            
            if (invalid || !di.Exists)
            {
                if (!invalid)
                {
                    try
                    {
                        di.Create();
                    }
                    catch
                    {
                        invalid = true;
                    }
                }
                if (invalid)
                {
                    using (var h = _Manager.GetBasicHelper(map))
                    {
                        h.AddKey("@JobProfileID", profile.JobProfileID);
                        h.QualifiedProcedure = INVALID;
                        h.RetryOnDeadlock = true;
                        h.ExpectedReturnValue = 0;
                        _Manager.ExecuteNonQuery(h);
                    }
                    LogInfo("Invalid Directory Found: " + profile.RegistrationFolder);
                }
                return; //Profile didn't exist or marked as invalid. No work, move on.
            }
            string Registered = Path.Combine(profile.RegistrationFolder, "_Registered"); //successful registrartion
            string Rejected = Path.Combine(profile.RegistrationFolder, "_Rejected"); //Hash in use for profile or something maybe?
            string Duplicate = Path.Combine(profile.RegistrationFolder, "_Duplicate"); //Name match
            CheckFolders(Registered, Rejected, Duplicate);

            var fileList = di.GetFiles(profile.FileFilter);
            List<RegistrationFile> regList = new List<RegistrationFile>();            
            fileList.ForEach(fi => regList.Add(new RegistrationFile(profile, fi)));            
            regList.OrderBy(reg=> reg.FileDate).ForEach((reg) =>
            {
                string success = Path.Combine(Registered, reg.FileName);
                if (File.Exists(success))
                {
                    var dupe = Path.Combine(Duplicate, reg.FileName + DateTime.Now.ToString("_yyyyMMdd_hhss"));
                    File.Move(reg.FilePath, dupe);
                    return;                     
                }
                string fail = Path.Combine(Rejected, reg.FileName);
                var j = reg.Register(_Manager, success, fail);
                if(j!= null)
                    CallerService.QueueExecution(j);                
            });

        }
    }
    public class OQueue:Operator
    {
        const string CHECK_SCHEDULE = "SEIDR.usp_Profile_CheckSchedules";
        const string GRAB_WORK = "SEIDR.usp_BatchProfile_sl_FileWatch";
        const string BULK_REGISTER = "SEIDR.usp_Batch_BulkRegister";
        object map;
        public OQueue(IOperatorManager owner, byte ID) 
            : base(owner,OperatorType.Maintenance, ID, "FILE_QUEUE_"+ ID)
        {
            map = new
            {
                ThreadID = ID,
                BatchSize = owner.BatchSize
            };
        }        
        public override void HandleAbort()
        {
            return;
        }

        public override bool CheckWork()
        {
            //Create batches based on schedule, not files. Run before grabbing files
            DbManager.ExecuteNonQuery(CHECK_SCHEDULE);
            using(var h = DbManager.GetBasicHelper())
            {
                h.QualifiedProcedure = GRAB_WORK;
                h.ParameterMap = map;
                h.RetryOnDeadlock = true;                
                ProfileQueue = DbManager.SelectList<BatchProfile>(h);
            }                        
            return ProfileQueue.Count() > 0;
        }
        IEnumerable<BatchProfile> ProfileQueue;
        void ProcessProfile(BatchProfile p)
        {
            DirectoryInfo di = new DirectoryInfo(p.InputFolder);
            if (!di.Exists)
            {
                try
                {
                    di.Create();
                }
                catch
                {
                    DbManager.ExecuteTextNonQuery(@"
    UPDATE SEIDR.BatchProfile
	SET InputFolder = '*INVALID*' + InputFolder
	WHERE batchProfileID = " + p.BatchProfileID.Value, true);
                    while(!Manager.LogError(this, "Invalid directory found: " + p.InputFolder))
                    {
                        Sleep(true);
                    }
                }
                return; //Profile didn't exist or marked as invalid. No work, move on.
            }
            string Registered = Path.Combine(p.InputFolder, "_Registered");
            string Rejected = Path.Combine(p.InputFolder, "_Rejected");
            string Duplicate = Path.Combine(p.InputFolder, "_Duplicate");
            CheckFolders(Registered, Rejected, Duplicate);


            var fileList = di.GetFiles(p.FileMask);
            List<Batch_File> files = new List<Batch_File>();
            foreach (FileInfo f in fileList)
            {
                try
                {
                    Batch_File bf = Batch_File.FromFileInfo(f,
                        Manager.ParseNameDate(f, p.InputFileDateFormat, p.DayOffset)
                        );
                    bf.FilePath = Path.Combine(Registered, bf.FileName);
                    if (bf.Exists)
                    {
                        Manager.LogError(this, "Duplicate file found for BatchProfileID " + p.BatchProfileID
                             + " - '" + bf.FileName + "' Moving to Duplicate folder: '" + Duplicate + "'");

                        string dest = Path.Combine(Duplicate, bf.FileName);
                        if (File.Exists(dest))
                            dest += DateTime.Now.ToString("_yyyyMMdd_hhmmss");                        
                        File.Move(bf.OriginalFilePath, dest);
                        continue; //Maybe don't continue? Make sure it was registered
                                  //rejected are not going to be moved in this case, so should be fine I think...
                    }
                    File.Move(bf.OriginalFilePath, bf.FilePath);
                    files.Add(bf);
                }
                catch(Exception ex)
                {
                    while(!Manager.LogError(this, "BatchProfileID: " + p.BatchProfileID
                            + "Error moving file pre-registration: '" + f.FullName + "'"
                            + Environment.NewLine
                            + "Exception:" + ex.Message)) { Sleep(true); }
                    continue;
                }
            }
            IEnumerable<Batch_File> rejects = null;
            IEnumerable<Batch> bList = null;
            try
            {
                bList = Batch.Register(DbManager, p.BatchProfileID.Value, files, out rejects);
            }            
            catch(Exception ex)
            {
                System.Data.SqlClient.SqlException ex2 = ex as System.Data.SqlClient.SqlException;
                if (ex2 != null && ex2.ErrorCode == -2)
                {
                    if(DbManager.TimeOut < 300)
                        DbManager.TimeOut += 5;
                    Manager.LogError(this, "Timeout while registering files. Increasing by 5 seconds.. (Limit 300). Current Value:" + DbManager.TimeOut);
                }
                else
                    Manager.LogError(this, "Unable to register files - Moving back to input. Exception: " + ex.Message);
                bList = null;
            }
            if(bList == null)
            {
                files.ForEach(f =>
                {
                    try
                    {
                        File.Move(f.FilePath, f.OriginalFilePath); //Move back to original path.
                    }
                    catch(Exception ex)
                    {
                        Manager.LogError(this, "Could not move File back to Input after Registration Error: '"
                            + f.FileName + "'. Destination(InputFolder): '" + p.InputFolder + "'."
                            + Environment.NewLine + "Current Location:'" + f.Directory + "'"
                            + Environment.NewLine + "Exception: " + ex.Message);
                    }
                });
                return; //No batches created. Return the non duplicate files(files that were already in _Registered)
            }
            MoveRejects(rejects, Rejected, p.BatchProfileID.Value);
            if (!bList.HasMinimumCount(1))
                return;

            Manager.DistributeBatches(bList);
        }
        public override void DoWork()
        {            
            foreach(var p in ProfileQueue)
            {
                if (!p.BatchProfileID.HasValue)
                    continue; //Shouldn't be grabbed
                ProcessProfile(p);                
            }
        }
        static void CheckFolders(params string[] directories)
        {
            foreach (var f in directories)
            {
                if (!Directory.Exists(f))
                    Directory.CreateDirectory(f);
            }
        }        
        void MoveRejects(IEnumerable<Batch_File> rejects, string rejectFolder, int BatchProfileID)
        {
            string uq = DateTime.Now.ToString("_yyyyMMdd_hhmmss");
            rejects.Where(r => r.FileName != null).ForEach(r =>
            {
                string newPath = Path.Combine(rejectFolder, r.FileName);
                try
                {                                        
                    if (File.Exists(newPath))
                        newPath += uq;
                    File.Move(r.FilePath, newPath);
                }
                catch(IOException ex)
                {
                    // If we get an IO Exception, don't really care that much 
                    // since it should be rejected again later on
                    // Move on. But do log it.
                    string Message = "BatchProfile: " + BatchProfileID
                    + " Destination Reject Folder: '" + rejectFolder + "'" + Environment.NewLine;
                    Manager.LogError(this, Message + "Exception: '" + ex.Message + "'");

                    Sleep("Error moving rejected file, BatchProfileID:" + BatchProfileID, true);
                    try
                    {
                        //Try again after sleep                        
                        File.Move(r.FilePath, newPath);
                    }
                    catch { }
                }
            });
        }
        
    }
}
