using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.JobExecutor
{
    /// <summary>
    /// Removes delay for any JobExecution records that have been flagged to 'RunNow'.
    /// Also increases the priority and triggers sorting the JobExecutor WorkQueue
    /// </summary>
    class ResetDelayExecutor : Executor
    {
        public ResetDelayExecutor( JobExecutorService caller, DataBase.DatabaseManager manager)
            :base(manager, caller, ExecutorType.Maintenance)
        {
            workList = new List<JobExecutionDetail>();
        }
        List<JobExecutionDetail> workList;
        public override int Workload => workList.Count;

        protected override void CheckWorkLoad()
        {            
            using (var h = _Manager.GetBasicHelper(true))
            {
                h.QualifiedProcedure = "[SEIDR].[usp_JobExecutionDetail_RePrioritize]";
                h["BatchSize"] = CallerService.BatchSize;
                h.BeginTran();
                workList = _Manager.SelectList<JobExecutionDetail>(h);
                h.CommitTran();
            }
        }
        
        protected override void Work()
        {
            while(workList.HasMinimumCount(1))
            {
                var detail = workList[0];
                JobExecutor.Queue(detail, false); 
                LogInfo("Reprioritized JobID: " + detail.JobExecutionID);
                //Replace older version of the execution detail.
                workList.RemoveAt(0);                
            }
        }
    }
}
