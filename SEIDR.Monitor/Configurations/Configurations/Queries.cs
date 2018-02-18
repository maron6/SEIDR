using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Ryan_UtilityCode.Dynamics.Configurations
{
    public sealed class Queries:IEnumerable<Query>, iConfigList
    {
        public List<Query> QueryList = new List<Query>();
        public void Add(Query q)
        {
            QueryList.Add(q);
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
        public System.Data.DataTable MyData
        {
            get 
            {
                return QueryList.ToArray().ToDataTable();
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
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Name { get; set; }
        public string ProcedureCall { get; set; }
        public string FromDateParam { get; set; }
        public string ThroughDateParam { get; set; }
        public string ActiveParam { get; set; }
        public string ExtraFilter { get; set; }
        public string IntParam1 { get; set; }
        public string IntParam2 { get; set; }
        public string DBConnectionName { get; set; }
        short? _RefreshTime = null;
        /// <summary>
        /// Minimum time for the query to auto refresh after selecting. Might not be used.
        /// </summary>
        public short? RefreshTime
        {
            get { return _RefreshTime; }
            set
            {
                if (value < 0)
                    _RefreshTime = 0;
                else
                    _RefreshTime = value;
            }
        }
    }
}
