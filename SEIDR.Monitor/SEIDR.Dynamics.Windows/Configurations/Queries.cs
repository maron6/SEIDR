using SEIDR.Dynamics.Configurations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Xml.Serialization;

namespace SEIDR.Dynamics.Configurations
{
    public sealed class Queries:IEnumerable<Query>, iConfigList
    {        
        public List<Query> QueryList = new List<Query>();
        public void Add(Query q)
        {
            QueryList.Add(q);
        }
        public Queries()        
        {
            Version = new Guid();
        }
        
        [XmlIgnore]
        public Query this[string idx]
        {
            get
            {
                foreach (var q in QueryList)
                {
                    if (q.Name == idx)
                        return q;
                }
                return null;
            }
            set
            {
                for (int i = 0; i < QueryList.Count; i++)
                {
                    Query q = QueryList[i];
                    if (q.Name == idx)
                    {
                        QueryList[i] = value;
                        return;
                    }
                }
                QueryList.Add(value);
            }
        }
        public void Remove(string toRemove)
        {
            foreach (Query q in QueryList)
            {
                if (q.Name == toRemove)
                {
                    QueryList.Remove(q);
                    return;
                }
            }
        }
        public void Remove(Query toRemove)
        {
            QueryList.Remove(toRemove);
        }        
        public IEnumerator<Query> GetEnumerator()
        {
            return QueryList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return QueryList.GetEnumerator();
        }
        public List<string> ToStringList(bool AddNew = true)
        {
            List<string> ret = new List<string>();
            if (AddNew)
            {
                ret.Add("(New Query)");
            }
            foreach (var q in QueryList)
            {
                ret.Add(q.Name);
            }
            return ret;
        }
        public List<string> GetNameList()
        {
            List<string> rl = new List<string>();
            foreach (var q in QueryList)
            {
                rl.Add(q.Name);
            }
            return rl;
        }
        public int GetIndex(string idx, bool IncludeNew = true)
        {
            for (int i = 0; i < QueryList.Count; i++)
            {
                if (QueryList[i].Name == idx)
                {
                    return i + (IncludeNew? 1:0);
                }
            }
            return -1;
        }
        
        [XmlIgnore]
        public DataTable MyData
        {
            get 
            {                
                return QueryList.ToArray().ToDataTable("MyName", "GroupedResultColumns", "Parameters",
                    //"EnablePieChart", "EnableFrequencyChart", "EnableAggregateChart",
                    "ExcludedResultColumns",  "RefreshTime" );
                /*
                System.Data.DataTable dt = new System.Data.DataTable();
                dt.Columns.Add("Name", typeof(string));
                dt.Columns.Add("Category", typeof(string));
                dt.Columns.Add("SubCategory", typeof(string));
                dt.Columns.Add("Procedure", typeof(string));
                dt.Columns.Add("From Date Parameter (date)", typeof(string));
                dt.Columns.Add("Through Date Parameter(date)", typeof(string));
                dt.Columns.Add("Active Parameter (bit)", typeof(string));
                dt.Columns.Add("Extra(varchar)", typeof(string));
                dt.Columns.Add("DBConnection", typeof(string));                
                dt.Columns.Add("IntParam1", typeof(string));
                dt.Columns.Add("IntParam2", typeof(string));
                foreach (Query q in this.QueryList)
                {
                    dt.Rows.Add(q.Name, q.Category, q.SubCategory, q.ProcedureCall, q.FromDateParam, q.ThroughDateParam, q.ActiveParam,
                        q.ExtraFilter, q.DBConnectionName, q.IntParam1, q.IntParam2);
                }
                return dt;*/
            }
        }

        public Guid Version
        {
            get;
            set;
        }

