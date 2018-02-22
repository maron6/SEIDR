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
        /// <summary>
        /// Limit 128 characters. Should be able to keep your job unique, as well as isolate your statuses and some other special handling.
        /// Possible example: Need specific handling for different vendors with the same file. You might use the same JobName for sorting purposes, but keep them unique via NameSpace
        /// </summary>
        string NameSpace { get; }
        /// <summary>
        /// If a Job cannot share the same thread as other jobs, it should share a name. 
        /// <para>When the job is picked up, the Executor thread will take on the name from the current job if this is specified.</para>
        /// <para>If a job is queued and then ready while another thread is already running with this threadName, the jobExecution will either be held or moved to the other thread's queue.</para>
        /// </summary>
        [DefaultValue(null)]
        string ThreadName { get; }
        /// <summary>
        /// Indicates if the job needs to be run on a single thread. <para>
        /// E.g., it needs to store a complex state outside of local variables in the Execute method.</para>
        /// </summary>
        [DefaultValue(false)]
        bool SingleThreaded { get; }
        /// <summary>
        /// The job is able to call <see cref="IJobExecutor.checkAcknowledgeCancel"/> and stops if requested.
        /// </summary>
        [DefaultValue(false)]
        bool SafeCancel { get; }
        /// <summary>
        /// Indicates whether the job needs to rerun the thread check
        /// </summary>
        [DefaultValue(false)]
        bool RerunThreadCheck { get; }
    }
    public interface IJob
    {
        /// <summary>
        /// Check if the job is okay to run for the specified thread.
        /// </summary>
        /// <param name="jobCheck">The job to check. E.g., if the job has to determine thread based on user keys or the profile in order to avoid stepping on other processes.</param>
        /// <param name="passedThreadID"></param>
        /// <param name="NewThreadID"></param>
        /// <returns>True if the passedThreadID is okay to use based on any further configuration from the job.<para>
        /// If false, newThreadID may be used to move the job to another thread.
        /// </para></returns>
        bool CheckThread(JobExecution jobCheck, int passedThreadID, out int NewThreadID);
        /// <summary>
        /// Called by the jobExecutor.
        /// </summary>
        /// <param name="execution"></param>        
        /// <param name="status">Optional status set, to allow a more detailed status. If the status does not have a namespace set, the NameSpace from the job meta data will be used.</param>        
        /// <returns>True for success, false for failure.</returns>
        bool Execute(IJobExecutor jobExecutor, JobExecution execution, ref ExecutionStatus status);

    }
}
