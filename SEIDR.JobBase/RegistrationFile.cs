using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR;
using SEIDR.Doc;
using SEIDR.DataBase;

namespace SEIDR.JobBase
{
    public class RegistrationFile
    {
        const string REGISTER_SPROC = "SEIDR.usp_Job_RegisterFile";
        public int JobProfileID { get; private set; }
        public string FilePath { get; private set; }
        public string FileName => System.IO.Path.GetFileName(FilePath);
        public long FileSize { get; private set; }
        DateTime _FileDate;
        public DateTime FileDate => _FileDate;
        public RegistrationFile(JobProfile profile, System.IO.FileInfo file)
        {
            JobProfileID = profile.JobProfileID.Value;
            FilePath = file.FullName;
            FileSize = file.Length;
            _FileDate = file.CreationTime.Date;
            FileHash = file.GetFileHash();
            file.FullName.ParseDateRegex(profile.FileDateMask, ref _FileDate);            
        }
        /// <summary>
        /// Registers this file as a new JobExecution under it's JobProfile
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="SuccessFilePath"></param>
        /// <param name="FailureFilePath"></param>
        /// <returns></returns>
        public JobExecution Register(DatabaseManager manager, string SuccessFilePath, string FailureFilePath)
        {            
            using (var help = manager.GetBasicHelper(this, includeConnection: true))
            {                
                help.QualifiedProcedure = REGISTER_SPROC;
                help[nameof(FilePath)] = SuccessFilePath;
                help.RetryOnDeadlock = true;

                help.BeginTran();
                var ds = manager.Execute(REGISTER_SPROC, this);
                var job = ds.GetFirstRowOrNull().ToContentRecord<JobExecution>();
                bool Success = job == null ? help.ReturnValue == 0 : true;
                try
                {
                    if (Success)
                    {
                        System.IO.File.Move(FilePath, SuccessFilePath); //Note: success always true if job != null
                    }
                    else
                    {
                        System.IO.File.Move(FilePath, FailureFilePath);
                    }
                    help.CommitTran();
                }
                catch
                {
                    help.RollbackTran();
                }
                return job;
            }            
        }
        public string FileHash { get; private set; }
        public static string CheckFileHash(string FilePath) => FilePath.GetFileHash();
        public static string CheckFileHash(System.IO.FileInfo file) => file.GetFileHash();
    }
}
