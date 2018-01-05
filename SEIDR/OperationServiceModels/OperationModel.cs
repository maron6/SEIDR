using SEIDR.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.OperationServiceModels
{
    //Replace with populating UDT and passing to validate procedure
    public class OperationModel
    {
        /// <summary>
        /// Set by Service from MetaData.
        /// </summary>
        public string OperationSchema { get; set; }
        /// <summary>
        /// Set by Service from MetaData.
        /// </summary>
        public string Operation { get; set; }
        /// <summary>
        /// Set by Service from MetaData
        /// </summary>
        public int Version { get; set; }
        public string Description { get; set; }
        public string ComputedTableName
        {
            get
            {
                string x = OperationSchema.Replace(' ', '_').Replace("[", "").Replace("]", "")
                    + "_" + Operation.Replace(' ', '_').Replace("[", "").Replace("]", "")
                    + "_" + Version.ToString();
                if (x.Length > 128)
                    x = x.Substring(0, 128);
                return x;
            }
        }
        /// <summary>
        /// For forcing an operation to use a specific thread. 
        /// </summary>
        public int? ThreadID { get; set; } = null;
        //OperationParameterModel Parameters { get; set; }
        public string AsXML()
        {
            return $"<OperationModel Assembly=\"{OperationSchema}\" Operation=\"{Operation}\" Version=\"{Version.ToString()}\" Description=\"{Description}\" ThreadID=\"{ThreadID.ToString()}\" />";                        
        }
        public void ValdiateDB(DatabaseManager dbm)
        {            
            /*
            DataBase.Schema.TABLE table = DataBase.Schema.TABLE.FromObject(Parameters, ComputedTableName, "SEIDR");
            var m = new
            {
                OperationSchema,
                Operation,
                Version,
                Description,
                ThreadID,
                ComputedTableName,
                Script = table.GetCreateTableScript()
            };
            bool t = dbm.RethrowException;
            dbm.RethrowException = false;
            dbm.ExecuteNonQuery("SEIDR.usp_Operation_Validate", m);
            var m2 = new
            {
                OperationSchema,
                Operation,
                Version,
                Parameters
            };
            dbm.ExecuteNonQuery("SEIDR.usp_Operation_Parameter_Validate", m2);
            dbm.RethrowException = t;*/
        }
        public static string AsXML(List<OperationModel> Operations)
        {
            string x = "<OperationModelList>";
            Operations.ForEach(o => x += o.AsXML());
            return x + "</OperationModelList>";
        }        
    }
}
