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
        public string FileHash { get; private set; }
        public long FileSize { get; private set; }
        DateTime _FileDate;
        public DateTime FileDate => _FileDate;
        int _StepNumber = 1;
        public int StepNumber
        {
            get { return _StepNumber; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(StepNumber), "Value must be > 0");
                _StepNumber = value;
            }
        }
        public RegistrationFile(JobProfile profile, System.IO.FileInfo file)
        {
            JobProfileID = profile.JobProfileID.Value;
            FilePath = file.FullName;
            FileSize = file.Length;
            _FileDate = file.CreationTime.Date;
            FileHash = file.GetFileHash();
            file.FullName.ParseDateRegex(profile.FileDateMask, ref _FileDate);
            if (!CheckSQLDateValid(_FileDate))
                _FileDate = file.CreationTime.Date;
        }
        public JobExecution CopyRegister(DatabaseManager manager, string SuccessFilePath, string FailureFilePath)
            => Register(manager, SuccessFilePath, FailureFilePath, true);
        /// <summary>
        /// Registers this file as a new JobExecution under it's JobProfile
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="SuccessFilePath"></param>
        /// <param name="FailureFilePath"></param>
        /// <returns></returns>
        public JobExecution Register(DatabaseManager manager, string SuccessFilePath, string FailureFilePath)
            => Register(manager, SuccessFilePath, FailureFilePath, false);
        private JobExecution Register(DatabaseManager manager, 
            string SuccessFilePath, string FailureFilePath, bool copyMode)
            => RegisterDataRow(manager, SuccessFilePath, FailureFilePath, copyMode).ToContentRecord<JobExecution>();        
        public System.Data.DataRow RegisterDataRowCopy(DatabaseManager manager, string sucessFilePath, string FailureFilePath)
            => RegisterDataRow(manager, SuccessFilePath: sucessFilePath, FailureFilePath: FailureFilePath, copyMode: true);
        public System.Data.DataRow RegisterDataRow(DatabaseManager manager, string sucessFilePath, string FailureFilePath)
            => RegisterDataRow(manager, SuccessFilePath: sucessFilePath, FailureFilePath: FailureFilePath, copyMode: false);
        private System.Data.DataRow RegisterDataRow(DatabaseManager manager,
            string SuccessFilePath, string FailureFilePath, bool copyMode)
        {
            using (var help = manager.GetBasicHelper(this, includeConnection: true))
            {
                help.QualifiedProcedure = REGISTER_SPROC;
                if (!string.IsNullOrWhiteSpace(SuccessFilePath))
                    help[nameof(FilePath)] = SuccessFilePath;
                help.RetryOnDeadlock = true;

                help.BeginTran();
                var ds = manager.Execute(REGISTER_SPROC, this);
                var job = ds.GetFirstRowOrNull();
                bool Success = job == null ? help.ReturnValue == 0 : true;
                try
                {
                    if (Success)
                    {
                        if (SuccessFilePath != FilePath && !string.IsNullOrWhiteSpace(SuccessFilePath))
                        {
                            if (copyMode)
                                System.IO.File.Copy(FilePath, SuccessFilePath, true);
                            else
                                System.IO.File.Move(FilePath, SuccessFilePath); //Note: success always true if job != null
                        }
                    }
                    else
                    {
                        if (FailureFilePath != FilePath && !string.IsNullOrWhiteSpace(FailureFilePath))
                        {
                            if (copyMode)
                                System.IO.File.Copy(FilePath, FailureFilePath, true);
                            else
                                System.IO.File.Move(FilePath, FailureFilePath);
                        }
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

        public bool CheckSQLDateValid(DateTime check)
        {
            if (check.CompareTo(new DateTime(1770, 1, 1, 0, 0, 0, 0)) <= 0 || check.CompareTo(new DateTime(9000, 12, 30)) >= 0)
                return false;
            return true;
        }
        public static string CheckFileHash(string FilePath) => FilePath.GetFileHash();
        public static string CheckFileHash(System.IO.FileInfo file) => file.GetFileHash();
    }
}
