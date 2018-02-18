using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Dynamics.Configurations
{
    class EmptyWindowConfiguration : iWindowConfiguration
    {
        public bool Altered { get; set; }

        public string Description
        {
            get { return null; }
            set { }
        }

        public int? ID
        {
            get
            {
                return null;
            }

            set
            {                
            }
        }

        public string Key
        {
            get
            {
                return "None";
            }

            set
            {                
            }
        }

        public WindowConfigurationScope MyScope
        {
            get
            {
                return WindowConfigurationScope.UNK;
            }
        }

        public int RecordVersion { get; set; }
    }
}
