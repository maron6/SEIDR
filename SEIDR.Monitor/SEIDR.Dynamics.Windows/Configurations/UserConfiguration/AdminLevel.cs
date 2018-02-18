using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Dynamics.Configurations.UserConfiguration
{
    public class AdminLevel : iWindowConfiguration
    {
        public bool Altered { get; set; }

        public string Description { get; set; }

        public int? ID { get; set; }

        public string Key { get; set; }

        public WindowConfigurationScope MyScope
        {
            get
            {
                return WindowConfigurationScope.ADML;
            }
        }        

        public int RecordVersion { get; set; }
        public static IEnumerable<AdminLevel> GetLevels(BasicUser u, short maxValue)
        {
            List<AdminLevel> rc = new List<AdminLevel>();
            rc.Add( new AdminLevel { ID = null, Key = "No Administration Level" });
            if (u.AdminLevel == null || u.AdminLevel.Value >= maxValue)
                return rc;
            for(short t = u.AdminLevel.Value; t < maxValue; t++)
            {
                rc.Add(new AdminLevel { ID = t, Key = "Level " + t });
            }
            return rc;
        }
    }
}
