using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.JobExecutor
{
    public abstract class Executor
    {
        public int ThreadID { get; private set; }
        public string ThreadName { get; protected set; }
        public DataBase.DatabaseManager _Manager { get; private set; }
        protected JobExecutorService callerService { get; private set; }
        public ExecutorType executorType { get; private set; }
        public Executor(int id, DataBase.DatabaseManager manager, JobExecutorService caller, ExecutorType type)
        {
            ThreadID = id;
            callerService = caller;
            executorType = type;
            _Manager = manager.Clone(true, type.GetDescription() + ": Thread #" + id);            
        }
        public abstract int Workload { get; }
        protected virtual void Work()
        {

        }
    }
    public enum ExecutorType
    {
        Maintenance,
        Job
    }
}
