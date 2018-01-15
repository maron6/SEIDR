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
        public bool IsWorking { get; set; }
        public abstract int Workload { get; }
        protected abstract void Work();
        protected abstract string HandleAbort();
        protected void SetStatus(string message, ThreadStatus.StatusType status = ThreadStatus.StatusType.General)
        {
            Status.SetStatus(message, status);
        }
        Thread worker;
        public void Call()
        {
            if(worker == null)
            {
                worker = new Thread(internalCall)
                {
                    IsBackground = true,
                    Name = LogName
                };
            }
            if (worker.ThreadState.In(ThreadState.Running, ThreadState.WaitSleepJoin, ThreadState.AbortRequested, ThreadState.SuspendRequested))
                return;
            worker.Start();
        }
        public void Stop()
        {
            if (worker.ThreadState.In(ThreadState.Aborted, ThreadState.AbortRequested))
                return;
            worker.Abort();
            worker.Join();
        }
        void internalCall()
        {            
            while (CallerService.ServiceAlive)
            {                
                try
                {
                    CallerService.PauseEvent.WaitOne();
                    IsWorking = true;
                    Work();
                }
                catch(ThreadAbortException)
                {                    
                    var m = HandleAbort();
                    if (!string.IsNullOrWhiteSpace(m))
                        SetStatus(m, ThreadStatus.StatusType.Unknown);
                    throw; //should throw anyway, but just to be sure
                }
                catch(Exception ex)
                {
                    CallerService.LogError(null, ex.Message);
                }
                finally
                {
                    IsWorking = false;
                }
            }
        }
        protected const int FAILURE_SLEEPTIME = 15;
        public virtual void Wait(int sleepSeconds, string logReason)
        {
            if (string.IsNullOrWhiteSpace(logReason))
                logReason = "(UNSPECIFIED)";
            CallerService.LogError(this, "Sleep Requested: " + logReason);
            SetStatus("Sleep requested:" + logReason, ThreadStatus.StatusType.Sleep_JobRequest);
            Thread.Sleep(sleepSeconds * 1000);
            SetStatus("Wake from Job Sleep Request");
        }
        
        public virtual void LogInfo(string message)
        {
            int count = 10;
            while (!CallerService.LogError(this, message) && count > 0)
            {
                count--;
                Thread.Sleep(FAILURE_SLEEPTIME * 1000);
            }
        }
    }
    public enum ExecutorType
    {
        Maintenance,
        Job
    }
}
