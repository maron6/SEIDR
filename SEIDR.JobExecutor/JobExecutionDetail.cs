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

        public const string REGISTERED = "R";
        public const string SCHEDULED = "S";
        public const string COMPLETE = "C";
        public const string CANCELLED = "CX";
        public const string FAILURE = "F";
        public const string STEP_COMPLETE = "SC";
        public bool CanStart => DelayStart == null || DelayStart < DateTime.Now;

        public bool Complete { get; set; }
    }
}
