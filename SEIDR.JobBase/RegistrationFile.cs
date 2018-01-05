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
            file.FullName.ParseDateRegex(profile.FileDateMask, ref _FileDate);            
        }
        public JobExecution Register(DatabaseManager manager)
        {            
            var ds = manager.Execute(REGISTER_SPROC, this);
            return ds.GetFirstRowOrNull().ToContentRecord<JobExecution>();
        }
    }
}
