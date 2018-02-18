using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Dynamics.Configurations.UserConfiguration
{
    public class Team : iWindowConfiguration
    {
        public bool Altered { get; set; }
        public string Description { get; set; }
        public int? ID { get; set; }
        public string Key { get; set; }                
        public WindowConfigurationScope MyScope
        {
            get
            {
                return WindowConfigurationScope.TM;
            }
        }
        public int RecordVersion { get; set; }
    }
}
