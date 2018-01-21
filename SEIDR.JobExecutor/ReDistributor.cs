using SEIDR.JobBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.JobExecutor
{
    public class ReDistributor : Executor
    {
        public ReDistributor(DataBase.DatabaseManager manager, JobExecutorService caller, IEnumerable<JobExecutor> executors)
            :base(manager, caller, ExecutorType.Maintenance)
        {
            list = executors;
        }
        IEnumerable<JobExecutor> list;
        public override int Workload => list.Count();

        protected override string HandleAbort()
        {
            return null;
        }
        protected override void CheckWorkLoad()
        {
            return; //Work is keeping an eye on other threads. Pretty much Constant.
        }

        protected override void Work()
        {
            double min = 9999;
            double max = -1;
            foreach(var je in list)
            {
                if (je.Workload < min)
                    min = je.Workload;
                if (je.Workload > max)
                    max = je.Workload;
            }
            List<JobExecution> dist = new List<JobExecution>();
            int boundary = (int)( min * (1 + min / max));
            foreach(var je in list)
            {
                if(je.Workload > boundary)
                {
                    je.UndistributeWork(je.Workload - boundary, dist);
                }                
            }            
            foreach(var je in list)
            {
                if(je.Workload < boundary)
                {
                    je.DistributeWork(je.Workload - boundary, dist);
                    if (dist.UnderMaximumCount(0))
                        return;
                }
            }
            foreach (var je in list)
            {                                
                je.DistributeWork(1, dist);
                if (dist.UnderMaximumCount(0))
                    return;                
            }
        }
    }
}
