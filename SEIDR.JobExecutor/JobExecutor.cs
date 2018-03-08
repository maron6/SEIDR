using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.DataBase;
using SEIDR.JobBase;
using System.Threading;
using SEIDR.ThreadManaging;

namespace SEIDR.JobExecutor
{
    public sealed class JobExecutor : Executor, IJobExecutor
    {
        int BatchSize => CallerService.BatchSize;
        static JobLibrary Library { get; set; } = null;
        static DateTime LastLibraryCheck = new DateTime(1, 1, 1);

        volatile static List<ExecutionStatus> statusList = new List<ExecutionStatus>();
        public static void PopulateStatusList(DatabaseManager manager)
        {
            //maybe switch over to readWrite lock, to reduce overhead? 
            //Possible ToDo: Wrapper for readWrite, since it's kind of bulkier to set up and needs extra try/catches, especially if transitioning to/from a Write lock..
            using (new LockHelper(Lock.Exclusive, STATUS_TARGET)) 
            {
                statusList.Clear();
                statusList = manager.SelectList<ExecutionStatus>(Schema:"SEIDR");
            }
        }
        void CheckStatus(ExecutionStatus check)
        {            
            //lock(statusListLock)
            using(var h= new LockHelper(Lock.Shared, STATUS_TARGET))
            {
                if (statusList.Exists(s => s.NameSpace == check.NameSpace && s.ExecutionStatusCode == check.ExecutionStatusCode))
                    return;
                h.Transition(Lock.Exclusive);
                statusList.Add(check);
            }
            //format: SEIDR.usp_{0}_i
            _Manager.Insert(check);
        }
        const string LIBRARY_TARGET = nameof(SEIDR.JobExecutor) + "." + nameof(Library);
        LockManager libraryLock = new LockManager(LIBRARY_TARGET); //NOT static.
        const string STATUS_TARGET = nameof(SEIDR.JobExecutor) + "." + nameof(statusList);
        //static object statusListLock = new object();
        public static void ConfigureLibrary(string location)
        {
            if (Library == null)
                Library = new JobLibrary(location);
        }
        /// <summary>
        /// Work Queue lock
        /// </summary>
        //static object workLockObj = new object();
        const string WORK_LOCK_TARGET = nameof(SEIDR.JobExecutor) + "." + nameof(workQueue);
        
        /// <summary>
        /// Thread name lock. (Since Job imports can be single thread required, organized by Name)
        /// </summary>
        static object NameLock = new object();

        public JobExecutor( JobExecutorService caller, DatabaseManager manager)
            : base(manager, caller, ExecutorType.Job)
        {
            CheckStatus(new ExecutionStatus { ExecutionStatusCode = ExecutionStatus.COMPLETE, Description = nameof(ExecutionStatus.COMPLETE), IsComplete = true, NameSpace = "SEIDR" });
            CheckStatus(new ExecutionStatus { ExecutionStatusCode = ExecutionStatus.FAILURE, Description= nameof(ExecutionStatus.FAILURE), IsError = true, NameSpace = "SEIDR" });
            CheckStatus(new ExecutionStatus { ExecutionStatusCode = ExecutionStatus.REGISTERED, Description = nameof(ExecutionStatus.REGISTERED), NameSpace = "SEIDR" });
            CheckStatus(new ExecutionStatus { ExecutionStatusCode = ExecutionStatus.SCHEDULED, Description = nameof(ExecutionStatus.SCHEDULED), NameSpace = "SEIDR" });
            CheckStatus(new ExecutionStatus { ExecutionStatusCode = ExecutionStatus.STEP_COMPLETE, Description = nameof(ExecutionStatus.STEP_COMPLETE), NameSpace = "SEIDR" });            
            CheckStatus(new ExecutionStatus { ExecutionStatusCode = ExecutionStatus.CANCELLED, Description = nameof(ExecutionStatus.CANCELLED), NameSpace = "SEIDR" });
            CheckStatus(new ExecutionStatus { ExecutionStatusCode = ExecutionStatus.WORKING, Description = nameof(ExecutionStatus.WORKING), NameSpace = "SEIDR" });
        }
        const string SET_STATUS = "SEIDR.usp_JobExecution_SetStatus";
        const string REQUEUE = "SEIDR.usp_JobExecution_Requeue";
        const string GET_WORK = "SEIDR.usp_JobExecution_sl_Work";
        const string START_WORK = "SEIDR.usp_JobExecution_StartWork";
        
