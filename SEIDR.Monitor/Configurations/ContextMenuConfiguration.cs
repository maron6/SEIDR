using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Ryan_UtilityCode.Configuration
{
    
    public class ContextMenuItem:ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired=true)]
        public string Name
        {
            get
            {
                return this["name"] as string;
            }
        }
        [ConfigurationProperty("owner", DefaultValue=null)]
        public string Owner
        {
            get
            {
                return this["owner"] as string;
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
        [ConfigurationProperty("Parameter1", DefaultValue=null)]
        public string Parameter1
        {
            get
            {
                string s = this["Parameter1"] as string;
                if (s != null)
                    s = s.Trim();
                return s == ""? null: s;
            }
        }
        [ConfigurationProperty("Parameter1Type", DefaultValue = null)]
        public string Parameter1Type
        {
            get
            {
                string s = this["Parameter1Type"] as string;
                s = s ?? "";
                s = s.Trim();
                return s == "" ? "string" : s;
            }
        }
        [ConfigurationProperty("Parameter2", DefaultValue = null)]
        public string Parameter2
        {
            get
            {
                string s = this["Parameter2"] as string;
                if (s != null)
                    s = s.Trim();
                return s == "" ? null : s;
            }
        }
        [ConfigurationProperty("Parameter2Type", DefaultValue = null)]
        public string Parameter2Type
        {
            get
            {
                string s = this["Parameter2Type"] as string;
                s = s ?? "";
                s = s.Trim();
                return s == "" ? "string" : s;
            }
        }        
        [ConfigurationProperty("Parameter3", DefaultValue = null)]
        public string Parameter3
        {
            get
            {
                string s = this["Parameter3"] as string;
                if (s != null)
                    s = s.Trim();
                return s == "" ? null : s;
            }
        }
        [ConfigurationProperty("Parameter3Type", DefaultValue = null)]
        public string Parameter3Type
        {
            get
            {
                string s = this["Parameter3Type"] as string;
                s = s ?? "";
                s = s.Trim();
                return s == "" ? "string" : s;
            }
        }
        [ConfigurationProperty("Paramter1Value", DefaultValue = null)]
        public object Parameter1Value
        {
            get
            {
                return this["Parameter1Value"] as object;
            }
        }
        [ConfigurationProperty("Paramter2Value", DefaultValue = null)]
        public object Parameter2Value
        {
            get
            {
                return this["Parameter2Value"] as object;
            }
        }
        [ConfigurationProperty("Paramter3Value", DefaultValue = null)]
        public object Parameter3Value
        {
            get
            {
                return this["Parameter3Value"] as object;
            }
        }
    }
    public class ContextMenuItems : ConfigurationElementCollection
    {
        public ContextMenuItem this[int index]
        {
            get
            {
                return base.BaseGet(index) as ContextMenuItem;
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
        public new ContextMenuItem this[string responseString]
        {
            get { return (ContextMenuItem)BaseGet(responseString); }
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
            return new ContextMenuItem();
        }
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ContextMenuItem)element).Name;
        }
    }
    public class RegisterMenusConfig : ConfigurationSection
    {
        public static RegisterMenusConfig GetConfig()
        {
            return (RegisterMenusConfig)System.Configuration.ConfigurationManager.GetSection("ContextMenuItems") ?? new RegisterMenusConfig();
        }
        [System.Configuration.ConfigurationProperty("ContextMenuItems")]
        [ConfigurationCollection(typeof(ContextMenuItems), AddItemName = "ContextMenuItem")]
        public ContextMenuItems ContextMenuItems
        {
            get
            {
                object temp = this["ContextMenuItems"];
                return temp as ContextMenuItems;
            }
        }
    }
}
