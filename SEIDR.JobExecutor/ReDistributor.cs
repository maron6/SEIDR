using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.JobExecutor
{
    public class ReDistributor : Executor
    {
        public ReDistributor(int id, DataBase.DatabaseManager manager, JobExecutorService caller, IEnumerable<JobExecutor> executors)
            :base(id, manager, caller, ExecutorType.Maintenance)
        {
            list = executors;
        }
        IEnumerable<JobExecutor> list;
        public override int Workload => list.Count();

        protected override string HandleAbort()
        {
            return null;
        }

        protected override void Work()
        {
            throw new NotImplementedException();
        }
    }
}
