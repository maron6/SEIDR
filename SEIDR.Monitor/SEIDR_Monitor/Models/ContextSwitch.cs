using SEIDR.Dynamics.Configurations;
using SEIDR.Dynamics.Configurations.ContextMenuConfiguration;
using SEIDR.Dynamics.Configurations.QueryConfiguration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.WindowMonitor.Models
{
    public class ContextSwitch
    {         
        public static QueryParameter[] DoContextSwitch(
            ContextMenuConfiguration Cmi,
            Query target, 
            DataRowView mapping)
        {
            var rqp = target.Parameters;
            if (rqp == null)
                return null; //No parameters to map, will auto run anyway;
            DataColumnCollection dcc = mapping.Row.Table.Columns;
            foreach (var qp in rqp)
            {
                if (!qp.OverrideRunTime)
                    continue;
                object value = DBNull.Value;
                string name = qp.ParameterName;
                string s = name[0] == '@' ? name.Substring(1) : name;
                if (dcc.Contains(s))
                {
                    value = mapping[s] ?? DBNull.Value;
                }/* ProcID shouldn't  be populated...
                else if (s == Cmi.ProcIDParameterName)
                {
                    value = Cmi.ProcID.HasValue ? (object)Cmi.ProcID : DBNull.Value;
                }*/
                else if (dcc.Contains("hdn" + s))
                {
                    value = mapping["hdn" + s] ?? DBNull.Value;
                }
                else if (dcc.Contains("dtl_" + s))
                {
                    value = mapping["dtl_" + s] ?? DBNull.Value;
                }
                qp.ParameterValue = value;                
            }
            return rqp;    
        }
    }
}
