using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR_ProfileManager
{
    static class ProcedureInfo
    {
        /*
         * Each table will have an _SL as well, which will just return the records.
         * 
         * May need to set up a CRUD for operation_Parameter_KEY. (Default and allowed values for operations in each sequence..)
         */
        public const string STEP_PARAMETER_INFO = "SEIDR.usp_LoaderMagicStepParameter_GetInfo";
        public const string STEP_PARAMETER_CREATE = null;
        public const string STEP_PARAMETER_EDIT = "SEIDR.usp_Step_Parameter_iu"; //Should nullify the string
        public const string STEP_PARAMETER_DELETE = null; //Cannot remove parameters, can change value to null.        
        public const string STEP_INFO = "SEIDR.usp_LoaderMagicStep_GetInfo";
        public const string STEP_CREATE = "SEIDR.usp_LoaderMagicStep_iu";
        public const string STEP_UPDATE = "SEIDR.usp_LoaderMagicStep_iu";
        public const string STEP_DELETE = "SEIDR.usp_LoaderMagicStep_d";        
        public const string OPERATION_INFO = "SEIDR.usp_LoaderMagicOperation_GetInfo";
        public const string OPERATION_CREATE = "SEIDR.usp_LoaderMagicOperation_iu";
        public const string OPERATION_UPDATE = "SEIDR.usp_LoaderMagicOperation_iu";
        public const string OPERATION_DELETE = "SEIDR.usp_LoaderMagicOperation_d";
    }
}
