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
    public class Query : iWindowConfiguration
    {
        [XmlIgnore]
        public bool Altered { get; set; }
        /*
        "MyName", "DisplayName","Name",
                "Category", "SubCategory",
                "ProcedureCall", "DBConnectionName", "RefreshTime"*/
        public string Description { get; set; }

        /// <summary>
        /// Numeric identifier
        /// </summary>
        public int? ID { get; set; }

        /// <summary>
        /// User key identifier.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Scope of configuration object: Q
        /// </summary>
        [XmlIgnore]
        public WindowConfigurationScope MyScope => WindowConfigurationScope.Q;
        //{ get { return WindowConfigurationScope.Q; } }

        /// <summary>
        /// Useable for broker management
        /// </summary>
        [DefaultValue(0)]
        public int RecordVersion { get; set; }

        /// <summary>
        /// Identify database connection to be used.
        /// </summary>
        public int? DBConnection { get; set; }

        /// <summary>
        /// If null or empty, will run without Setting any parameter values.
        /// </summary>
        [DefaultValue(null)]
        public QueryParameter[] Parameters { get; set; } = null;
        [DefaultValue(null)]
        public string Category { get; set; }
        [DefaultValue(null)]
        public string SubCategory { get; set; }
        public string ProcedureCall { get; set; }


        public QueryParameter[] GetParametersForRunTime()
        {
            if (Parameters == null || Parameters.Length == 0)
                return null;
            QueryParameter[] ret = new QueryParameter[Parameters.Length];
            for (int i = 0; i < ret.Length; i++)
            {
                var qp = Parameters[i];
                ret[i] = new QueryParameter
                {
                    ParameterName = qp.ParameterName,
                    ParameterType = qp.ParameterType,
                    ParameterValue = qp.ParameterValue,
                    OverrideRunTime = qp.OverrideRunTime
                };
            }
            return ret;
        }


        /// <summary>
        /// Allow overriding logic executing commands for the database configurations
        /// </summary>
        /// <param name="db">The database configuration</param>
        /// <param name="configuredParameters">Parameters configured by user as of run time.</param>        
        /// <param name="UserID">Id of executing user, if used for procedures.<para>
        /// Default: adds as key 'SWID' to the DatabaseManager model's parameters</para> </param>
        /// <returns></returns>
        public virtual DataSet Execute(DatabaseConfiguration.Database db, 
            QueryParameter[] configuredParameters, int? UserID)
        {
            var dm = db.Manager;      
            using (var m = dm.GetBasicHelper(QueryParameter.ToDictionary(configuredParameters), UnqualifiedProcedureName:null))
            {                
                m.AddKey("SWID", ID);
                m.QualifiedProcedure = this.ProcedureCall;
                return dm.Execute(m);
            }

        }
        /// <summary>
        /// Determine whether we can look at a single row view as a pie chart in SEIDR.Window
        /// </summary>
        [DefaultValue(false)]
        public bool EnablePieChart { get; set; } = false;
        [DefaultValue(false)]
        public bool EnableFrequencyChart { get; set; } = false;
        [DefaultValue(false)]
        public bool EnableAggregateChart { get; set; } = false;
        [DefaultValue(null)]
        public string[] GroupedResultColumns { get; set; } = null;
        [DefaultValue(null)]
        public string[] ExcludedResultColumns { get; set; } = null;
        [XmlIgnore]
        short? _RefreshTime = null;
        /// <summary>
        /// Minimum time for the query to auto refresh after selecting. If null, don't refresh automatically
        /// </summary>
        [DefaultValue(null)]
        public short? RefreshTime
        {
            get { return _RefreshTime; }
            set
            {
                if (value == null)
                    _RefreshTime = value;
                else if (value < 0)
                    _RefreshTime = 0;
                else
                    _RefreshTime = value;
            }
        }        

        [XmlIgnore]
        public DateTime LastRunTime;
        
    }
}
