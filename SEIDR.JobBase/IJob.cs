using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.JobBase
{
    public interface IJobMetaData
    {
        string JobName { get; }
        [DefaultValue(null)]
        string Description { get; }
        string NameSpace { get; }
        /// <summary>
        /// If a Job cannot share the same thread as other jobs, it should share a name. 
        /// <para>When the job is picked up, the Executor thread will take on the name from the current job if this is specified.</para>
        /// <para>If a job is queued and then ready while another thread is already running with this threadName, the jobExecution will either be held or moved to the other thread's queue.</para>
        /// </summary>
        [DefaultValue(null)]
        string ThreadName { get; }
        [DefaultValue(false)]
        bool SingleThreaded { get; }
        /// <summary>
        /// The job is able to call <see cref="IJobExecutor.checkAcknowledgeCancel"/> and stop if requested.
        /// </summary>
        [DefaultValue(false)]
        bool SafeCancel { get; }

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
}
