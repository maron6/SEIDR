using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Ryan_UtilityCode.Configuration
{
    
    public class Query:ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired=true)]
        public string Name
        {
            get
            {
                return this["name"] as string;
            }
        }
        [ConfigurationProperty("procName", IsRequired=true)]
        public string ProcedureName
        {
            get
            {
                return this["procName"] as string;
            }
        }
        //[ConfigurationProperty("hasFromDate", DefaultValue=false)]
        //public bool HasFromDate
        //{
        //    get
        //    {
        //        try
        //        {
        //            return Convert.ToBoolean(this["hasFromDate"]);
        //        }
        //        catch { return false; }
        //    }
        //}
        [ConfigurationProperty("FromDate", DefaultValue=null)]
        public string FromDate
        {
            get
            {
                string s = this["FromDate"] as string;
                if (s != null)
                    s = s.Trim();
                return s == ""? null: s;
            }
        }
        [ConfigurationProperty("ThroughDate", DefaultValue= null)]
        public string ThroughDate
        {
            get
            {                
                //return this["ThroughDate"] as string;
                string s = this["ThroughDate"] as string;
                if (s != null)
                    s = s.Trim();
                return s == "" ? null : s;
            }
        }
        [ConfigurationProperty("ActiveFilter", DefaultValue=null)]
        public string ActiveParm
        {
            get
            {
                //return this["ActiveFilter"] as string;
                string s = this["ActiveFilter"] as string;
                if (s != null)
                    s = s.Trim();
                return s == "" ? null : s;
            }
        }
        [ConfigurationProperty("DBConnection", DefaultValue=null)]
        public string DBConnection
        {
            get
            {
                string s = this["DBConnection"] as string;
                if (s != null)
                    s = s.Trim();
                return (s == "" ? null : s)??"Default";
            }
        }
    }
    public class Queries : ConfigurationElementCollection
    {
        public Query this[int index]
        {
            get
            {
                return base.BaseGet(index) as Query;
            }
            set
            {
                if (base.BaseGet(index) != null)
                {
                    base.BaseRemoveAt(index);
                }
                this.BaseAdd(index, value);
            }
        }
        public new Query this[string responseString]
        {
            get { return (Query)BaseGet(responseString); }
            set
            {
                if (BaseGet(responseString) != null)
                {
                    base.BaseRemove(responseString);
                }
                BaseAdd(value);
            }
        }
        protected override ConfigurationElement CreateNewElement()
        {
            return new Query();
        }
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((Query)element).Name;
        }
    }
    public class RegisterQueriesConfig : ConfigurationSection
    {
        public static RegisterQueriesConfig GetConfig()
        {
            return (RegisterQueriesConfig)System.Configuration.ConfigurationManager.GetSection("Queries") ?? new RegisterQueriesConfig();
        }
        [System.Configuration.ConfigurationProperty("Queries")]
        [ConfigurationCollection(typeof(Queries), AddItemName = "Query")]
        public Queries Queries
        {
            get
            {
                object temp = this["Queries"];
                return temp as Queries;
            }
        }
    }
}
