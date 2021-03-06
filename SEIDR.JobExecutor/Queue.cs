﻿using System;
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
        List<JobProfile> work = new List<JobProfile>();
        public Queue( JobExecutorService caller, DatabaseManager db)
            : base(db, caller, ExecutorType.Maintenance)
        {
            
            map = new
            {
                ThreadID,
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
        protected override void CheckWorkLoad()
        {
            if (Workload == 0)
            {
                using (var h = _Manager.GetBasicHelper(map))
                {
                    h.QualifiedProcedure = GRAB_WORK;
                    h.RetryOnDeadlock = true;
                    h.ExpectedReturnValue = 0;
                    work = _Manager.Execute(h).ToContentList<JobProfile>();
                }
            }
        }
        protected override void Work()
        {
            var profile = work[0];
            work.RemoveAt(0);
            bool invalid = false;
            DirectoryInfo di = new DirectoryInfo(profile.RegistrationFolder);
            if (!di.Root.Exists)
            {
                string root = di.Root.Name;
                if (root.Like("%:%") && !root.Like(@"\\%"))
                {
                    invalid = true;
                    //Should be a drive, on the local machine.
                }
                else
                {
                    LogInfo("Unable to access network path:" + profile.RegistrationFolder, true);
                    return; //Root doesn't exist, but it's (probably) a UNC path. May just be connection issues. Skip
                }
            }
            
            if (invalid || !di.Exists)
            {
                if (!invalid)
                {
                    try
                    {
                        di.Create();
                        LogInfo("Created directory: " + profile.RegistrationFolder);
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
            if (!string.IsNullOrWhiteSpace(profile.RegistrationDestinationFolder))
                Registered = profile.RegistrationDestinationFolder;
            string Rejected = Path.Combine(profile.RegistrationFolder, "_Rejected"); //Hash in use for profile or something maybe?
            string Duplicate = Path.Combine(profile.RegistrationFolder, "_Duplicate"); //Name match
            CheckFolders(Registered, Rejected, Duplicate);

            var fileList = di.GetFiles(profile.FileFilter);
            List<RegistrationFile> regList = new List<RegistrationFile>();            
            fileList.ForEach(fi => regList.Add(new RegistrationFile(profile, fi)));            
            regList.OrderBy(reg => reg.FileDate).ForEach((reg) => ProcessRegistrationFile(reg, Registered, Duplicate, Rejected, profile));
        }
        private void ProcessRegistrationFile(RegistrationFile reg, string Registered, string Duplicate, string Rejected, JobProfile profile)
            {
                string success = Path.Combine(Registered, reg.FileName);
                if (File.Exists(success))
                {
                    var dupe = Path.Combine(Duplicate, reg.FileName + DateTime.Now.ToString("_yyyyMMdd_hhss"));
                    File.Move(reg.FilePath, dupe);
                    LogInfo($"Job Profile {profile.JobProfileID}, '{reg.FileName}' - moved to duplicate (" + Duplicate + ").", true);
                    return;                     
                }
                string fail = Path.Combine(Rejected, reg.FileName);
                LogInfo($"Job Profile {profile.JobProfileID}, '{reg.FileName}' - Attempt Register", true);
                var j = reg.RegisterDataRow(_Manager, success, fail).ToContentRecord<JobExecutionDetail>();
                if (j != null)
                {
                    CallerService.QueueExecution(j);
                    LogInfo($"Job Profile {profile.JobProfileID}, '{reg.FileName}' - Queued for execution. JobExecutionID: {j.JobExecutionID}.", true);
                }
        }
    }
}
