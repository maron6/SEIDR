using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SEIDR.OperationServiceModels
{
    [InheritedExport(typeof(iOperation))]
    public interface iOperation
    {
        [Import]
        IOperatorManager Manager {set;}
        /// <summary>
        /// Executes the Operation.
        /// </summary>
        /// <param name="b">Batch to execute. Contains a list of file objects</param>
        /// <param name="parameters">Results of calling the procedure from meta data. May be null.</param>
        /// <param name="BatchStatus">Attempt to Override result status. Will be validated.</param>
        /// <returns>True if completed successfully, else false</returns>
        bool Execute(Batch b, DataSet parameters, ref string BatchStatus);       
        /// <summary>
        /// Allow for a specialized status message for notification e-mail. Ok to return null
        /// </summary>
        /// <param name="ExecuteResult"></param>
        /// <param name="BatchStatus"></param>
        /// <returns></returns>
        string GetResultNotification(bool ExecuteResult, string BatchStatus);        
    }
    [InheritedExport(typeof(iOperationMetaData))]
    public interface iOperationMetaData
    {
        /// <summary>
        /// Name of your operation
        /// </summary>
        string Operation { get; }
        /// <summary>
        /// A namespace for your operation to prvent of overlap. Should not be null, but defaults to 'SEIDR'.
        /// <para>If parameter select does not include a schema, this will be used for the Schema.</para>
        /// </summary>
        [DefaultValue("SEIDR")]
        string OperationSchema { get; }
        /// <summary>
        /// Description of operation's purpose. 
        /// </summary>
        [DefaultValue(null)]
        string Description { get; }
        /// <summary>
        /// Numeric identifier - allow updating the operation while keeping a legacy version intact. Version defaults to 1
        /// </summary>
        [DefaultValue(1)]
        int Version { get; }
        /// <summary>
        /// For forcing the Operator service to pass to a specific thread. 
        /// <para>Default is to use null - set thread based on batch type or profile</para>
        /// <para>NOTE: If your operation is not thread safe/uses global variables for state, set this to a value between 1 and 15</para>
        /// <para>If the threadID is zero or too high for the set up, then the operation will be ignored.</para>
        /// <para>Higher than 4 is better to set on batch types/profiles, rather than on the operation, though</para>
        /// </summary>
        [DefaultValue(null)]
        byte? ThreadID { get; }
        /// <summary>
        /// Procedure to be called by Operator service - the datarow will be passed on along with the batch.
        /// <para>Should take a @Profile_OperationID (int or bigger) as the primary parameter</para>
        /// <para>BatchID will also be passed if available as a parameter</para>
        /// <para>Will be ignored if null or white space</para>
        /// </summary>
        [DefaultValue(null)]
        string ParameterSelect { get; }
    }    
}
