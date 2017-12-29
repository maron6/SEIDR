using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SEIDR.OperationServiceModels.Status;

namespace SEIDR.OperationServiceModels
{
    public interface IOperatorManager
    {
        DataBase.DatabaseManager DataManager { get; }
        bool LogBatchError(Operator caller, Batch errBatch, string Message, string Extra, int? Batch_FileID = null);
        bool LogError(Operator caller, string Message);
        bool LogBatchError(Batch errBatch, string Message, string ExtraInfo, int? Batch_FileID = null);
        long GetFileSize(string FilePath);
        DateTime ParseNameDate(FileInfo file, string format, int dateOffset = 0);
        bool ServiceAlive { get; }
        bool Paused { get; }
        Operator GetOperator(OperatorType type, byte ID);
        void DistributeBatch(Batch nonSpecifiedThreadBatch);
        void DistributeBatches(IEnumerable<Batch> Batches);
        Operator GetOperatorByBatchID(OperatorType type, int BatchID);
        /// <summary>
        /// Call PauseEvent.WaitOne() to prevent doing work when service is supposed to be paused.
        /// </summary>
        ManualResetEvent PauseEvent { get; }
        /// <summary>
        /// Max number of batches to select at a time
        /// </summary>
        byte BatchSize { get; }
        /// <summary>
        /// Extra space for WorkQueue - Batch/Work queue limit determined by BatchSize + Margin
        /// </summary>
        byte QueueLimitMargin { get; }
        ServiceStatus MyStatus { get; }
    }
    public abstract class Operator
    {
        public byte ID { get; private set; }
        public string Name { get; private set; }
        public OperatorType MyType { get; private set; }
        protected IOperatorManager Manager { get; private set; }
        protected DataBase.DatabaseManager DbManager
        {
            get
            {
                return Manager.DataManager;
            }
        }
        private List<Batch> WorkQueue { get; set; } = new List<Batch>();
        protected Batch CurrentBatch { get; private set; } = null;
        public bool CheckForBatch(int CheckBatchID)
        {
            lock (workLock)
            {
                if (CurrentBatch.BatchID == CheckBatchID)
                    return true;
                return WorkQueue.Exists(w => w.BatchID == CheckBatchID);
            }
        }
        public int Workload { get { return WorkQueue.Count + (CurrentBatch != null? 1 : 0); } }
        public bool Working { get; protected set; } = false;
        object workLock = new object();
        /// <summary>
        /// Add batches to a list - to be used by cancellation and/or Executor Operators. Queue may be able to use profile off of it?
        /// </summary>
        /// <param name="batchList"></param>
        public void AddBatches(IEnumerable<Batch> batchList)
        {
            lock (workLock)
            {
                WorkQueue.AddRangeLimited(
                    batchList.Exclude(
                        b => b.BatchID == null || WorkQueue.Exists(q => q.BatchID == b.BatchID))
                    , Manager.BatchSize + Manager.QueueLimitMargin); //Exclude null batches or batches whose batchID is already in the operator.
                                              //limit number of batches that can be added to BatchSize + 3
            }
        }
        public void AddBatch(Batch b)
        {
            lock (workLock)
            {                
                if(CurrentBatch == null || b.BatchID != CurrentBatch.BatchID.Value)
                {
                    //Can turn down adding batch in this case, if workload is high enough
                    if(Workload > Manager.BatchSize + Manager.QueueLimitMargin)        
                        return; //Allow some minor leeway
                }
                if(!WorkQueue.Exists(q => q.BatchID == b.BatchID))
                    WorkQueue.Add(b);
            }
        }
        
