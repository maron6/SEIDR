using System.ComponentModel;
using System.Linq;

namespace SEIDR.Dynamics.Configurations.UserConfiguration
{
    /// <summary>
    /// Exposes the SEIDR.Window user representation for SEIDR.Window plugins
    /// </summary>
    public abstract class BasicUser
    {

        /// <summary>
        /// User's ID, set by Project that implements the concrete User. May be null if the user is impersonating a team or was auto created as a default user 
        /// </summary>
        public abstract int? UserID { get; }
        /// <summary>
        /// User's login domain
        /// </summary>
        public abstract string Domain { get; }
        /// <summary>
        /// User name, expected to match Environment.UserName
        /// </summary>
        public abstract string UserName { get; }

        public short? AdminLevel { get; protected set; }
        public BasicUserPermissions MyPermissions { get; set; }
        public const BasicUserPermissions BasicPermissionSet
            = BasicUserPermissions.QueryEditor
            | BasicUserPermissions.DatabaseConnectionEditor
            //| BasicUserPermissions.MiscSettings
            | BasicUserPermissions.CanExportData
            | BasicUserPermissions.ContextMenuEditor
            | BasicUserPermissions.AddonUser;
        
        public const BasicUserPermissions AdminPermissions
            = BasicPermissionSet
            | BasicUserPermissions.TeamSettingEditor
            | BasicUserPermissions.UserEditor;

        public const BasicUserPermissions SuperAdminPermissions
            = AdminPermissions
            | BasicUserPermissions.SessionCache            
            | BasicUserPermissions.TeamEditor
            | BasicUserPermissions.AddonEditor;
        /// <summary>
        /// If true, user has permission to impersonate users with no or lower(>) admin level.
        /// </summary>
        public bool CanImpersonateUsers
        {
            get
            {
                return CheckPermission(BasicUserPermissions.CanImpersonate);
            }
        }

        public bool CanUseAddons
        { 
            get
            {
                return CheckPermission(BasicUserPermissions.AddonUser);
            }
        }
        /// <summary>
        /// True if user is considered an Admin. Determined by having all basic permissions.
        /// </summary>
        public bool Admin
        {
            get
            {
                return AdminLevel.HasValue 
                    && (MyPermissions & AdminPermissions) != BasicUserPermissions.None;
            }
        }
        public bool SuperAdmin
        {
            get
            {
                return AdminLevel.HasValue
                    && (MyPermissions & SuperAdminPermissions) != BasicUserPermissions.None;
            }
        }
        /// <summary>
        /// Can edit settings in SEIDR.Window
        /// </summary>
        public bool CanEditSettings
        {
            get
            {
                return (MyPermissions & BasicUserPermissions.AddonEditor) 
                    != BasicUserPermissions.None;
            }
        }

        /// <summary>
        /// List of addons that the user has permission for
        /// </summary>
        public abstract string[] AddonPermissions { get; }        
        /// <summary>
        /// Team the user is associated with
        /// </summary>
        public abstract string Team { get; }        
        /// <summary>
        /// Main validation used for SEIDR addons/plugins.
        /// </summary>
        /// <param name="RequireSessionCache">If true, requires that the user has permission to use session cache for session manager<para>
        /// This is checked because session cache allows sharing data between plugins</para></param>
        /// <param name="requirePermission">If true, checks taht the user has been given permission to use the provided AddonName (should match meta data)</param>
        /// <param name="AddonName">If not null, will check against the Addon's teams</param>
        /// <param name="AddonTeam">If not null, will check against the AddonPermissions list</param>
        /// <returns>True if the user is allowed to use the addon/plugin</returns>
        public bool BasicValidate(bool RequireSessionCache, bool requirePermission, string AddonName = null, string AddonTeam = null)
        {
            if (SuperAdmin)
                return true;
            if (RequireSessionCache && !CanUseCache)
                return false;           
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
                return !requirePermission || AddonPermissions.Contains(AddonName);
            }
            
            return Team == AddonTeam && (!requirePermission || AddonPermissions.Contains(AddonName));
        }
        /// <summary>
        /// If true, you can put items into the UserSession cache when inheriting from a BasicSessionWindow.<para>
        /// If false, the cache value will always end up being null</para>
        /// <para>Also, note that the cache is shared between plugins and also the main seidr application</para>
        /// </summary>
        public bool CanUseCache
        {
            get
            {
                return (MyPermissions & BasicUserPermissions.SessionCache) != BasicUserPermissions.None;
            }
        }
        /// <summary>
        /// Check that the user has the full permission.
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        public bool CheckPermission(BasicUserPermissions check)
        {
            return (MyPermissions & check) == check;
        }
        /// <summary>
        /// check that the user has all listed permissions
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        public bool CheckPermission(params BasicUserPermissions[] check)
        {
            BasicUserPermissions toCheck = BasicUserPermissions.None;
            foreach(var c in check)
            {
                toCheck |= c;
            }
            return CheckPermission(toCheck);
        }
        /// <summary>
        /// Clone user so that addons cannot edit the stored settings. Default is XML Serialization
        /// </summary>
        /// <returns></returns>
        public virtual BasicUser Clone()
        {
            return this.XClone();
        }
    }

    [System.Flags]
    public enum BasicUserPermissions
    {
        [WindowConfigurationEditorIgnore]
        None =0,
        [Description("Can Edit Local DatabaseConnections")]
        DatabaseConnectionEditor = 1,
        [Description("Can Edit Local Queries.")]
        QueryEditor = 2,
        [Description("Can Edit local Context menu settings. Requires local queries to go into effect.")]
        ContextMenuEditor=4,
        [Description("Can Edit local Plugin settings")]
        AddonEditor = 8,
        [Description("Can export DataGrid results to files (Note: Export could also be implemented by addons..)")]
        CanExportData = 16,
        //MiscSettings =16,
        [Description("Can edit the available Teams")]
        TeamEditor=32,
        [Description("Can edit the team versions of Queries/Menus/etc")]
        TeamSettingEditor =64,   
        [Description("Can add and edit non admin users or users with lower administration levels")]
        UserEditor=128,
        [Description("Can use caching functionality in Plugins")]
        SessionCache=256,
        [Description("Can use plugins")]
        AddonUser = 512,
        [Description("Can use impersonate other users. Must be implemented in ")]
        CanImpersonate = 1024,
        [Description("Can throttle context menu commands with a Queue")]
        ContextQueue = 2048,
        //CanExportData = 2048
    }
}
