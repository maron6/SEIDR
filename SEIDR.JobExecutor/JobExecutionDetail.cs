using SEIDR.JobBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.JobExecutor
{
    public class JobExecutionDetail: JobExecution
    {
        public JobProfile ExecutionJobProfile;
        /// <summary>
        /// Used with JobNameSpace to find the IJob
        /// </summary>
        public string JobName { get; private set; }
        public string JobNameSpace { get; private set; }
        /// <summary>
        /// Used for single threaded logic
        /// </summary>
        public string JobThreadName { get; private set; }
        /// <summary>
        /// Determine if we need to check for other Executor threads running the same job
        /// </summary>
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
        public int? RequiredThreadID { get; set; }
        /// <summary>
        /// Work Queue sorting. Determined in view by database based on: 
        /// <para>* the amount of time since it was last queued/worked on,</para><para> 
        /// * the Profile's Priority, the Execution's priority,</para><para> 
        /// * the Procesing age (older processingDates get a slight boost to priority, future processing dates get a slightly lowered priority) 
        /// </para></summary>
        public int WorkPriority { get; private set; } = 1;

        public int RetryCount { get; private set; } = 0;
        /// <summary>
        /// Ignore schedule for execution sequence
        /// </summary>
        public bool ForceSequence { get; private set; }
        /// <summary>
        /// Used for requeueing.
        /// </summary>
        public DateTime? DelayStart;

        /// <summary>
        /// Computed based on DelayStart
        /// </summary>
        public bool CanStart
        {
            get
            {
                if (DelayStart == null)
                    return true;
                if (DelayStart < DateTime.Now)
                {
                    DelayStart = null;
                    return true;
                }
                return false;
            }
        }


        /// <summary>
        /// Based on settings in Database - requeue with delay if Job returns false
        /// </summary>
        public bool CanRetry { get; set; }
        /// <summary>
        /// How long to wait before a JobExecution can be retried after failure
        /// </summary>
        public int RetryDelay { get; set; }


        public bool Complete { get; set; } = false;

        public bool ThreadChecked { get; set; } = false;
        /// <summary>
        /// Performance monitoring
        /// </summary>
        public int? ExecutionTimeSeconds { get; set; } = null;
        DateTime? ExecutionStart = null;
        public void Start() => ExecutionStart = DateTime.Now;
        public void Finish()
        {
            ExecutionTimeSeconds = (ExecutionStart.HasValue ? (int?)(DateTime.Now - ExecutionStart.Value).TotalSeconds : null);
            ExecutionStart = null;
        }
        /// <summary>
        /// The time that the ExecutionDetail object was created (data pulled from DB)
        /// </summary>
        public readonly DateTime DetailCreated = DateTime.Now;
    }
}
