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
    public class JobExecutor: Executor, iJobExecutor
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
        
    @JobExecutionID bigint,
	@JobProfileID int,
	@FilePath varchar(250),
	@FileSize bigint,
    @StepNumber smallint,
	@Success bit,
    @Working
        public DatabaseConnection connection => _Manager.CloneConnection();

        public JobProfile job => currentJob;
        

        bit,
	@ExecutionStatusCode varchar(5) = null,
	@ExecutionStatusNameSpace varchar(128) = null
        public void Requeue(int delayMinutes)
        {
            throw new NotImplementedException();
            Dictionary<string, object> Keys = new Dictionary<string, object>
            {
                { "@JobExecutionID", currentExecution.JobExecutionID}
            };
            using(var i = _Manager.GetBasicHelper(Keys, REQUEUE))
            {
                var ds = _Manager.Execute(i);
            }
            
        }
        protected override void Work()
        {
            base.Work();
            try
            {
                
            }
            catch(ThreadAbortException ab)
            {
                SetStatus(false, false, "CX");
            }
            catch(Exception ex)
            {
                SetStatus(false, false);
            }
        }
        void SetStatus(bool success, bool working, string statusCode = null, string StatusNameSpace = "SEIDR")
        {
            if (currentExecution == null)
                return;
            Dictionary<string, object> Keys = new Dictionary<string, object>
            {
                { "@JobExecutionID", currentExecution.JobExecutionID},
                { "@FilePath", currentExecution.FilePath },
                { "@FileSize", currentExecution.FileSize },
                { "@Success", success},
                { "@Working", working },
                { "@StepNumber", currentExecution.StepNumber },
                { "@ExecutionStatusCode", statusCode },
                { "@ExecutionStatusNameSpace", StatusNameSpace }
            };
            using(var i = _Manager.GetBasicHelper(Keys, SET_STATUS))
            {
                var next = _Manager.Execute(i).GetFirstRowOrNull().ToContentRecord<JobExecution>();
                if (next != null)
                {
                    if (next.RequiredThreadID == ThreadID)
                        Queue(next);
                    else
                        callerService.QueueExecution(next);
                }
            }
        }
        public void Queue(JobExecution job, bool Cut = false)
        {
            if (Cut)
                workQueue.Insert(0, job);
            else
                workQueue.Add(job);
        }
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
                        var md = Library.GetJobMetaData(je.JobName, je.JobNameSpace);
                        if (md.SingleThreaded)
                        { //If job is considered single threaded, need to check for any other thread running the job.
                            if (!callerService.CheckSingleThreadedJobThread(je, ThreadID))
                            {
                                if (je.RequiredThreadID == null)
                                    workQueue.RemoveAt(i);
                                i--; //Removing record at i. Need to decrement so we don't skip a record.
                                continue;
                            }
                        }
                        workQueue.RemoveAt(i);
                        ThreadName = je.JobThreadName;
                    }                                        
                    return je;
                }
            }
            return null;
        }
        List<JobExecution> workQueue;
        public override int Workload => workQueue.Count;
        public void Wait(int sleepSeconds, string logReason)
        {
            throw new NotImplementedException();
        }

        public void LogError(string message, Exception ex)
        {
            throw new NotImplementedException();
        }

        public void LogInfo(string message)
        {
            throw new NotImplementedException();
        }
    }
}
