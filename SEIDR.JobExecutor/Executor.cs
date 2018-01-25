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

        static int _maintenanceCounter = 0;
        static int _jobCounter = 0;
        public static int JobExecutorCount => _jobCounter;
        public static int MaintenanceCount => _maintenanceCounter;
        protected const int DEADLOCK_TIME_INCREASE = 45;
        protected const int MAX_TIMEOUT = 1200;

        public int ThreadID { get; private set; }
        volatile string _ThreadName;
        public string ThreadName
        {
            get { return _ThreadName; }
            private set { _ThreadName = value; }
        }
        public string LogName { get; private set; }
        public DataBase.DatabaseManager _Manager { get; private set; }
        protected JobExecutorService CallerService { get; private set; }
        public ExecutorType ExecutorType { get; private set; }
        protected ThreadInfo Info { get; private set; }
        public ThreadStatus Status { get; private set; }
        protected object WorkLock = new object();
        protected abstract void CheckWorkLoad();
        public Executor(DataBase.DatabaseManager manager, JobExecutorService caller, ExecutorType type)
        {
            int id;
            if (type == ExecutorType.Job)
                id = ++_jobCounter;
            else
                id = ++_maintenanceCounter;

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
            ThreadName = newName ?? "ThreadID" + ThreadID.ToString();
            _Manager.ProgramName = mgrName;
        }
        public volatile bool IsWorking;
        public abstract int Workload { get; }
        protected abstract void Work();
        protected abstract string HandleAbort();
        protected void SetStatus(string message, ThreadStatus.StatusType status = ThreadStatus.StatusType.General)
        {
            lock (Status) //Not best practice, but it is a status for 'this'
            {
                Status.SetStatus(message, status);
            }
        }
        Thread worker;
        public void Call()
        {
            if(worker == null)
            {
                worker = new Thread(internalCall)
                {
                    IsBackground = true, 
                    //Note that background threads if a service doesn't have any foreground threads.
                    //Full threads because they're for long running processes.
                    Name = LogName
                };
            }
            int count = 20;
            while(worker.ThreadState.In(ThreadState.AbortRequested, ThreadState.SuspendRequested) && count > 0)
            {
                Wait(FAILURE_SLEEPTIME, "Waiting for thread to finish Abort/Suspend request...");
                count--;
            }
            lock (WorkLock)
            {
                if (worker.ThreadState.In(ThreadState.Running, ThreadState.WaitSleepJoin,
                    ThreadState.AbortRequested, ThreadState.SuspendRequested, ThreadState.Background))
                {
                    SetStatus("Executor.Call() - Thread Status is still: " + worker.ThreadState + " after waiting. Return.", 
                        ThreadStatus.StatusType.Unknown);
                    return;
                }
                IsWorking = false;
                worker.Start();
            }
        }
        public virtual bool Stop()
        {
            lock (WorkLock)
            {
                if (!IsWorking)
                    return false;
            }
            if (worker.ThreadState.In(ThreadState.Aborted, ThreadState.AbortRequested, ThreadState.Stopped, ThreadState.StopRequested))
                return false;
            worker.Abort();
            worker.Join();
            return true;
        }
        void internalCall()
        {            
            while (CallerService.ServiceAlive)
            {                
                try
                {
                    CallerService.PauseEvent.WaitOne();
                    SetStatus("Check Workload", ThreadStatus.StatusType.Start);
                    CheckWorkLoad();
                    if(Workload == 0)
                    {
                        Wait(FAILURE_SLEEPTIME, "No Work found");
                        continue;
                    }
                    lock (WorkLock)
                    {
                        IsWorking = true;
                    }
                    Work();
                    SetStatus("Finish Work", ThreadStatus.StatusType.Finish);
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
                    SetStatus("Error:" + ex.Message, ThreadStatus.StatusType.Error);
                }
                finally
                {
                    lock (WorkLock)
                    {
                        IsWorking = false;
                    }
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