        private Batch PopBatch()
        {
            if (WorkQueue.Count == 0)
                return null;
            Batch b = WorkQueue[0];
            WorkQueue.RemoveAt(0);            
            return b;
        }
        public ThreadInfo myInfo { get; protected set; }
        public ThreadStatus MyStatus { get; private set; }
        ~Operator()
        {
            if (worker != null && worker.ThreadState == ThreadState.Running)
            {
                worker.Abort(); 
                //Should have already exited loop from ServiceAlive = false by the time we get here, though.                 
            }
            worker = null;
        }
        public void Sleep(string Message, bool Failure = false)
        {
            if (!string.IsNullOrWhiteSpace(Message))
                Message += " - ";
            else
                Message = string.Empty;

            Message += Failure ? "Sleeping due to failure" : "Sleeping";
            MyStatus.SetStatus(Message, Failure ? ThreadStatus.StatusType.Error : ThreadStatus.StatusType.Sleep);
            Thread.Sleep(Failure ? 3000 : 6000);
        }
        public Operator(IOperatorManager owner, OperatorType type, byte ID, string Name)
        {
            if (owner == null)
                throw new ArgumentNullException("owner");
            if (string.IsNullOrWhiteSpace(Name))
                throw new ArgumentException("'Name' is null or white space");
                
            Manager = owner;
            MyType = type;
            this.Name = Name;
            this.ID = ID;            
            myInfo = new ThreadInfo(Name, type.ToString(), ID);
            MyStatus = new ThreadStatus(myInfo);
            owner.MyStatus.Add(MyStatus);
            MyStatus.MyStatus = ThreadStatus.StatusType.Unknown;
            worker = new Thread(() =>
            {
                owner.PauseEvent.WaitOne();
                MyStatus.SetStatus("Starting up Thread Work", ThreadStatus.StatusType.Start);
                Working = true;
                while (Manager.ServiceAlive)
                {
                    try
                    {
                        MyStatus.SetStatus("Check Pause", ThreadStatus.StatusType.General);
                        owner.PauseEvent.WaitOne();
                        if (!Manager.ServiceAlive)
                            break;
                        MyStatus.SetStatus("Checking for work", ThreadStatus.StatusType.General);
                        lock (workLock)
                        {
                            Working = true;
                            if (WorkQueue.Count > 0 || CheckWork())
                            {
                                CurrentBatch = PopBatch();
                                MyStatus.SetStatus("Found work!", ThreadStatus.StatusType.General);
                                DoWork();
                                CurrentBatch = null;                              
                            }
                            else
                                Sleep("No Work found", false);                                                        
                        }
                        Working = false;
                        MyStatus.SetStatus("Check Pause");
                        owner.PauseEvent.WaitOne();
                    }
                    catch(ThreadAbortException)
                    {
                        MyStatus.SetStatus("Aborted", ThreadStatus.StatusType.Error);
                        HandleAbort();
                        lock (workLock)
                        {
                            CurrentBatch = null;
                            /*
                            WorkQueue.ForEach(qb => 
                            {
                                try
                                {
                                    AbortQueuedBatch(qb);
                                }
                                catch(Exception ex)
                                {
                                    owner.LogBatchError(this, 
                                        qb, ex.Message, "Error Aborting Queued batch", 
                                        null);
                                }
                            });
                            */
                            //WorkQueue.Clear(); 
                            //Actually, don't really care about the work queue. can just leave alone
                            // so that if we're not closing down the service, can just get back to work.
                            // if we are closing down the service, the clean locks call at the start should do the clean up.
                        }
                        owner.LogError(this, "Aborted!");
                        return;
                    }
                    catch(Exception ex)
                    {
                        MyStatus.SetStatus(ex.Message, ThreadStatus.StatusType.Error);
                        if (CurrentBatch != null)
                            owner.LogBatchError(this, CurrentBatch, ex.Message, this.Name, null);
                        else
                            owner.LogError(this, ex.Message);
                        lock (workLock)
                        {
                            CurrentBatch = null;                            
                        }
                        Sleep(true);
                    }
                }
                Working = false;
                MyStatus.SetStatus("Exited Work Loop", ThreadStatus.StatusType.Finish);                
            });
            worker.Name = Name;
            worker.Priority = type == OperatorType.Execution ? ThreadPriority.Normal : ThreadPriority.BelowNormal;
            worker.IsBackground = true;            
        }
        public void Sleep(bool failure) => Sleep(null, failure);
        
        public abstract void HandleAbort();
        //public abstract void AbortQueuedBatch(Batch queued);
        public abstract bool CheckWork();
        public abstract void DoWork();
        private Thread worker { get; set; }

        public void Call()
        {
            if (worker.IsAlive || worker.ThreadState.In(ThreadState.Running, ThreadState.Background, ThreadState.WaitSleepJoin) )
                return;
            worker.Start();
        }
        public void Reset()
        {
            if (worker.IsAlive && worker.ThreadState != ThreadState.AbortRequested)
            {                
                worker.Abort();
            }
            Sleep("Reset!");
            Call(); //If worker is already alive still somehow, will return right away
        }        
        public void ConditionalReset(int batchID)
        {
            lock (workLock)
            {
                if (CurrentBatch?.BatchID == batchID)
                {
                    Reset();
                }
                else
                {
                
                    int x;
                    WorkQueue = WorkQueue.Exclude(w => w.BatchID == batchID, out x);
                    if(x > 0)
                    {
                        DbManager.ExecuteTextNonQuery(@"
UPDATE SEIDR.Batch
SET BatchStatusCode = 'SX', LU = GETDATE()
WHERE BatchID = " + batchID, true);
                    }
                }
            }
        }
        public virtual void Stop()
        {
            if (worker.IsAlive
                && worker.ThreadState != ThreadState.AbortRequested
                && worker.ThreadState != ThreadState.StopRequested
                && worker.ThreadState != ThreadState.Stopped)
            {                
                MyStatus.SetStatus("Stopping", ThreadStatus.StatusType.Unknown);
                worker.Abort();
            }
        }
    }
    public enum OperatorType
    {
        Execution,
        Maintenance
    }
}
