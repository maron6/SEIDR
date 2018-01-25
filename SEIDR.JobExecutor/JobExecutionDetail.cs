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
        public int? RequiredThreadID { get; set; }
        /// <summary>
        /// Determined in view by database based on: 
        /// <para>* the amount of time since it was last queued/worked on,</para><para> 
        /// * the Profile's Priority, the Execution's priority,</para><para> 
        /// * the Procesing age (older processingDates get a slight boost to priority, future processing dates get a slightly lowered priority) 
        /// </para>
        /// </summary>
        public int WorkPriority { get; private set; } = 1;

        public int RetryCount { get; private set; } = 0;
        public bool ForceSequence { get; private set; }
        /// <summary>
        /// Used for requeueing.
        /// </summary>
        public DateTime? DelayStart;

        public bool CanStart => DelayStart == null || DelayStart < DateTime.Now;


        public bool CanRetry { get; set; }
        public int RetryDelay { get; set; }


        public bool Complete { get; set; } = false;

        public bool ThreadChecked { get; set; } = false;
        public int? ExecutionTimeSeconds { get; set; } = null;      
        DateTime? ExecutionStart = null;
        public void Start() => ExecutionStart = DateTime.Now;
        public void Finish()
        {
            ExecutionTimeSeconds = (ExecutionStart.HasValue ? (int?)(DateTime.Now - ExecutionStart.Value).TotalSeconds : null);
            ExecutionStart = null;
        }
        public readonly DateTime DetailCreated = DateTime.Now;
    }
}
