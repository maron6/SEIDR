using System;
using SEIDR.DataBase;
using SEIDR.META;
using System.ComponentModel;

namespace SEIDR.JobBase
{
    
    public class JobProfile//: DatabaseObject<JobProfile>
    {        
        public int? JobProfileID { get; private set; }
        public string Description { get; set; }
        public string Creator { get; set; }
        public DateTime DC { get; set; } = DateTime.Now;
        public DateTime LU { get; set; } = DateTime.Now;
        public DateTime? DD { get; set; } = null;
        public string RegistrationFolder { get; set; }
        public string FileDateMask { get; set; }
        public string FileFilter { get; set; }

        public int UserKey { get; set; }
        public string UserKey1 { get; set; }
        public string UserKey2 { get; set; }

        /// <summary>
        /// Allows specifying that a JobProfile needs to run a specific thread number. Can be overridden at execution level.
        /// </summary>
        public byte? RequiredThreadID { get; private set; }
        /// <summary>
        /// For creating JobExecutions without folder monitoring
        /// </summary>
        public int? ScheduleID { get; set; }
    }
    public class JobExecution //: DatabaseObject<JobExecution>
    {
        public JobExecution() { }        
        public long? JobExecutionID { get; private set; }
        
        public int JobProfileID { get; private set; }

        public int JobProfile_JobID { get; private set; }
        public int StepNumber { get; private set; }
        public int JobID { get; private set; }


        public int UserKey { get; private set; }
        public string UserKey1 { get; private set; }
        public string UserKey2 { get; set; }

        public DateTime ProcessingDate { get; private set; }
        public string ExecutionStatusCode { get; private set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string FileHash { get; set; }
        public string FileName => System.IO.Path.GetFileName(FilePath);

       
    }
    public class ExecutionStatus //: DatabaseObject<ExecutionStatus>
    {                
        public string ExecutionStatusCode { get; set; }
        /// <summary>
        /// Indicates if the execution is complete. Default: false
        /// </summary>
        public bool IsComplete { get; set; } = false;
        /// <summary>
        /// Indicates if the execution is at an error status. Default: false.
        /// </summary>
        public bool IsError { get; set; } = false;
        /// <summary>
        /// Used to populate ExecutionStatus table when first added. Should be descriptive for users.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Specify for non default to prevent overlap. If not set, the Namespace from JobMetaData will be used.
        /// </summary>
        public string NameSpace { get; set; }
        /// <summary>
        /// Status allows being picked for queueing.
        /// </summary>
        public bool Queueable => !IsComplete && !IsError;
        public static ExecutionStatus REQUEUE
            => new ExecutionStatus
            {
                ExecutionStatusCode = "RQ",
                IsComplete = false,
                IsError = false,
                Description = "Requeue the job without updating the status. Was not ready to run.",
                NameSpace ="SEIDR"
            };        
    }
}
