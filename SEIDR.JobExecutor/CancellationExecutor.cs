using SEIDR.OperationServiceModels;
using SEIDR.DataBase;

namespace SEIDR.JobExecutor
{
    public class CancellationExecutor : Operator
    {
        public const string GET_CANCEL_REQUESTS = "usp_Batch_sl_Cancel";
        DatabaseManagerHelperModel Model;
        public CancellationExecutor(IOperatorManager owner, byte ID) 
            : base(owner, OperatorType.Maintenance, ID, "CANCELLATION")
        {
            Model = new DatabaseManagerHelperModel(
                GET_CANCEL_REQUESTS, 
                new { ThreadID = ID, owner.BatchSize })
            {
                RetryOnDeadlock = true
            };
        }

        public override bool CheckWork()
        {
            var b = DbManager.Execute(Model).ToContentList<Batch>(0);            
            if (b == null || b.Count == 0)
                return false;
            AddBatches(b);
            return true;
        }

        public override void DoWork()
        {
            Batch work = CurrentBatch;
            if (work == null || work.BatchID.HasValue == false)
                return; //Shouldn't happen (CheckWork should only return work after adding Batch(es) to the work queue, but just in case             
            if (work.ThreadID.HasValue)
                Manager
                    .GetOperator(OperatorType.Execution, work.ThreadID.Value)
                    .ConditionalReset(work.BatchID.Value);
            else
            {
                //Try to find the batch, reset the thread if found.
                Manager
                    .GetOperatorByBatchID(OperatorType.Execution, work.BatchID.Value)
                    ?.ConditionalReset(work.BatchID.Value);
            }
        }

        public override void HandleAbort()
        {            
            return;
        }
    }
}
