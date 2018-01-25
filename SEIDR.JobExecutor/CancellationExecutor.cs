using SEIDR.OperationServiceModels;
using SEIDR.DataBase;
using System.Collections.Generic;
using System.Linq;
using SEIDR.JobBase;

namespace SEIDR.JobExecutor
{
    public class CancellationExecutor: Executor
    {
        public const string GET_CANCEL_REQUEST = "usp_JobExecution_ss_Cancel";
        DatabaseManagerHelperModel model;
        IEnumerable<JobExecutor> execList;
        public CancellationExecutor(JobExecutorService caller, DatabaseManager database, IEnumerable<JobExecutor> executors)
            :base(database, caller, ExecutorType.Maintenance)
        {
            execList = executors;
            model = database.GetBasicHelper(QualifiedProcedure: GET_CANCEL_REQUEST, Keys:null);
        }

        public override int Workload => cancel != null ? 1 : 0;
        volatile JobExecutionDetail cancel = null; //volatile - Workload may get checked from another thread

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
                if (jobThread.Stop()) //Calls thread Join, so should wait for the job to finish                                
                    jobThread.Call(); 
                //If above returned false, stop request came from somewhere else, so don't call. 
                //Work that needed to stop has stopped, though, so return true
                return true;                                
            }
            return false;
        }
        protected override void CheckWorkLoad()
        {
            cancel = _Manager.SelectSingle<JobExecutionDetail>(model);
        }
        protected override void Work()
        {
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
}
