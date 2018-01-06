using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.OperationServiceModels;
using SEIDR.DataBase;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using SEIDR.ThreadManaging;

namespace SEIDR.JobExecutor
{
    class OperationExecutor : Operator
    {
        public static DatabaseManager ExecutionManager; // Difference from the DbManager - this one will throw exceptions and have to be caught in here.
        static JobLibrary Library;
        static DateTime LastLibraryCheck = new DateTime(1, 1, 1);
        /// <summary>
        /// Timeout increase on deadlock.
        /// </summary>
        const int DEADLOCK_TIME_INCREASE = 45;
        const int MAX_TIMEOUT = 1200;

        public static Mailer ExecutionAlerts { private get; set; }
        public static void SetLibrary(string Location)
        {
            Library = new JobLibrary(Location);
            CheckLibrary();
        }        
        
        DatabaseManagerHelperModel MapModel;
        public OperationExecutor(IOperatorManager owner, byte ID) 
            : base(owner, OperatorType.Execution, ID, "OperationExecutor_" + ID)
        {

            var Map = new
            {
                ThreadID = ID,
                BatchSize = owner.BatchSize
            };
            MapModel = new DatabaseManagerHelperModel
            {
                QualifiedProcedure = "SEIDR.usp_Batch_SL_Work",
                RetryOnDeadlock = true,                
                ParameterMap = Map
            };
            MapModel.SetConnection(ExecutionManager);
        }

