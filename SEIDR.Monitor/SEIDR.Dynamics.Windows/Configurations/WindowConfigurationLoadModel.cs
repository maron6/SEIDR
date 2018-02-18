using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Dynamics.Configurations
{
    /// <summary>
    /// Contains information about the way the Configuration list was loaded.
    /// <para>Mainly - TeamID and UserSpecific mode (If not user specific mode, a null TeamID is default team)</para>
    /// <para>User specific will not be set if teamID is set.</para>
    /// <para>Tag and key can be set as needed to help with saving or re-loading later on</para>
    /// </summary>
    public class WindowConfigurationLoadModel
    {
        public int? TeamID { get; set; } = null;
        public bool UserSpecific { get; set; } = false;
        public string Key { get; set; } = null;
        public object Tag { get; set; }
        /// <summary>
        /// Shallow clone.
        /// </summary>
        /// <returns></returns>
        public WindowConfigurationLoadModel CloneSimple()
        {
            return new WindowConfigurationLoadModel
            {
                TeamID = TeamID,
                UserSpecific = UserSpecific,
                Key = Key,
                Tag = Tag
            };
        }
    }
}
