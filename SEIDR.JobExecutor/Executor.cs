using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.JobBase.Status;
using System.Threading;

namespace SEIDR.JobExecutor
{
    public abstract class Executor
    {


        protected const int DEADLOCK_TIME_INCREASE = 45;
        protected const int MAX_TIMEOUT = 1200;

        public int ThreadID { get; private set; }
        public string ThreadName { get; private set; }
        public string LogName { get; private set; }
        public DataBase.DatabaseManager _Manager { get; private set; }
        protected JobExecutorService CallerService { get; private set; }
        public ExecutorType ExecutorType { get; private set; }
        protected ThreadInfo Info { get; private set; }
        public ThreadStatus Status { get; private set; }
        public Executor(int id, DataBase.DatabaseManager manager, JobExecutorService caller, ExecutorType type)
        {
            ThreadID = id;
            CallerService = caller;
            ExecutorType = type;
            string logName = type.GetDescription() + ": Thread #" + id;
            LogName = $"{type}_{id}";
            _Manager = manager.Clone(true, logName);

            Info = new ThreadInfo(logName, type.ToString(), id);
            Status = new ThreadStatus(Info) { MyStatus = ThreadStatus.StatusType.Unknown };
            caller.MyStatus.Add(Status);            
        }
        public void SetThreadName(string newName)
        {
            string mgrName = ExecutorType.GetDescription() + ": Thread #" + ThreadID;
            if (!string.IsNullOrWhiteSpace(newName))
            {
                mgrName += " - " + newName;
            }
            ThreadName = newName;
            _Manager.ProgramName = mgrName;            
        }
        public virtual bool IsWorking => true;
        public abstract int Workload { get; }
        protected abstract void Work();
        protected abstract string HandleAbort();
        protected void SetStatus(string message, ThreadStatus.StatusType status = ThreadStatus.StatusType.General)
        {
            Status.SetStatus(message, status);
        }
        public void Call()
        {
            while (CallerService.ServiceAlive)
            {
                try
                {
                    CallerService.PauseEvent.WaitOne();
                    Work();
                }
                catch(ThreadAbortException)
                {
                    var m = HandleAbort();
                    if (!string.IsNullOrWhiteSpace(m))
                        SetStatus(m, ThreadStatus.StatusType.Unknown);
                }
                catch(Exception ex)
                {
                    CallerService.LogError(null, ex.Message);
                }
            }
        }
    }
    public enum ExecutorType
    {
        Maintenance,
        Job
    }
}
