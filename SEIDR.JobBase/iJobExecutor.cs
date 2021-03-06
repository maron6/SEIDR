﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.ComponentModel.Composition;

namespace SEIDR.JobBase
{    
    public interface IJobExecutor
    {
        /// <summary>
        /// A readonly copy of the database connection being used by the Executor
        /// </summary>
        DataBase.DatabaseConnection connection { get; }
        /// <summary>
        /// Profile of current execution
        /// </summary>
        JobProfile job { get; }
        /// <summary>
        /// The actual number ID of the executing thread, used by the Service
        /// </summary>
        int ThreadID { get; }
        /// <summary>
        /// Name of the thread, for logging purposes.
        /// </summary>
        string ThreadName { get; }
        /// <summary>
        /// If called, will move the current jobExecution to the end of the queue with a mark to retry in at least <paramref name="delayMinutes"/> minutes.
        /// <para>The execution should return false. Status should either be null or REQUEUE</para>
        /// </summary>
        /// <param name="delayMinutes"></param>
        void Requeue(int delayMinutes);
        /// <summary>
        /// If called, will cause the executor thread to change its status (JOB_REQUESTED_SLEEP) and then sleep. 
        /// <para>After sleep ends, status is reverted and returns control to the job.
        /// </para>
        /// </summary>
        /// <param name="sleepSeconds">Number of seconds to cause thread to sleep. Included in thread status XML doc and other logs</param>
        /// <param name="logReason">Included in logs, and the thread status XML doc.</param>
        /// <returns></returns>
        void Wait(int sleepSeconds, string logReason);
        void LogError(string message, Exception ex);
        void LogInfo(string message);
        /// <summary>
        /// Call when at a point where the job can stop if requested.
        /// <para>It should only be called when the job is at a point where it's safe to return when the method returns true.</para>
        /// </summary>
        /// <returns>True if the job has been requested to stop.<para>
        /// If this returns true, the job should return false if it is able to stop.</para></returns>
        bool checkAcknowledgeCancel();
        /// <summary>
        /// Sets the JobExecution status to an intermediary working status.If the job has to stop or restart or retry, this can indicate where to pick up again
        /// </summary>
        /// <param name="workingStatus"></param>
        void UpdateStatus(ExecutionStatus workingStatus);
    }
}