        volatile JobExecutionDetail currentExecution;
        volatile IJobMetaData currentJobMetaData;

        public volatile bool CancelRequested = false;
        volatile bool cancelSuccess = false;
        public bool checkAcknowledgeCancel()
        {
            if (!CancelRequested)
                return false;

            cancelSuccess = true;
            return true;
        }


        public DatabaseConnection connection => _Manager.CloneConnection();

        public JobProfile job => currentExecution.ExecutionJobProfile;

        volatile bool _requeue = false;
        public void Requeue(int delayMinutes)
        {
            currentExecution.DelayStart = DateTime.Now.AddMinutes(delayMinutes);
            currentExecution.ThreadChecked = false;
            _requeue = true;
            
        }
        protected override void Work()
        {
            cancelSuccess = false;
            CancelRequested = false;
            _requeue = false;
            try
            {
                currentExecution = CheckWork();
                using (var h = _Manager.GetBasicHelper())
                {
                    h.QualifiedProcedure = START_WORK;
                    h["@JobExecutionID"] = currentExecution.JobExecutionID;
                    currentExecution = _Manager.SelectSingle<JobExecutionDetail>(h);
                    if (h.ReturnValue != 0 || currentExecution == null)
                        return; //ThreadName will have been changed, but that should be okay.
                }
                currentExecution.ExecutionJobProfile = _Manager.SelectSingle<JobProfile>(currentExecution);
                
                ExecutionStatus status = null;
                bool success = false;
                using (new LockHelper(Lock.Shared, libraryLock))
                {
                    //Volatile warning as ref
#pragma warning disable 420
                    int newThread;
                    IJob job = Library.GetOperation(currentExecution.JobName,
                            currentExecution.JobNameSpace,
                            out currentJobMetaData);
#pragma warning restore 420
                    if (currentJobMetaData.RerunThreadCheck || !currentExecution.ThreadChecked)
                    {
                        if (!job.CheckThread(currentExecution, ThreadID, out newThread)
                            && (newThread % CallerService.ExecutorCount) + 1 != ThreadID)
                        {
                            //if new thread goes over ExecutorCount, it's okay in this thread. if newThread % ExecutorCount is this ID
                            using (var h = _Manager.GetBasicHelper())
                            {
                                h.QualifiedProcedure = "[SEIDR].[usp_JobExecution_UnWork]"; //Mark as not working.
                                h[nameof(currentExecution.JobExecutionID)] = currentExecution.JobExecutionID;
                                _Manager.ExecuteNonQuery(h);
                            }
                            currentExecution.RequiredThreadID = newThread;
                            Queue(currentExecution); //put it back into the queue. after being removed
                            currentExecution.ThreadChecked = true;
                            return;
                        }
                    }

                    //extra safety. Enter the monitor for code if single threaded. (Essentially, conditional lock block)
                    Lock jobLockTarget = currentJobMetaData.SingleThreaded ? Lock.Exclusive : Lock.NoLock; 
                    using (new LockHelper(jobLockTarget, currentJobMetaData.NameSpace + "." + currentJobMetaData.JobName))
                    {
                        LogStart();
                        success = job.Execute(this, currentExecution, ref status);
                        LogFinish();
                    }
                }
                if (cancelSuccess)
                {
                    SetExecutionStatus(false, false, ExecutionStatus.CANCELLED);
                }
                else
                {
                    if(!success && _requeue)
                    {
                        Queue(currentExecution);
                        return;
                    }
                    if(status == null)
                    {
                        if (success)
                            status = new ExecutionStatus { ExecutionStatusCode = ExecutionStatus.STEP_COMPLETE, NameSpace = nameof(SEIDR) };
                        else
                            status = new ExecutionStatus { ExecutionStatusCode = ExecutionStatus.FAILURE, NameSpace = nameof(SEIDR) };

                    }
                    else if (string.IsNullOrWhiteSpace(status.NameSpace))
                        status.NameSpace = currentJobMetaData.NameSpace;

                    CheckStatus(status);

                    SetExecutionStatus(success, false, status.ExecutionStatusCode, status.NameSpace);
                    SendNotifications(currentExecution, success);
                    /*  Probably not needed because SET_STATUS does the same thing basically.
                    if(!success && currentExecution.CanRetry)
                    {
                        currentExecution.DelayStart = DateTime.Now.AddMinutes(currentExecution.RetryDelay);
                        Queue(currentExecution);
                    }*/
                }
            }
            catch (Exception ex)
            {
                SetExecutionStatus(false, false);
                LogError("JobExecutor.Work()", ex);
                /* Shouldn't be needed because SET_STATUS does the same thing basically
                if (currentExecution.CanRetry)
                {
                    currentExecution.DelayStart = DateTime.Now.AddMinutes(currentExecution.RetryDelay);
                    Queue(currentExecution);
                }*/
            }
            finally
            {
                currentJobMetaData = null;
                currentExecution = null;
            }
        }
        void SendNotifications(JobExecutionDetail executedJob, bool success)
        {
            string subject;
            string MailTo = string.Empty;
            string Message = string.Empty;
            if (success)
            {
                if (string.IsNullOrWhiteSpace(executedJob.SuccessNotificationMail)
                    || !executedJob.Complete) //Out parameter on set status.
                    return;
                MailTo = executedJob.SuccessNotificationMail;
                subject = "Job Execution completed: JobProfile " + executedJob.JobProfileID;                
            }
            else
            {
                if (string.IsNullOrWhiteSpace(executedJob.FailureNotificationMail))
                    return;
                MailTo = executedJob.FailureNotificationMail;
                subject = $"Job Execution Step failure: Job Profile {executedJob.JobProfileID}, Step {executedJob.StepNumber}";
            }
            Message = $"JobExecutionID: {executedJob.JobExecutionID}{Environment.NewLine} Cancelled? {cancelSuccess}{Environment.NewLine} Execution Start Time: {executedJob.DetailCreated}"
                + (executedJob.ExecutionTimeSeconds.HasValue ? ". Execution Duration: " + executedJob.ExecutionTimeSeconds.ToString() : string.Empty);

            CallerService.TrySendMail(MailTo, subject, Message);

        }
        void SetExecutionStatus(bool success, bool working, string statusCode = null, string StatusNameSpace = "SEIDR")
        {
            if (currentExecution == null)
                return;            
            using (var i = _Manager.GetBasicHelper(currentExecution, true))
            {
                i.QualifiedProcedure = SET_STATUS;
                i["Working"] = working;
                i["Success"] = success;
                i["ExecutionStatusCode"] = statusCode;
                i["ExecutionStatusNameSpace"] = StatusNameSpace;

                i.BeginTran();
                var next = _Manager.SelectSingle<JobExecutionDetail>(i, CommitSuccess: true);
                if (next != null)
                {
                    Queue(next);
                }
                //else
                //    currentExecution.Complete = (bool)i["@Complete"]; //completion notification
            }
        }
        /// <summary>
        /// Add the Execution Detail to the workQueue
        /// </summary>
        /// <param name="job">ExecutionDetail to queue for execution</param>
        /// <param name="Cut">If true, adds to position 0 and skips sorting.</param>
        public static void Queue(JobExecutionDetail job, bool Cut = false)
        {
            //lock (workLockObj)
            using(new LockHelper(Lock.Exclusive, WORK_LOCK_TARGET)) //pass target, static
            {
                if (workQueue.Exists(detail => detail.JobExecutionID == job.JobExecutionID))
                {
                    workQueue.RemoveAll(detail => detail.JobExecutionID == job.JobExecutionID && detail.DetailCreated <= job.DetailCreated);
                    //shouldn't happen, but as a safety, keep the latest one.                    
                    if (workQueue.Exists(detail => detail.JobExecutionID == job.JobExecutionID))
                        return;
                }
                //else if (replaceOnly) //Execution is going to be validated and refreshed before actually starting work anyway, so safe to add to the queue anyway.
                //    return;

                if (Cut)
                    workQueue.Insert(0, job);
                else
                {
                    workQueue.Add(job);
                    workQueue.Sort((a, b) =>
                    {
                        //positive: a is greater.                    
                        if (a.DelayStart.HasValue && b.DelayStart.HasValue)
                        {
                            if (a.DelayStart.Value > b.DelayStart.Value)
                                return 1;
                            return -1;
                        }
                        else if (a.DelayStart.HasValue)
                            return 1;
                        if (b.DelayStart.HasValue)
                            return -1; // (int)DateTime.Now.Subtract(b.DelayStart.Value).TotalSeconds; //Treat b as greater
                        if (a.WorkPriority > b.WorkPriority)
                            return 1;
                        return a.WorkPriority < b.WorkPriority ? -1 : 0;
                    });
                }
            }
        }

