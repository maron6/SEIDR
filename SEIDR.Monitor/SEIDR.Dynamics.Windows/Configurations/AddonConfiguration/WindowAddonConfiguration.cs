using SEIDR.Dynamics.Configurations.ContextMenuConfiguration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SEIDR.Dynamics.Configurations.AddonConfiguration
{
    public sealed class WindowAddonConfiguration: iWindowConfiguration
    {
        public bool Altered { get; set; }
        
        public string Description { get; set; }

        [WindowConfigurationEditorIgnore]
        [XmlIgnore]
        public string InternalName { get { return MyScope.ToString() + ID; } }
        public int? ID { get; set; }

        [LookupSource(WindowConfigurationScope.DB)]
        public int? DatabaseID { get; set; }
        public string Key { get; set; }

        public WindowConfigurationScope MyScope
        {
            get
            {
                return WindowConfigurationScope.A;
            }
        }
        public string AddonName { get; set; }
        [XmlIgnore]
        public bool IsConfigurationSingleton { get; set; }
        public int RecordVersion { get; set; }

        [XmlIgnore]
        public DateTime? NextCallback { get; set; } = null;
        public List<ParameterInfo> ParameterInfo { get; set; }
        [XmlIgnore]
        public Dictionary<string, object> Parameters
        {
            get
            {
                Dictionary<string, object> temp = new Dictionary<string, object>();
                foreach (var info in ParameterInfo)
                {
                    //temp.Add(info.Key, info.Value);
                    temp[info.Key] = info.Value;
                }
                return temp;
            }
        }
        [XmlIgnore]
        public object this[string Key]
        {
            get
            {
                return (from param in ParameterInfo
                         where param.Key == Key
                         select param.Value).FirstOrDefault();                
            }
            set
            {
                ParameterInfo p = new ParameterInfo { Key = Key, Value = value };
                var existing = (from param in ParameterInfo
                                where param.Key == Key
                                select param).FirstOrDefault();
                if (existing == null)
                    ParameterInfo.Add(p);
                else
                {
                    ParameterInfo.Remove(existing);
                    ParameterInfo.Add(p);
                }
            }
        }
        
    }
}
