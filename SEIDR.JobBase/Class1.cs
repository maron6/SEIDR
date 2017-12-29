using System;
using SEIDR.DataBase;
using SEIDR.META;

namespace SEIDR.JobBase
{
    public class JobProfile: DatabaseObject
    {        
        public int? JobProfileID { get; private set; }
        public string Description { get; set; }
        public string Creator { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public string Folder { get; set; }

        /// <summary>
        /// For creating JobExecutions without folder monitoring
        /// </summary>
        public int? ScheduleID { get; set; }
        /// <summary>
        /// For checking if jobExecution is eligible to run.
        /// </summary>
        public int? SequenceScheduleID { get; set; }
    }
    public class JobExecution : DatabaseObject
    {
        public int JobExecutionID { get; private set; }
        public int JobProfileID { get; private set; }
        public DateTime ProcessingDate { get; set; }
        public string ExecutionStatusCode { get; set; }
        public string FilePath { get; set; }
        public string FileName => System.IO.Path.GetFileName(FilePath);
    }
    public class ExecutionStatus : DatabaseObject
    {        
        public string ExecutionStatusCode { get; set; }
        public bool IsComplete { get; set; }
        public bool IsError { get; set; }
        public bool Description { get; set; }
    }
}
