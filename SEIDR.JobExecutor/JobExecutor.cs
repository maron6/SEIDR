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
            lock (statusListLock)
            {
                statusList.Clear();
                statusList = manager.SelectList<ExecutionStatus>(Schema:"SEIDR");
            }
        }
        void CheckStatus(ExecutionStatus check)
        {
            lock(statusListLock)
            {
                if (statusList.Exists(s => s.NameSpace == check.NameSpace && s.ExecutionStatusCode == check.ExecutionStatusCode))
                    return;
                statusList.Add(check);
            }
            //format: SEIDR.usp_{0}_i
            _Manager.Insert(check);
        }
        const string LIBRARY_TARGET = nameof(SEIDR.JobExecutor) + "." + nameof(Library);
        LockManager libraryLock = new LockManager(LIBRARY_TARGET); //NOT static.
        static object statusListLock = new object();
        public static void ConfigureLibrary(string location)
        {
            if (Library == null)
                Library = new JobLibrary(location);
        }
        /// <summary>
        /// Work Queue lock
        /// </summary>
        static object workLockObj = new object();
        /// <summary>
        /// Thread name lock. (Since Job imports can be single thread required, organized by Name)
        /// </summary>
        static object NameLock = new object();

        public JobExecutor( DatabaseManager manager, JobExecutorService caller)
            : base(manager, caller, ExecutorType.Job)
        {
            CheckStatus(new ExecutionStatus { ExecutionStatusCode = JobExecutionDetail.COMPLETE, IsComplete = true, NameSpace = "SEIDR" });
            CheckStatus(new ExecutionStatus { ExecutionStatusCode = JobExecutionDetail.FAILURE, IsError = true, NameSpace = "SEIDR" });
            CheckStatus(new ExecutionStatus { ExecutionStatusCode = JobExecutionDetail.REGISTERED, NameSpace = "SEIDR" });
            CheckStatus(new ExecutionStatus { ExecutionStatusCode = JobExecutionDetail.SCHEDULED, NameSpace = "SEIDR" });
            CheckStatus(new ExecutionStatus { ExecutionStatusCode = JobExecutionDetail.STEP_COMPLETE, NameSpace = "SEIDR" });            
            CheckStatus(new ExecutionStatus { ExecutionStatusCode = JobExecutionDetail.CANCELLED, NameSpace = "SEIDR" });
        }
        const string SET_STATUS = "SEIDR.usp_JobExecution_SetStatus";
        const string REQUEUE = "SEIDR.usp_JobExecution_Requeue";
        const string GET_WORK = "SEIDR.usp_JobExecution_sl_Work";
        const string START_WORK = "SEIDR.usp_JobExecution_StartWork";
        JobProfile currentJob;
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

        public JobProfile job => currentJob;


        public void Requeue(int delayMinutes)
        {
            Dictionary<string, object> Keys = new Dictionary<string, object>
            {
                { "@JobExecutionID", currentExecution.JobExecutionID}
            };
            using (var i = _Manager.GetBasicHelper(Keys, REQUEUE))
            {
                var ds = _Manager.Execute(i);
                var jb = ds.GetFirstRowOrNull(0).ToContentRecord<JobExecutionDetail>();
                jb.DelayStart = DateTime.Now.AddMinutes(delayMinutes);
                Queue(jb);
                currentExecution = null;
            }

        }
        protected override void Work()
        {
            cancelSuccess = false;
            CancelRequested = false;
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
                currentJob = _Manager.SelectSingle<JobProfile>(currentExecution);
                SetExecutionStatus(false, true);
                ExecutionStatus status = null;
                bool success = false;
                using (new LockHelper(libraryLock, Lock.Shared))
                {
#pragma warning disable 420
                    int newThread;
                    IJob job = Library.GetOperation(currentExecution.JobName,
                            currentExecution.JobNameSpace,
                            out currentJobMetaData);
#pragma warning restore 420

                    if (!job.CheckThread(currentExecution, ThreadID, out newThread)
                        && newThread % CallerService.ExecutorCount != ThreadID) 
                    {
                        //if new thread goes over ExecutorCount, it's okay in this thread. if newThread % ExecutorCount is this ID
                        currentExecution.RequiredThreadID = newThread;
                        CallerService.QueueExecution(currentExecution);
                        return;
                    }
                    success = job.Execute(this, currentExecution, ref status);
                }
                if (cancelSuccess)
                {
                    SetExecutionStatus(false, false, "CX");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(status.NameSpace))
                        status.NameSpace = currentJobMetaData.NameSpace;

                    CheckStatus(status);

                    SetExecutionStatus(success, false, status.ExecutionStatusCode, status.NameSpace);
                    SendNotifications(currentExecution, success);
                }
            }
            catch (Exception ex)
            {
                SetExecutionStatus(false, false);
                LogError("JobExecutor.Work()", ex);
            }
            finally
            {
                currentJobMetaData = null;
            }
        }
        void SendNotifications(JobExecutionDetail executedJob, bool success)
        {
            string subject;
            string MailTo = string.Empty;
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
            

        }
        void SetExecutionStatus(bool success, bool working, string statusCode = null, string StatusNameSpace = "SEIDR")
        {
            if (currentExecution == null)
                return;
            Dictionary<string, object> Keys = new Dictionary<string, object>
            {
                { "@JobExecutionID", currentExecution.JobExecutionID},
                { "@JobProfileID", currentExecution.JobProfileID },
                { "@FilePath", currentExecution.FilePath },
                { "@FileSize", currentExecution.FileSize },
                { "@FileHash", currentExecution.FileHash },
                { "@Success", success},
                { "@Working", working },
                { "@StepNumber", currentExecution.StepNumber },
                { "@ExecutionStatusCode", statusCode },
                { "@ExecutionStatusNameSpace", StatusNameSpace },
                { "@Complete", false }
            };
            using (var i = _Manager.GetBasicHelper(Keys, SET_STATUS))
            {
                i.BeginTran();
                var next = _Manager.SelectSingle<JobExecutionDetail>(i, CommitSuccess: true);
                if (next != null)
                {
                    Queue(next);
                }
                else
                    currentExecution.Complete = (bool)i["@Complete"];
            }
        }
        public static void Queue(JobExecutionDetail job, bool Cut = false)
        {
            lock (workLockObj)
            {
                if (Cut)
                    workQueue.Insert(0, job);
                else
                    workQueue.Add(job);
            }
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

        }/*
        void CheckLibrary()
        {
            if (LastLibraryCheck.AddMinutes(15) >= DateTime.Now)
                return;
            //libraryLock.Acquire(Lock.Exclusive);

            //lock (libraryLock)
            using (new LockHelper(libraryLock, Lock.Exclusive))
            {
                //  Don't care so much if validate has an exception, 
                //  but don't care errors from loading library itself         
                Library.RefreshLibrary();
                try
                {

                    Library.ValidateOperationTable(_Manager);
                }
                finally
                {
                    LastLibraryCheck = DateTime.Now;
                }
            }
        }*/
        /// <summary>
        /// Goes through the work queue. If something workable is found, removes it from the queue and returns it
        /// </summary>
        /// <returns></returns>
        JobExecutionDetail CheckWork()
        {
            lock (NameLock)
                SetThreadName(null);
            lock (workLockObj)
            {
                SortWork();
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
                        SetThreadName(threadName);
                        //If we already have this ThreadName, don't need to check other threads, so skip
                        //If job is considered single threaded, need to check for any other thread running the job.
                        if (!CallerService.CheckSingleThreadedJobThread(detail, ThreadID))
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
                    }
                    workQueue.Remove(detail);
                    return detail;
                }            
            }
            return null;
        }
        void SortWork()
        {
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
        /* --Deprecated via static list - only threads without a requiredThreadID can be redistributed and they are all in the same queue.
        /// <summary>
        /// Removes up to <paramref name="count"/> records from the back of the queue.
        /// </summary>
        /// <param name="count"></param>
        /// <param name="workingList">List of JobExecutions to be redistributed.</param>
        /// <returns></returns>
        public void UndistributeWork(int count, List<JobExecution> workingList)
        {

            if (Workload == 0)
                return;
            lock (workLockObj)
            {
                SortWork();
                for (int i = workQueue.Count - 1; i >= 0; i--)
                {
                    var je = workQueue[i];
                    if (je.JobSingleThreaded || je.RequiredThreadID.HasValue)
                        continue;
                    workingList.Add(je);
                    workQueue.RemoveAt(i); //Going backwards through the list, don't need to worry about the position messing up.                  
                }
            }
        }        
        public void DistributeWork(int count, List<JobExecutionDetail> workList)
        {
            lock (workLockObj)
            {
                if (count > workList.Count)
                    count = workList.Count;
                if (count == 0)
                    return;
                workQueue.AddRangeLimited(workList, count);
                SortWork();
                workList.RemoveRange(0, count);
            }
        }
        void DistributeWork(List<JobExecutionDetail> list)
            => DistributeWork(list.Count, list);
        */
        /// <summary>
        /// Identify if the JobExecutor includes the JobExecutionID in its workload
        /// </summary>
        /// <param name="JobExecutionID"></param>
        /// <param name="remove">If it's not the current execution, remove from the workload queue.</param>
        /// <returns>True if the JobExecutionID is being worked or in the queue. 
        /// <para>Null if it has been removed from the queue as a result of this call.</para>
        /// <para>False if the execution was not on this Executor's workload</para>
        /// </returns>
        public bool? CheckWorkLoad(long JobExecutionID, bool remove)
        {
            lock (WorkLock)
            {
                if (!IsWorking)
                    return false;
            }
            if (currentExecution.JobExecutionID == JobExecutionID)
                return true;
            lock (workLockObj)
            {
                int i = workQueue.FindIndex(je => je.JobExecutionID == JobExecutionID);
                if (i >= 0)
                {
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

        protected override void CheckWorkLoad()
        {
            /*
            List<JobExecution> temp = new List<JobExecution>();
            if (CallerService.GrabShareableWork(this, temp))
            {
                DistributeWork(temp);
                return;
            }*/
            lock (workLockObj)
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
                h.AddKey(nameof(BatchSize), BatchSize);
                
                lock (workLockObj)
                {
                    workQueue.AddRange(_Manager.SelectList<JobExecutionDetail>(h));
                }
            }
        }

        bool Match(JobExecutionDetail check)
        {
            if (check.RequiredThreadID == null)
                return true;
            if (check.RequiredThreadID % JobExecutorCount != ThreadID)
                return false;
            return check.CanStart;
        }
        static List<JobExecutionDetail> workQueue;
        public override int Workload
        {
            get
            {
                lock (workLockObj)
                    return workQueue.Count(Match);
            }
        }
        public override void Wait(int sleepSeconds, string logReason)
        {
            CallerService.LogFileError(this, currentExecution, "Sleep Requested: " + logReason);
            SetStatus("Sleep requested:" + logReason, JobBase.Status.ThreadStatus.StatusType.Sleep_JobRequest);
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

        public override void LogInfo(string message)
        {
            int count = 10;
            while(!CallerService.LogFileError(this, currentExecution, message) && count > 0)
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
            SetExecutionStatus(false, false, "CX");
            return msg;
            
        }
        public override bool Stop()
        {
            if (CallerService.ServiceAlive && (currentJobMetaData?.SafeCancel == true))
            {
                CancelRequested = true;
                return false; //Thread does not need to be restarted
            }            
            return base.Stop();
        }
    }
}