        public void UpdateStatus(ExecutionStatus workingStatus)
        {
            workingStatus.IsComplete = false;
            workingStatus.IsError = false;
            SetExecutionStatus(false, true, workingStatus.ExecutionStatusCode, workingStatus.ExecutionStatusCode);
        }

        /// <summary>
        /// Called by Service during startup, before setting up individual jobexecutors.
        /// </summary>
        /// <param name="Manager"></param>
        public static void CheckLibrary(DatabaseManager Manager)
        {
            if (LastLibraryCheck.AddMinutes(20) >= DateTime.Now)
                return;
            using (new LockHelper(Lock.Exclusive, LIBRARY_TARGET))
            {
                Library.RefreshLibrary();
                try
                {
                    Library.ValidateOperationTable(Manager);
                }
                finally
                {
                    LastLibraryCheck = DateTime.Now;
                }
            }
        }
        #region workload
        /// <summary>
        /// Goes through the work queue. If something workable is found, removes it from the queue and returns it
        /// </summary>
        /// <returns></returns>
        JobExecutionDetail CheckWork()
        {
            lock (NameLock)
                SetThreadName(null);
            //lock (workLockObj)
            using(var h = new LockHelper(Lock.Shared, WORK_LOCK_TARGET))
            {                
                if (workQueue.UnderMaximumCount(Match, 0))
                    return null;                
                foreach (var detail in workQueue)
                {
                    if (!Match(detail))
                        continue; //Cannot start, or for a different thread

                    string threadName = detail.JobThreadName;
                    if (string.IsNullOrWhiteSpace(threadName))
                        threadName = $"{detail.JobNameSpace}.{detail.JobName}";

                    lock (NameLock)
                    {                        
                        //If we already have this ThreadName, don't need to check other threads, so skip
                        //If job is considered single threaded, need to check for any other thread running the job.
                        if (detail.JobSingleThreaded && !CallerService.CheckSingleThreadedJobThread(detail, ThreadID))
                        {                            
                            if (detail.RequiredThreadID != null)
                                detail.DelayStart = DateTime.Now.AddMinutes(1);


                            continue;
                            /*
                                If ThreadID is specified(not null, matches current threadID), then another thread is running with this JobName, 
                                but the job still has a required ThreadID. 
                                Add a delay and check the next record.
                                */
                        }
                        else
                            SetThreadName(threadName);

                    }
                    h.Transition(Lock.Exclusive);
                    workQueue.Remove(detail);
                    return detail;
                }            
            }
            return null;
        } 
        /// <summary>
        /// Identify if the JobExecutor includes the JobExecutionID in its workload
        /// </summary>
        /// <param name="JobExecutionID"></param>
        /// <param name="remove">If it's not the current execution, remove from the workload queue.</param>
        /// <returns>True if the JobExecutionID is being worked or in the queue. 
        /// <para>Null if it has been removed from the queue as a result of this call.</para>
        /// <para>False if the execution was not on this Executor's workload</para>
        /// </returns>
        public bool? CheckWorkQueue(long JobExecutionID, bool remove)
        {
            lock (WorkLock)
            {
                if (!IsWorking)
                    return false;
            }
            if (currentExecution.JobExecutionID == JobExecutionID)
                return true;
            //lock (workLockObj)
            using(var h = new LockHelper(Lock.Shared_Exclusive_Intent, WORK_LOCK_TARGET))
            {
                int i = workQueue.FindIndex(je => je.JobExecutionID == JobExecutionID);
                if (i >= 0)
                {
                    h.Transition(Lock.Exclusive);
                    if (remove)
                    {
                        workQueue.RemoveAt(i);
                        return null;
                    }
                    return true;
                }
            }
            return false;
        }
        public static bool CheckWorkQueue(long jobExecutionID)
        {
            using(new LockHelper(Lock.Shared, WORK_LOCK_TARGET))
            {
                return workQueue.Exists(d => d.JobExecutionID == jobExecutionID);
                    
            }
        }
        protected override void CheckWorkLoad()
        {           
            //lock (workLockObj)
            using(new LockHelper(Lock.Shared, WORK_LOCK_TARGET))
            {
                if(workQueue.HasMinimumCount(Match, 1))
                    return;
                
            }

            //first call a method on the callerService and see if there's any jobs we can grab from other threads.
            //If a thread has >= 5 jobs, grab a couple jobs from the thread. 
            using (var h = _Manager.GetBasicHelper())
            {
                h.QualifiedProcedure = GET_WORK;
                h.AddKey(nameof(ThreadID), ThreadID);
                h.AddKey("ThreadCount", JobExecutorCount);
                h.AddKey(nameof(BatchSize), BatchSize);
                LogInfo("Work_SL. ThreadID: " + h[nameof(ThreadID)]?.ToString() ?? "(NULL)" );
                
                //lock (workLockObj)
                using(var lh = new LockHelper(Lock.Exclusive, WORK_LOCK_TARGET))
                {
                    workQueue.AddRange(_Manager.SelectList<JobExecutionDetail>(h));
                    lh.Transition(Lock.Shared);
                    LogInfo("Added Jobs to WorkQueue. Queued Count:" + workQueue.Count(Match));
                }
            }
        }