        public override bool CheckWork()
        {            
            var batches = ExecutionManager.SelectList<Batch>(MapModel);            
            if (batches.Count() == 0)
                return false;
            AddBatches(batches);
            return true;
        }
        //static object libraryLock = new object();        
        static LockManager libraryLock = new LockManager("LIBRARY");
        LockManager myLock = new LockManager("LIBRARY");
        public static void CheckLibrary()
        {
            if (LastLibraryCheck.AddMinutes(15) >= DateTime.Now)
                return;
            //libraryLock.Acquire(Lock.Exclusive);

            //lock (libraryLock)
            using (new LockHelper(libraryLock, Lock.Exclusive))                                    
            {
                //  Don't care so much if validate has an exception, 
                //  but don't care errors from loading library itself         
                Library.RefreshLibrary();
                try
                {
                    
                    Library.ValidateOperationTable(ExecutionManager); 
                }
                finally
                {
                    LastLibraryCheck = DateTime.Now;
                }
            }
            //libraryLock.Release(); //Allow operations to continue again.            

        }
        void WorkBatch(Batch cb)
        {
            iOperationMetaData md = null;
            iOperation op = null;
            //lock (libraryLock)
            {
                 //op = Library.GetOperation(cb.Operation, cb.OperationSchema, cb.Version, out md);
            }
            #region invalid operation
            if (op == null)
            {
                Manager.LogBatchError(cb, "Unable to find Operation", $"[{cb.OperationSchema ?? "SEIDR" }][{cb.Operation ?? "_NULL_"}] V:{cb.Version}");
                cb.BatchStatus = BATCHSTATUS.INVALID_STOP;
                cb.OperationSuccess = false;
                //DbManager.Update(cb);
                while (true)
                {
                    try
                    {
                        ExecutionManager.Update(cb);
                        ExecutionManager.TimeOut = DbManager.TimeOut;
                        //After success, return Timeout to the original timeout. Then return.
                        return;
                    }
                    catch (SqlException ex)
                    {
                        if (!ex.ErrorCode.In(1205, -2))
                            throw;
                        Sleep(true);
                        if (ex.ErrorCode < 0 && ExecutionManager.TimeOut < MAX_TIMEOUT)
                            ExecutionManager.TimeOut += DEADLOCK_TIME_INCREASE;
                    }
                }
            }
            #endregion
            string Message = null;
            #region invalid thread for operation
            if (md.ThreadID != null && md.ThreadID != ID)
            {
                var o = Manager.GetOperator(OperatorType.Execution, md.ThreadID.Value);
                if (o != null)
                {
                    o.AddBatch(cb);
                    return;
                }
                else
                {
                    Manager.LogBatchError(cb,
                        "Batch Thread does not match Operation Meta data, but the corresponding Thread was not found - continuing on the specified thread anyway",
                        $"{md.OperationSchema ?? "SEIDR"}.{md.Operation}.{md.Version} - Thread: " + md.ThreadID.Value);
                }
            }
            #endregion
            CurrentBatch.SetMissingThreadID(ID); //Set thread ID if not populated
            #region Prep for getting parameters
            var map = new
            {
                cb.Profile_OperationID,
                cb.OperationID,
                cb.BatchID,
                cb.BatchProfileID,                
            };
            string QualifiedProc = md.ParameterSelect;
            DatabaseManagerHelperModel model = null;
            if (!string.IsNullOrWhiteSpace(QualifiedProc))
            {
                var Procedure = md.ParameterSelect.Split('.');
                if (Procedure.Length == 1) //Procedure is definitely unqualified. Add a default schema
                    QualifiedProc = md.OperationSchema + "." + md.ParameterSelect;
                model = new DatabaseManagerHelperModel(map, QualifiedProc);
            }
            #endregion
            string Error = "Getting parameters";
            try
            {
                DataSet param = null;
                if (model != null)
                    param = ExecutionManager.Execute(model);
                string status = null;
                Error = "Lock batch, starting work!";
                try
                {
                    int rowsUpdated = ExecutionManager.
                        ExecuteNonQuery("SEIDR.usp_Batch_StartWork", CurrentBatch, true);
                    if (rowsUpdated == 0)
                    {
                        Manager.LogError(this, "Batch is already locked"); 
                        //Log to file, not to table(Log to error could cause CE later)
                        return;
                    }
                }
                catch (SqlException ex)
                {
                    if (ex.ErrorCode.In(1205, -2)) //Shouldn't get a 1205 actually...(Retry on deadlock = true)
                    {
                        Manager.LogError(this, Error + ": " + ex.Message);
                        AddBatch(cb); //Stick it back in the work queue.                    
                        if (ex.ErrorCode < 0 && ExecutionManager.TimeOut < MAX_TIMEOUT)
                            ExecutionManager.TimeOut += DEADLOCK_TIME_INCREASE;
                    }
                    else
                    {
                        Manager.LogError(this, "Could not ensure a lock on the batch to start work!");
                    }
                    return;
                }
                Error = "Executing Operation";
                cb.OperationSuccess = op.Execute(cb, param, ref status);

                Error = "Attempting Updating Status: '" + status + "' from '" + cb.BatchStatus + "'";
                cb.BatchStatus = status;
                #region Update Status
                while (Error != null)
                {
                    try
                    {
                        ExecutionManager.Update(cb);
                        ExecutionManager.TimeOut = DbManager.TimeOut;
                        //After success, return Timeout to the original timeout. Then return.
                        Error = null; //Remove error now. Cause it finishhhhhhed. Wooooooo! Operation done!
                    }
                    catch (SqlException ex)
                    {
                        if (!ex.ErrorCode.In(1205, -2))
                            throw;
                        Sleep(true);
                        if (ex.ErrorCode < 0 && ExecutionManager.TimeOut < MAX_TIMEOUT)
                            ExecutionManager.TimeOut += DEADLOCK_TIME_INCREASE;
                    }
                }
                #endregion
            }
            #region Log errors
            catch (SqlException ex)
            {
                Message = "Sql Exception - " + Error; //Default message.
                if (ex.ErrorCode.In(1205, -2)) //Deadlock or timeout getting parameters... add the batch to re-doing work...
                {
                    Manager.LogError(this, ex.Message);
                    AddBatch(cb); //Stick it back in the work queue.                    
                    if (ex.ErrorCode < 0 && ExecutionManager.TimeOut < MAX_TIMEOUT)
                        ExecutionManager.TimeOut += DEADLOCK_TIME_INCREASE;
                    return;
                }
                //else if(Error.StartsWith("Updating Status"))

                HandleBatchFailure(cb, ex.Message, Message);
                return;
            }
            catch (Exception ex)
            {
                //Should be an operation exception...
                Message = "Non SQL Exception - " + Error;
                HandleBatchFailure(cb, ex.Message, Message);
                return;
            }
            #endregion
            Message = op.GetResultNotification(cb.OperationSuccess, cb.BatchStatus) ?? string.Empty;

            #region Mail Alerts
            if (cb.OperationSuccess)
            {
                if (!string.IsNullOrWhiteSpace(cb.SuccessNotification))
                {
                    string FormattedMessage = $"<p>Batch Profile: {cb.BatchProfileID}    Batch ID: {cb.BatchID}</p><p>Batch Date: {cb.BatchDate.ToString("MM/dd/yyyy")}</p>";                    
                    FormattedMessage += "<p>Batch StatusCode '" + cb.BatchStatus + "'    ";                    
                    FormattedMessage += "Batch FileCount: " //+ cb.Files.Count() 
                        + "</p><p>Attempts: " + cb.AttemptCount 
                        + "</p><p>Success Time: " + DateTime.Now.ToString("MMM dd, yyyy hh:ss")
                        + "    ThreadID: " + ID + "</p><br /><br />" + Message;

                    ExecutionAlerts.SendMailAlert("Batch Failure", FormattedMessage, recipient: cb.SuccessNotification);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(cb.FailureNotification))
                {
                    string FormattedMessage = $"<p>Batch Profile: {cb.BatchProfileID}    Batch ID: {cb.BatchID}</p><p>Batch Date: {cb.BatchDate.ToString("MM/dd/yyyy")}</p>";
                    FormattedMessage += "<p>Batch StatusCode '" + cb.BatchStatus + "'    ";
                    FormattedMessage += "Batch FileCount: " //+ cb.Files.Count()
                        + "</p><p>Attempts: " + cb.AttemptCount
                        + "</p><p>Failure Time: " + DateTime.Now.ToString("MMM dd, yyyy hh:ss")
                        + "    ThreadID: " + ID + "</p><br /><br />" + Message;

                    ExecutionAlerts.SendMailAlert("Batch Failure", FormattedMessage, recipient: cb.FailureNotification);
                }
            }
            #endregion
        }        
        public override void DoWork()
        { 
            if (CurrentBatch == null)
                return; //Shouldn't happen, but just in case
            //lock (libraryLock)
            //    Working++;
            myLock.Acquire(Lock.Shared);    //Share among operation executors.. 
                                            //Static LockManager will wait for exclusive.
            //lock (libraryLock)
            {
                WorkBatch(CurrentBatch);
            }
            myLock.Release();
            //lock (libraryLock)
            //    Working--;

        }
        public void HandleBatchFailure(Batch b, string Message, string ExtraInfo)
        {
            Manager.LogBatchError(b, Message, ExtraInfo);
            b.BatchStatus = BATCHSTATUS.WORKING_ERROR;
            b.OperationSuccess = false;
            DbManager.Update(b);
            if (!string.IsNullOrWhiteSpace(b.FailureNotification))
            {
                string FormattedMessage = $"<p>Batch Profile: {b.BatchProfileID}    Batch ID: {b.BatchID}</p><p>Batch Date: {b.BatchDate.ToString("MM/dd/yyyy")}</p>";
                FormattedMessage += "<p>Batch StatusCode '" + b.BatchStatus + "'    ";
                FormattedMessage += "Batch FileCount: "// + b.Files.Count() 
                    + "</p><p>Failure Time: " + DateTime.Now.ToString("MMM dd, yyyy hh:ss") + "    ThreadID: " + ID + "</p><br /><br />" + Message;

                ExecutionAlerts.SendMailAlert("Batch Failure", FormattedMessage, recipient: b.FailureNotification);
            }
        }        
        public override void HandleAbort()
        {
            if (CurrentBatch == null)
                return;            
            const string UPDATE_SPROC = "usp_Batch_U";            
            var model = new DatabaseManagerHelperModel(UPDATE_SPROC, new Dictionary<string, object>
            {
                { "BatchID", CurrentBatch.BatchID },
                { "BatchProfileID", CurrentBatch.BatchProfileID },
                { "Step", CurrentBatch.CurrentStep },
                { "BatchStatusCode", BATCHSTATUS.STOP_FULFILLED },
                { "OperationSuccess", false },
                { "FileXML", CurrentBatch.FileXML }
            });
            model.RetryOnDeadlock = true;
            DbManager.ExecuteNonQuery(model);            
        }        
    }
}
