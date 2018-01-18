using System;
using SEIDR.DataBase;
using SEIDR.META;
using System.ComponentModel;

namespace SEIDR.JobBase
{
    public interface IJobMetaData
    {        
        string JobName { get; }
        [DefaultValue(null)]
        string Description { get; }        
        string NameSpace { get;  }
        /// <summary>
        /// If a Job cannot share the same thread as other jobs, it should share a name. 
        /// <para>When the job is picked up, the Executor thread will take on the name from the current job if this is specified.</para>
        /// <para>If a job is queued and then ready while another thread is already running with this threadName, the jobExecution will either be held or moved to the other thread's queue.</para>
        /// </summary>
        [DefaultValue(null)]
        string ThreadName { get; }
        [DefaultValue(false)]
        bool SingleThreaded { get; }
    }
    public interface IJob
    {        
        /// <summary>
        /// Called by the jobExecutor.
        /// </summary>
        /// <param name="execution"></param>        
        /// <param name="status">Optional status set, to allow a more detailed status.</param>        
        /// <returns>True for success, false for failure.</returns>
        bool Execute(IJobExecutor jobExecutor, JobExecution execution, ref ExecutionStatus status);

    }
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

        public string JobName { get; private set; }
        public string JobNameSpace { get; private set; }
        public string JobThreadName { get; private set; }
        public bool JobSingleThreaded { get; private set; }
        /// <summary>
        /// Total job completion notification.
        /// </summary>
        public string SuccessNotificationMail { get; private set; }
        /// <summary>
        /// Step failure notification.
        /// </summary>
        public string FailureNotificationMail { get; private set; }
        /// <summary>
        /// Allows specifying that an Execution needs to run a specific thread number. 
        /// </summary>
        public byte? RequiredThreadID { get; private set; }

        public int UserKey { get; private set; }
        public string UserKey1 { get; private set; }
        public string UserKey2 { get; set; }

        public DateTime ProcessingDate { get; private set; }
        public string ExecutionStatusCode { get; private set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string FileHash { get; set; }
        public string FileName => System.IO.Path.GetFileName(FilePath);

        public int RetryCount { get; private set; } = 0;
        public bool ForceSequence { get; private set; }
        /// <summary>
        /// Used for requeueing.
        /// </summary>
        public DateTime? DelayStart;
        public const string REGISTERED = "R";
        public const string SCHEDULED = "S";
        public const string WORKING = "W";
        public const string COMPLETE = "C";
        public const string CANCELLED = "CX";
        public const string FAILURE = "F";
        public const string STEP_COMPLETE = "SC";
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
        /// Indicates that the status should not get picked up for queueing.
        /// </summary>
        public bool IsWorking { get; set; } = false;
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
        public bool Queueable => !IsComplete && !IsError && !IsWorking;
        public static ExecutionStatus REQUEUE
            => new ExecutionStatus
            {
                ExecutionStatusCode = "RQ",
                IsComplete = false,
                IsError = false,
                IsWorking = true,
                Description = "Requeue the job without updating the status. Was not ready to run.",
                NameSpace ="SEIDR"
            };        
    }
}