        bool Match(JobExecutionDetail check)
        {
            if (check.RequiredThreadID == null)
                return check.CanStart;
            if (1 + (check.RequiredThreadID % JobExecutorCount) != ThreadID) //Modulo is 0 based, ThreadID is 1 based
                return false;
            return check.CanStart;
        }
        static List<JobExecutionDetail> workQueue = new List<JobExecutionDetail>();
        public override int Workload
        {
            get
            {                
                using(new LockHelper(Lock.Shared, WORK_LOCK_TARGET))
                    return workQueue.Count(Match);
            }
        }
        #endregion
        #region Service features
        public override void Wait(int sleepSeconds, string logReason)
        {
            CallerService.LogToFile(this, currentExecution, "Sleep Requested: " + logReason);
            SetStatus("Sleep requested:" + logReason, JobBase.Status.StatusType.Sleep_JobRequest);
            Thread.Sleep(sleepSeconds * 1000);
            SetStatus("Wake from Job Sleep Request");
        }
        const int LOG_FAILURE_WAIT = 5 * 1000;
        public void LogError(string message, Exception ex)
        {
            int count = 10;
            while(!CallerService.LogExecutionError(this, currentExecution, 
                message + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace, 
                ExtraID: null) &&  count > 0)
            {
                count--;                
                Thread.Sleep(LOG_FAILURE_WAIT);
            }
        }
        void LogStart()
        {
            if (currentExecution == null)
                return;
            currentExecution.Start();
            int count = 5;
            while(!CallerService.LogExecutionStartFinish(this, currentExecution, true) && count > 0)
            {
                count--;
                Thread.Sleep(LOG_FAILURE_WAIT);
            }
        }
        void LogFinish()
        {
            if (currentExecution == null)
                return;
            currentExecution.Finish();
            int count = 5;
            while (!CallerService.LogExecutionStartFinish(this, currentExecution, false) && count > 0)
            {
                count--;
                Thread.Sleep(LOG_FAILURE_WAIT);
            }
        }
        void IJobExecutor.LogInfo(string message) => LogInfo(message, false);
        public override void LogInfo(string message, bool shared = false)
        {
            int count = 5;
            while(!CallerService.LogToFile(this, currentExecution, message) && count > 0)
            {
                count--;
                Thread.Sleep(LOG_FAILURE_WAIT);
            }
        }

        protected override string HandleAbort()
        {
            if (currentExecution == null)
                return null;
            string msg = "JobExecutionID: " + currentExecution.JobExecutionID;
            SetExecutionStatus(false, false,  ExecutionStatus.CANCELLED);
            return msg;
            
        }
        public override bool Stop()
        {
            if (CallerService.ServiceAlive && (currentJobMetaData?.SafeCancel == true))
            {
                CancelRequested = true;
                return false;
            }            
            return base.Stop();
        }
        #endregion
    }
}
