using System.ComponentModel;

namespace SEIDR.OperationServiceModels
{    
    /// <summary>
    /// Descriptions of the base BatchStatuses. If an operation tries to pass an invalid status, then a base status will be chosen depending on
    /// whether it was expected to be success or failure.
    /// <para>E.g., trying to pass a failure status after returning true from operation will result in either 'S' or 'C'</para>
    /// </summary>
    public class BATCHSTATUS
    {
        /// <summary>
        /// Status set when the batch has been picked up for adding to the work queue.
        /// </summary>
        public const string WORKING = "W";
        /// <summary>
        /// Status set when the Batch is done with all steps
        /// </summary>
        public const string COMPLETE = "C";
        /// <summary>
        /// Status set when the batch is done with all steps but finished with either an error status or entries in the Error Log
        /// </summary>
        public const string COMPLETE_INVAlID = "CE";
        /// <summary>
        /// Status Set when Batch is created by a schedule
        /// </summary>
        public const string REGISTERED = "R";
        /// <summary>
        /// Status set when batch has been created by registering files
        /// </summary>
        public const string REGISTERED_FILES = "RL";
        /// <summary>
        /// Status set when a user wants to stop a Batch. 
        /// <para>If the step completes anyway, will go to either 'X' or 'CE', depending on the presence of additional steps
        /// </para>
        /// </summary>
        public const string STOP_REQUESTED = "SR";
        /// <summary>
        /// Status set when the Operation Stopper has found the batch
        /// </summary>
        public const string STOP_ACKNOWLEDGED = "SA";
        /// <summary>
        /// Status set when the worker has been force-stopped.
        /// </summary>
        public const string STOP_FULFILLED = "SX";
        /// <summary>
        /// Status set when the Batch is not in sequence
        /// </summary>
        public const string INVALID_SEQUENCE = "X";
        /// <summary>
        /// Status set when the batch execution failed.
        /// </summary>
        public const string WORKING_ERROR = "F";
        /// <summary>
        /// Status to be set when the Parameters are invalid or the batch is otherwise not valid for use with an operation.        
        /// </summary>
        public const string INVALID_STOP = "IV";
        /// <summary>
        /// Single step has been completed successfully
        /// </summary>
        public const string STEP_SUCCESS = "S";
        /// <summary>
        /// Single step has been partially completed, but can still move on to the next operation without finishing completely
        /// </summary>
        public const string PARTIAL_SUCCESS = "SE";
        /// <summary>
        /// Step should be skipped without doing work. E.g., an operation should only be done in exceptional cases
        /// </summary>
        public const string SKIP_STEP = "SK";                
    }
}