using System.Linq;

namespace Ryan_UtilityCode.Dynamics.Configurations
{
    public class BasicUser
    {
        public int ID { get; set; }
        public string UserName { get; set; }
        public ushort? AdminLevel { get; set; }
        public bool CanEditSettings { get; set; }
        public bool CanOpenDashboard { get; set; }
        public string[] AddonPermissions { get; set; }
        public string Team { get; set; }
        public bool SuperAdmin { get { return AdminLevel.HasValue && AdminLevel.Value <= 1; } }

        public bool BasicValidate(string AddonName = null, string AddonTeam = null)
        {
            if (SuperAdmin)
                return true;
            if(AddonName == null)
            {
                if (AddonTeam == null)
                    return true;
                return Team == AddonTeam;
            }
            if(AddonTeam == null)
            {
                if (AddonName == null)
                    return true;
                return AddonPermissions.Contains(AddonName);
            }
            return Team == AddonTeam && AddonPermissions.Contains(AddonName);
        }
    }
}
