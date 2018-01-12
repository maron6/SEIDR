using SEIDR.OperationServiceModels;
using SEIDR.DataBase;
using System.Collections.Generic;
using System.Linq;

namespace SEIDR.JobExecutor
{
    public class CancellationExecutor: Executor
    {
        public const string GET_CANCEL_REQUEST = "usp_JobExecution_ss_Cancel";
        DatabaseManagerHelperModel model;
        IEnumerable<JobExecutor> execList;
        public CancellationExecutor(int ID, JobExecutorService caller, DatabaseManager database, IEnumerable<JobExecutor> executors)
            :base(ID, database, caller, ExecutorType.Maintenance)
        {
            execList = executors;
        }

        public override int Workload => IsWorking ? 1 : 0;

        protected override string HandleAbort()
        {
            return null;
        }
        private bool Cancel(JobExecutor jobThread, long ExecutionID)
        {
            bool? check = null;
            check = jobThread.CheckWorkLoad(ExecutionID, true);
            if (check == null)
                return true;
            if (check.Value)
            {
                jobThread.Stop();
                while (jobThread.IsWorking)
                {
                    //wait for thread abort to finish..
                    Wait(FAILURE_SLEEPTIME, $"Cancelling thread {jobThread.ThreadName} ({jobThread.LogName})...");
                }
                jobThread.Call();
                return true;
            }
            return false;
        }
        protected override void Work()
        {
            JobBase.JobExecution cancel = _Manager.Execute(model).GetFirstRowOrNull().ToContentRecord<JobBase.JobExecution>();
            if (cancel == null || cancel.JobExecutionID == null)
            {
                Wait(FAILURE_SLEEPTIME, "No Work");
                return;
            }
            int skipThread = -1;
            if(cancel.RequiredThreadID != null)
            {
                var thread = (from t in execList
                              where t.ThreadID == cancel.RequiredThreadID % CallerService.ExecutorCount
                              select t).FirstOrDefault();
                if(thread != null && Cancel(thread, cancel.JobExecutionID.Value))
                {
                    return;
                }
                skipThread = thread.ThreadID;
            }
            foreach(var t in execList)
            {
                if (t.ThreadID == skipThread)
                    continue;
                if (Cancel(t, cancel.JobExecutionID.Value))
                    return;
            }
        }
    }
    public class OCancellationExecutor : Operator
    {
        public const string GET_CANCEL_REQUESTS = "usp_Batch_sl_Cancel";
        DatabaseManagerHelperModel Model;
        public OCancellationExecutor(IOperatorManager owner, byte ID) 
            : base(owner, OperatorType.Maintenance, ID, "CANCELLATION")
        {
            Model = new DatabaseManagerHelperModel(
                GET_CANCEL_REQUESTS, 
                new { ThreadID = ID, owner.BatchSize })
            {
                RetryOnDeadlock = true
            };
        }

        public override bool CheckWork()
        {
            var b = DbManager.Execute(Model).ToContentList<Batch>(0);            
            if (b == null || b.Count == 0)
                return false;
            AddBatches(b);
            return true;
        }

        public override void DoWork()
        {
            Batch work = CurrentBatch;
            if (work == null || work.BatchID.HasValue == false)
                return; //Shouldn't happen (CheckWork should only return work after adding Batch(es) to the work queue, but just in case             
            if (work.ThreadID.HasValue)
                Manager
                    .GetOperator(OperatorType.Execution, work.ThreadID.Value)
                    .ConditionalReset(work.BatchID.Value);
            else
            {
                //Try to find the batch, reset the thread if found.
                Manager
                    .GetOperatorByBatchID(OperatorType.Execution, work.BatchID.Value)
                    ?.ConditionalReset(work.BatchID.Value);
            }
        }

        public override void HandleAbort()
        {            
            return;
        }
    }
}
