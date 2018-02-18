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
    public class ContextAddonConfiguration : iWindowConfiguration
    {
        public bool Altered { get; set; }

        public string Description { get; set; }

        public int? ID { get; set; }

        public string Key { get; set; }

        public WindowConfigurationScope MyScope
        {
            get
            {
                return WindowConfigurationScope.ACM;
            }
        }

        public int RecordVersion { get; set; }
        [LookupSource(WindowConfigurationScope.Q)]
        [LookupSource(WindowConfigurationScope.CM)]
        [LookupSource(WindowConfigurationScope.ACM)]
        [LookupSource(WindowConfigurationScope.D)]
        public int? Parent { get; set; }


        [DefaultValue(null)]
        //[WindowConfigurationEditorElementInfo("Parameters", 2)]
        public List<ParameterInfo> ParameterInfo { get; set; }
        [XmlIgnore]
        public Dictionary<string, object> Parameters
        {
            get
            {
                return ContextMenuConfiguration.ParameterInfo.ToDictionary(ParameterInfo);
            }
        }
        public WindowConfigurationScope ParentScope { get; set; }
        

        [XmlIgnore]
        public string AppName { get { return Key; } set { Key = value; } }

        public string Guid { get; set; }
    }
}