        public bool Cloneable
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the list of all distinct categories in use
        /// </summary>
        /// <returns></returns>
        public string[] GetCategories()
        {
            var q = (from query in QueryList
                     select query.Category).Distinct();
            return q.ToArray<string>();
        }
        /// <summary>
        /// Get the list of non-nullable subCategories for the provided Category
        /// </summary>
        /// <param name="Category"></param>
        /// <returns></returns>
        public string[] GetSubCategories(string Category)
        {
            var q = (from query in QueryList
                     let subcat = query.SubCategory
                     where query.Category == Category
                     && subcat != null
                     select subcat).Distinct();
            return q.ToArray();
        }

        public iConfigList cloneSetup()
        {
            Queries clone = new Queries();
            clone.QueryList = new List<Query>(QueryList); //going to point to the same references anyway actually...
            return clone;
            /*foreach(Query q in QueryList)
            {
                clone.Add(q.DClone());
            }*/
        }
        /* 
public Dictionary<string, string[]> GetCombos()
{
   var q = (from query in QueryList
            group query by query.Category into Combos
            select Combos); // as-is: returns a list of all the categories with a link to every query in the category...
   Dictionary<string, string[]> combos = new Dictionary<string, string[]>();
   foreach(var group in q)
   {

       string[] x = new string[group.Count()];
       int c = 0;
       foreach(var categoryCombo in group)
       {
           x[c++] = categoryCombo.SubCategory;
       }
   }

   return combos;            
}*/
    }
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
        public string OverrideColumnName { get
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
            foreach(var p in builtParams)
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
            foreach(QueryParameter qp in values)
            {
                if (cols.Contains(qp.ParameterName))
                {
                    qp.ParameterValue = editedValues[qp.ParameterName];
                }
                if(cols.Contains(qp.OverrideColumnName))
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
            foreach(var qp in parameters)
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
    }
    
    /// <summary>
    /// Query class - used by SEIDR.Window to describe queries for the query menu
    /// </summary>
    public sealed class Query
    {
        public bool NeedsParameterEvaluation()
        {
            var checks = new string[] //Meta data that is not a parameter and does not need to be included when calling a procedure
            {
                "MyName", "DisplayName","Name",
                "Category", "SubCategory",
                "ProcedureCall", "DBConnectionName", "RefreshTime"
            };
            var props = typeof(Query).GetProperties();
            foreach(var prop in props)
            {
                if (checks.Contains(prop.Name))
                    continue;
                if (prop.GetValue(this) != null)
                    return true;
            }
            return false;
        }
        public Query()
        {
            Name = ConfigListHelper.GetIDName(ConfigListHelper.Scope.Q);

        }

        [XmlIgnore]
        public string MyName
        {
            get
            {
                if (DisplayName != null)
                    return DisplayName;
                if (Name.ToUpper().StartsWith("Q_"))
                    return Name.Substring(2);
                return Name;
            }
        }
        public string DisplayName { get; set; }
        [DefaultValue(null)]
        public string Category { get; set; }
        [DefaultValue(null)]
        public string SubCategory { get; set; }
        public string Name { get; set; }
        public string ProcedureCall { get; set; }

        #region obsolete, to be replaced by Parameters
        /*
        public string FromDateParam { get; set; }
        public string ThroughDateParam { get; set; }
        public string ActiveParam { get; set; }
        public string ExtraFilter { get; set; }
        public string IntParam1 { get; set; }
        public string IntParam2 { get; set; }
        */
        #endregion
        /// <summary>
        /// If null or empty, will run without Setting any parameter values.
        /// </summary>
        [DefaultValue(null)]
        public QueryParameter[] Parameters { get; set; } = null;
        /// <summary>
        /// Gets a copy of the query parameters for use at run time to avoid changing the values stored with the query.
        /// </summary>
        /// <returns></returns>
        
        public QueryParameter[] GetParametersForRunTime()
        {
            if (Parameters == null || Parameters.Length == 0)
                return null;
            QueryParameter[] ret = new QueryParameter[Parameters.Length];
            for(int i= 0; i < ret.Length; i++)
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
        [DefaultValue("Default")]
        public string DBConnectionName { get; set; } = "Default";
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
