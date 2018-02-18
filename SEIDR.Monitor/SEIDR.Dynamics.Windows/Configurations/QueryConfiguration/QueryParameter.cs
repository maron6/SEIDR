using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SEIDR.Dynamics.Configurations.QueryConfiguration
{
    public class QueryParameter
    {
        public string ParameterName { get; set; }
        [DefaultValue(null)]
        public object ParameterValue { get; set; } = DBNull.Value;
        [XmlIgnore]
        public Type ParameterType { get; set; }
        [Obsolete("Don't use this directly - it's just for Serialization", true)]
        public string ParamTypeName
        {
            get
            {
                return ParameterType.AssemblyQualifiedName;
            }
            set
            {
                ParameterType = Type.GetType(value);
            }
        }
        [DefaultValue(false)]
        public bool OverrideRunTime { get; set; } = false;

        [XmlIgnore]
        public string OverrideColumnName
        {
            get
            {
                string x = ParameterName;
                if (x[0] == '@')
                    x = x.Substring(1);
                return x + "_Override_At_Runtime";
            }
        }
        /*
         * Flow: Create query, build sqlparameters-> pass to convert parameters, add these QueryParameters to the query object/do initial mapping
         * Run time: If there are any parameters that need to be overridden at runtime, map them to a data row for run time and populate with 
         * editable dashboard. Then add together with the QueryParameters that don't need override and update the SqlParameters in the SqlCommand and run
         */
        public static QueryParameter[] ConvertParameters(System.Data.SqlClient.SqlParameter[] builtParams)
        {
            List<QueryParameter> ret = new List<QueryParameter>();
            foreach (var p in builtParams)
            {
                if (p.ParameterName == "@RETURN_VALUE")
                    continue;
                ret.Add(new QueryParameter
                {
                    ParameterName = p.ParameterName,
                    ParameterType = SQLDbTypeHelper.ConvertDBType(p.SqlDbType),
                    OverrideRunTime = false,
                    ParameterValue = DBNull.Value
                });
            }
            return ret.ToArray(); //Values will be edited in editable dashboard...
        }
        public static void MapDataRow(DataRow editedValues, QueryParameter[] values)
        {
            var cols = editedValues.Table.Columns;
            foreach (QueryParameter qp in values)
            {
                if (cols.Contains(qp.ParameterName))
                {
                    qp.ParameterValue = editedValues[qp.ParameterName];
                }
                if (cols.Contains(qp.OverrideColumnName))
                {
                    qp.OverrideRunTime = (bool)editedValues[qp.OverrideColumnName];
                }
                else //Already runTime, editing a copy
                {
                    qp.OverrideRunTime = false;
                }
            }
        }
        public static DataRowView ConvertList(QueryParameter[] parameters, bool isRunTime = false)
        {
            if (parameters == null)
                return null;
            DataTable dt = new DataTable();
            foreach (var qp in parameters)
            {
                if (isRunTime && !qp.OverrideRunTime)
                    continue;
                dt.Columns.Add(new DataColumn
                {
                    ColumnName = qp.ParameterName,
                    DataType = qp.ParameterType,
                    DefaultValue = qp.ParameterValue,
                    AllowDBNull = true
                });
                if (!isRunTime)
                {
                    dt.Columns.Add(new DataColumn
                    {
                        DataType = typeof(bool),
                        ColumnName = qp.OverrideColumnName,
                        Caption = "Override '" + qp.ParameterName + "' at Query runtime",
                        DefaultValue = false,
                        AllowDBNull = false
                    });
                }
            }
            dt.Rows.Add(dt.NewRow());
            return dt.AsDataView()[0];
        }

        public static Dictionary<string, object> ToDictionary(IEnumerable<QueryParameter> parameters)
        {
            return (from p in parameters
                    select new { p.ParameterName, p.ParameterValue })
                    .ToDictionary(a => a.ParameterName, a => a.ParameterValue);                    
        }
    }
}
