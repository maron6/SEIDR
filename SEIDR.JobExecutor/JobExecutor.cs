using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.DataBase;
using SEIDR.JobBase;
using System.Threading;

namespace SEIDR.JobExecutor
{
    public class JobExecutor: Executor, IJobExecutor
    {
        static JobLibrary Library { get; set; }
        static void ConfigureLibrary(string location)
        {
            Library = new JobLibrary(location);
        }
        static object lockObj = new object();
        public JobExecutor(int id, DatabaseManager manager, JobExecutorService caller)
            :base(id, manager, caller, ExecutorType.Job)
        {

        }
        const string SET_STATUS = "SEIDR.usp_JobExecution_SetStatus";
        const string REQUEUE = "SEIDR.usp_JobExecution_Requeue";
        JobProfile currentJob;
        JobExecution currentExecution;
        
    
        public DatabaseConnection connection => _Manager.CloneConnection();

        public JobProfile job => currentJob;
        

  
        public void Requeue(int delayMinutes)
        {            
            Dictionary<string, object> Keys = new Dictionary<string, object>
            {
                { "@JobExecutionID", currentExecution.JobExecutionID}
            };
            using(var i = _Manager.GetBasicHelper(Keys, REQUEUE))
            {
                var ds = _Manager.Execute(i);
                var jb = ds.GetFirstRowOrNull(0).ToContentRecord<JobExecution>();
                jb.DelayStart = DateTime.Now.AddMinutes(delayMinutes);
                Queue(jb);
                currentExecution = null;
            }
            
        }
        protected override void Work()
        {            
            try
            {
                currentExecution = CheckWork();
                currentJob = _Manager.SelectSingle<JobProfile>(currentExecution);
                SetExecutionStatus(false, true, statusCode: "W");
                IJob job = Library.GetOperation(currentExecution.JobName, currentExecution.JobNameSpace, out IJobMetaData data);
                ExecutionStatus status = null;
                bool success = job.Execute(this, currentExecution, ref status);
                SetExecutionStatus(success, false, status.ExecutionStatusCode, status.NameSpace);
                //if(!success)
            }
            catch(Exception ex)
            {
                SetExecutionStatus(false, false);
                LogError("JobExecutor.Work()", ex);
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
                { "@Success", success},
                { "@Working", working },
                { "@StepNumber", currentExecution.StepNumber },
                { "@ExecutionStatusCode", statusCode },
                { "@ExecutionStatusNameSpace", StatusNameSpace }
            };
            using (var i = _Manager.GetBasicHelper(Keys, SET_STATUS))
            {                
                i.BeginTran();
                var next = _Manager.Execute(i, CommitSuccess: true).GetFirstRowOrNull().ToContentRecord<JobExecution>();
                if (next != null)
                {
                    if (next.RequiredThreadID == ThreadID)
                        Queue(next);
                    else
                        CallerService.QueueExecution(next);
                }
            }
        }
        public void Queue(JobExecution job, bool Cut = false)
        {
            lock (lockObj)
            {
                if (Cut)
                    workQueue.Insert(0, job);
                else
                    workQueue.Add(job);
            }
        }
        /// <summary>
        /// Goes through the work queue. If something workable is found, removes it from the queue and returns it
        /// </summary>
        /// <returns></returns>
        JobExecution CheckWork()
        {
            if (Workload > 0)
            {
                lock (lockObj)
                {
                    JobExecution je = null;
                    for(int i = 0; i < workQueue.Count; i++)
                    {
                        je = workQueue[i];                        
                        if (je.DelayStart != null && je.DelayStart > DateTime.Now)
                            continue;
                        //var md = Library.GetJobMetaData(je.JobName, je.JobNameSpace);
                        if (je.JobSingleThreaded)
                        { //If job is considered single threaded, need to check for any other thread running the job.
                            if (!CallerService.CheckSingleThreadedJobThread(je, ThreadID))
                            {
                                if (je.RequiredThreadID == null)
                                    workQueue.RemoveAt(i);
                                i--; //Removing record at i. Need to decrement so we don't skip a record.
                                continue;
                            }
                        }
                        workQueue.RemoveAt(i);
                        SetThreadName(je.JobThreadName ?? je.JobName);
                    }                                        
                    return je;
                }
            }
            return null;
        }
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
            lock (lockObj)
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
                    return 0;
                });
                for(int i = workQueue.Count - 1; i >= 0; i--)
                {
                    var je = workQueue[i];
                    if (je.JobSingleThreaded || je.RequiredThreadID.HasValue)
                        continue;
                    workingList.Add(je);
                    workQueue.RemoveAt(i); //Going backwards through the list, don't need to worry about the position messing up.                  
                }
            }            
        }
        public void DistributeWork(int count, List<JobExecution> workList)
        {
            lock (lockObj)
            {
                if (count > workList.Count)
                    count = workList.Count;
                workQueue.AddRangeLimited(workList, count);
                workList.RemoveRange(0, count);                
            }
        }
        List<JobExecution> workQueue;
        public override int Workload => workQueue.Count;
        public void Wait(int sleepSeconds, string logReason)
        {
            CallerService.LogFileError(this, currentExecution, "Sleep Requested: " + logReason);
            SetStatus("Sleep requested:" + logReason, JobBase.Status.ThreadStatus.StatusType.Sleep_JobRequest);
            Thread.Sleep(sleepSeconds * 1000);
            SetStatus("Wake from Job Sleep Request");
        }

        public void LogError(string message, Exception ex)
        {
            int count = 10;
            while(!CallerService.LogExecutionError(this, currentExecution, message, ex.Message + Environment.NewLine + ex.StackTrace) &&  count > 0)
            {
                count--;
                Thread.Sleep(5 * 1000);
            }
        }

        public void LogInfo(string message)
        {
            int count = 10;
            while(!CallerService.LogFileError(this, currentExecution, message) && count > 0)
            {
                count--;
                Thread.Sleep(5 * 1000);
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
    }
}
