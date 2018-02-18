using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Ryan_UtilityCode.Configuration
{

    public class DBConnection : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get
            {
                return this["name"] as string;
            }
        }
        [ConfigurationProperty("Server", IsRequired = true)]
        public string Server
        {
            get
            {
                return this["Server"] as string;
            }
        }
        [ConfigurationProperty("Catalog", IsRequired = true, DefaultValue="DataServices")]
        public string Catalog
        {
            get
            {
                return this["Catalog"] as string;
            }
        }
    }
    public class DBConnections : ConfigurationElementCollection
    {
        public DBConnection this[int index]
        {
            get
            {
                return base.BaseGet(index) as DBConnection;
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
        public new DBConnection this[string responseString]
        {
            get { return (DBConnection)BaseGet(responseString); }
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
            return new DBConnection();
        }
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((DBConnection)element).Name;
        }
    }
    public class RegisterConnectionsConfig : ConfigurationSection
    {
        public static RegisterConnectionsConfig GetConfig()
        {
            return (RegisterConnectionsConfig)System.Configuration.ConfigurationManager.GetSection("DBConnections") ?? new RegisterConnectionsConfig();
        }
        [System.Configuration.ConfigurationProperty("DBConnections")]
        [ConfigurationCollection(typeof(DBConnections), AddItemName = "DBConnection")]
        public DBConnections DBConnections
        {
            get
            {
                object temp = this["DBConnections"];
                return temp as DBConnections;
            }
        }
    }
}
