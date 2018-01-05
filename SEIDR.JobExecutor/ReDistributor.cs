using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.JobExecutor
{
    public class ReDistributor : Executor
    {
        bool _working;
        public override bool IsWorking => _working;
        public ReDistributor(int id, DataBase.DatabaseManager manager, JobExecutorService caller, IList<Executor> executors)
            :base(id, manager, caller, ExecutorType.Maintenance)
        {
            list = (from ex in executors
                    where ex.ExecutorType == ExecutorType.Job
                    select ex as JobExecutor);
        }
        IEnumerable<JobExecutor> list;
        public override int Workload => throw new NotImplementedException();

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
