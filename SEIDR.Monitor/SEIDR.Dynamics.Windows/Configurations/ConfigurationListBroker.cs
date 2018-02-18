using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Configurations.UserConfiguration;
using SEIDR.Dynamics.Configurations.Encryption;
using SEIDR.Dynamics.Configurations.ContextMenuConfiguration;
using SEIDR.Dynamics.Configurations.QueryConfiguration;
using SEIDR.Dynamics.Configurations.DatabaseConfiguration;
using SEIDR.Dynamics.Configurations.AddonConfiguration;
using SEIDR.Dynamics.Configurations.DynamicEditor;
using SEIDR;

namespace SEIDR.Dynamics.Configurations
{    
    public abstract class ConfigurationListBroker
    {
        #region constants
        protected const string QUERIES_FILE = "Queries.XML";
        protected const string CONTEXTMENU_FILE = "ContextMenus.XML";
        protected const string DATABASECONNECTION_FILE = "DatabaseConnections.XML";
        protected const string CONTEXT_ADDONS_FILE = "ContextAddons.XML";
        protected const string WINDOW_ADDONS_FILE = "WindowAddons.XML";

        protected const string NETWORK_QUERIES_FILE = "Queries.SCRL";
        protected const string NETWORK_CONTEXTMENU_FILE = "ContextMenus.SCRL";
        protected const string NETWORK_DATABASECONNECTION_FILE = "DatabaseConnections.SCRL";
        protected const string NETWORK_CONTEXT_ADDONS_FILE = "ContextAddons.SCRL";
        protected const string NETWORK_WINDOW_ADDONS_FILE = "WindowAddons.SCRL";
        protected const string TEAM_FILE = "Teams.SCRL";
        protected const string USER_FILE = "Users.SCRL";
        #endregion
        public short MaxAdminLevel { get; private set; } = 7;
        /// <summary>
        /// used by default load/save model
        /// </summary>
        public string BasePath { get; private  set; }
        /// <summary>
        /// used by default load/save model
        /// </summary>
        public string NetworkPath { get; private set; }
        /// <summary>
        /// Called for logging. <para>
        /// Can be overridden in <see cref="ConfigurationListBroker.PostLoginSetup"/> to a subclass that overrides the handle methods for more control.</para>
        /// </summary>
        public ExceptionManager MyExceptionManager { get; protected set; }
        /// <summary>
        /// Constructor for the configuration list broker to be used by SEIDR.Window
        /// <para>Ignore the paths if using a DB specific implementation rather than file based storage</para>
        /// </summary>
        /// <param name="maxAdminLevel">Sets the maximum admin level when editing users.</param>
        public ConfigurationListBroker(short maxAdminLevel) { MaxAdminLevel = maxAdminLevel; }
        /// <summary>
        /// Default constructor. Sets MaxAdminLevel to 7
        /// </summary>
        public ConfigurationListBroker() { MaxAdminLevel = 7; }
        /// <summary>
        /// Basic set up... Call the
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="networkPath"></param>
        /// <param name="exmanager"></param>
        public void Setup(string basePath, string networkPath, ExceptionManager exmanager)
        {
            BasePath = basePath;
            NetworkPath = networkPath;
            SetExceptionManager(exmanager);         
        }
        /// <summary>
        /// Sets the exception manager. Can be called from application if 
        /// </summary>
        /// <param name="exManager"></param>
        public virtual void SetExceptionManager(ExceptionManager exManager)
            => MyExceptionManager = exManager;
        /// <summary>
        /// An overridable call to set up - can be used to perform any setting changes needed. 
        /// <para>Called just after the user has logged in and Configureduser should be populated.</para>
        /// <para>Base version does nothing.</para>
        /// </summary>
        public virtual void PostLoginSetup() { }
        
        /// <summary>
        /// Calls load on all the configuration lists after the user has logged in.
        /// <para>Calls for the user's team or the User's custom one depending on permissions and availability.</para>
        /// <para>If overloading the individual loads, you should also inherit from the the configuration type and override its update/add/remove/save methods</para>
        /// </summary>
        public void LoadConfigurations(bool preLogin, bool singleUserMode)
        {
            WindowConfigurationLoadModel basic = new WindowConfigurationLoadModel();
            if (preLogin)
            {
                Users = LoadUsers(true, ref basic);
                Users.LoadModel = basic;
                return;
            }
            BasicUserPermissions permission = ConfiguredUser.MyPermissions;
            if (singleUserMode)
            {
                Users = new WindowUserCollection();
                Users.ConfigurationEntries.Add(ConfiguredUser);
                Teams = new TeamList();
            }
            else
            {
                if (!ConfiguredUser.CheckPermission(BasicUserPermissions.UserEditor))                
                    Users = new WindowUserCollection();                                    
                else
                    Users = LoadUsers(false, ref basic) ?? new WindowUserCollection();
                if (!Users.Contains(ConfiguredUser.Key))
                    Users.ConfigurationEntries.Add(ConfiguredUser); //Configured user should always be in the list
                Users.LoadModel = basic;
                basic = new WindowConfigurationLoadModel();
                if (!ConfiguredUser.CheckPermission(BasicUserPermissions.TeamEditor))
                    basic.TeamID = ConfiguredUser.TeamID;
                if (BasicUserPermissions.None !=
                        (permission
                        & (BasicUserPermissions.TeamEditor
                            | BasicUserPermissions.TeamSettingEditor
                            | BasicUserPermissions.UserEditor)
                        )
                    )
                    Teams = LoadTeams(ref basic) ?? new TeamList();
                else if (ConfiguredUser.TeamID.HasValue)
                {
                    Teams = LoadTeams(ref basic);
                    Teams.ConfigurationEntries = Teams
                        .ConfigurationEntries
                        .Where(ltm => ltm.ID == ConfiguredUser.TeamID)
                        .ToList();
                }
                else
                    Teams = new TeamList();
                Teams.LoadModel = basic;

                SetupUserTeamDescriptions();                
            }

            BasicUserPermissions query = BasicUserPermissions.QueryEditor | BasicUserPermissions.DatabaseConnectionEditor;
            BasicUserPermissions context = query | BasicUserPermissions.ContextMenuEditor;
            BasicUserPermissions windowAddon = BasicUserPermissions.DatabaseConnectionEditor | BasicUserPermissions.AddonEditor;
            BasicUserPermissions windowAddonuser = BasicUserPermissions.DatabaseConnectionEditor | BasicUserPermissions.AddonUser;
            BasicUserPermissions contextAddon = context | BasicUserPermissions.AddonEditor;
            BasicUserPermissions contextAddonUser = context | BasicUserPermissions.AddonUser;
            
            WindowConfigurationLoadModel um = new WindowConfigurationLoadModel
            {
                TeamID = null,
                UserSpecific = true
            };
            WindowConfigurationLoadModel tm = new WindowConfigurationLoadModel
            {
                TeamID = ConfiguredUser.TeamID,
                UserSpecific = false
            };            
            Queries = null;
            ContextMenus = null;
            Connections = null;
            ContextAddons = null;
            WindowAddons = null;

            var local = um.CloneSimple();
            var team = tm.CloneSimple();
                
            if (ConfiguredUser.CheckPermission(query))
                Queries = LoadQuery(ref local);
            if (Queries == null)
            {
                Queries = LoadQuery(ref team) ?? new QueryList();
                Queries.LoadModel = team;
                team = tm.CloneSimple();
            }
            else
            {
                Queries.LoadModel = local;
                local = um.CloneSimple();
            }

            
            if (ConfiguredUser.CheckPermission(context))                
                ContextMenus = LoadContextMenus(ref local);
            if (ContextMenus == null)
            {
                ContextMenus = LoadContextMenus(ref team) ?? new ContextMenuList();
                ContextMenus.LoadModel = team;
                team = tm.CloneSimple();
            }
            else
            {
                ContextMenus.LoadModel = local;
                local = um.CloneSimple();
            }            

            if (ConfiguredUser.CheckPermission(BasicUserPermissions.DatabaseConnectionEditor))
                Connections = LoadConnections(ref local);
            if (Connections == null)
            {
                Connections = LoadConnections(ref team) ?? new DatabaseList();
                Connections.LoadModel = team;
                team = tm.CloneSimple();
            }
            else
            {
                Connections.LoadModel = local;
                local = um.CloneSimple();
            }

            
            if (ConfiguredUser.CheckPermission(contextAddon) || ConfiguredUser.CheckPermission(contextAddonUser))            
                ContextAddons = LoadContextAddons(ref local);
            if (ContextAddons == null)
            {
                ContextAddons = LoadContextAddons(ref team) ?? new ContextAddonList();
                ContextAddons.LoadModel = team;
                team = tm.CloneSimple();
            }
            else
            {
                ContextAddons.LoadModel = local;
                local = um.CloneSimple();
            }

            
            if (ConfiguredUser.CheckPermission(windowAddon) || ConfiguredUser.CheckPermission(windowAddonuser))                        
                WindowAddons = LoadWindowAddons(ref local);
            if (WindowAddons == null)
            {
                WindowAddons = LoadWindowAddons(ref team) ?? new WindowAddonList();
                WindowAddons.LoadModel = team;
                team = tm.CloneSimple();
            }
            else
            {
                WindowAddons.LoadModel = local;
                local = um.CloneSimple();
            }
            
        }
        
        #region Configuration loading/saving
        #region save
        //Save should actually be done by the individual concrete type.
        private const string ENC_CK = "SEIDR.WindowMonitorConfiguration";
        //protected void BaseSaveToFile<ConcreteConfig>(WindowConfigurationList<ConcreteConfig> config, bool Encrypt = false)
        //    where ConcreteConfig : iWindowConfiguration
        //{
        //    string content = string.Empty;
        //    string path = (string)config.Tag;
        //    if (File.Exists(path))
        //    {
        //        content = File.ReadAllText(path);
        //        if (Encrypt)
        //            content = content.Decrypt(ENC_CK);
        //        var x = content.DeserializeXML<WindowConfigurationList<ConcreteConfig>>();
        //        if (x != null && x.Version != config.Version)
        //        {
        //            MyExceptionManager.Handle("Configuration for '" + config.ValidationScope.GetDescription() + "' is out of sync. Cannot save.");
        //            return;
        //        }
        //    }
        //    try
        //    {
        //        content = config.SerializeToXML();
        //        if (Encrypt)
        //            content = content.Encrypt(ENC_CK);
        //        File.WriteAllText(path, content);
        //    }
        //    catch(Exception ex)
        //    {
        //        MyExceptionManager.Handle(ex, "Error serializing Configuration for '" + config.ValidationScope.GetDescription() + "'. Cannot save.");
        //    }
        //}
        //protected virtual void SaveQuery(QueryList toSave)
        //{
        //    BaseSaveToFile(toSave);
        //        //string Path = (string)toSave.Tag;
        //        //QueryConfigurationList x = 
        //        //toSave.SerializeToFile((string)toSave.Tag);
        //}
        //public virtual void SaveTeam(TeamList toSave)
        //    => BaseSaveToFile(toSave);
        
        //public virtual void SaveUsers(WindowUserCollection toSave)
        //    => BaseSaveToFile(toSave);
        //public virtual void SaveContextMenu(ContextMenuList toSave)
        //    => BaseSaveToFile(toSave);

        #endregion
        /// <summary>
        /// For use with any file based configuration.
        /// <para>Used for the default file based settings.</para>
        /// </summary>
        protected string TeamFolder
        {
            get
            {
                if (ConfiguredUser.TeamID == null)
                    return Path.Combine(NetworkPath, "DEFAULT");
                else
                    return Path.Combine(NetworkPath, "TEAM_" + ConfiguredUser.TeamID);
            }
        }

        /// <summary>
        /// Populate the Queries configuration
        /// </summary>
        /// <param name="TeamID"></param>
        /// <param name="UserSpecific"></param>
        public virtual QueryList LoadQuery(ref WindowConfigurationLoadModel m)
        {
            string path;
            string content = string.Empty;
            if (m.UserSpecific)
                path = Path.Combine(BasePath, QUERIES_FILE);
            else
                path = Path.Combine(NetworkPath, NETWORK_QUERIES_FILE);

            m.Tag = path;
            m.Key = m.UserSpecific ? null : ENC_CK;                
            try
            {
                content = File.ReadAllText(path);
                QueryList q;
                if (!m.UserSpecific)
                    q = content.Decrypt(ENC_CK).DeserializeXML<QueryList>();
                else
                    q = content.DeserializeXML<QueryList>();
                q.LoadModel = m;
                return q;
            }
            catch(Exception ex)
            {
                MyExceptionManager.Handle(ex, null, ExceptionLevel.UI_Advanced);
                return null;
            }
        }
        public virtual DatabaseList LoadConnections(ref WindowConfigurationLoadModel m)
        {
            string path;
            string content = string.Empty;
            if (m.UserSpecific)
                path = Path.Combine(BasePath, DATABASECONNECTION_FILE);
            else
                path = Path.Combine(NetworkPath, NETWORK_DATABASECONNECTION_FILE);
            m.Tag = path;            
            try
            {
                DatabaseList db;
                content = File.ReadAllText(path);
                if (!m.UserSpecific)
                {
                    db = content.Decrypt(ENC_CK).DeserializeXML<DatabaseList>();
                    m.Key = ENC_CK;
                }
                else db = content.DeserializeXML<DatabaseList>();
                db.LoadModel = m;
                return db;
            }
            catch(Exception ex)
            {
                MyExceptionManager.Handle(ex, null, ExceptionLevel.UI_Advanced);
                return null;
            }
        }
        /// <summary>
        /// Populates <see cref="Users"/> either for editing or use in <see cref="LogIn(LoginInfo)"/>/<see cref="Impersonate(LoginInfo, LoginInfo)"/>.<para>
        /// If overriding validate to check credentials from a database, 
        /// may want to override this to do nothing when <paramref name="preLoad"/> is true.</para>
        /// <para>In that case, <see cref="ValidateCredential(LoginInfo, string)"/> should populate the <see cref="Users"/> with just the validated user.</para>
        /// </summary>
        /// <param name="preLoad">True if loading users for use in the LogIn method</param>
        /// <returns></returns>
        public virtual WindowUserCollection LoadUsers(bool preLoad, ref WindowConfigurationLoadModel m)
        {
            string path = Path.Combine(NetworkPath, USER_FILE);
            m.Key = ENC_CK;
            m.Tag = path;
            if(File.Exists(path))
            {
                try
                {
                    string content = File.ReadAllText(path);
                    return content.Decrypt(ENC_CK).DeserializeXML<WindowUserCollection>();
                }
                catch (Exception ex)
                {
                    MyExceptionManager.Handle(ex, "Could not load users", ExceptionLevel.UI_Advanced);
                    return null;
                }
            }
            return new WindowUserCollection();
        }
        /// <summary>
        /// Loads a team. If team is populated in the load model, should be fine to limit to that team
        /// </summary>
        /// <param name="SpecificTeam">If has value, other teams can be excluded from selection.<para>
        /// This doesn't *have* to be done, though, the caller will likely filter afterward</para></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public virtual TeamList LoadTeams(ref WindowConfigurationLoadModel m)
        {
            string path = Path.Combine(NetworkPath, TEAM_FILE);
            m.Key = ENC_CK;
            m.Tag = path;
            if (File.Exists(path))
            {
                try
                {
                    string content = File.ReadAllText(path);
                    return content.Decrypt(ENC_CK).DeserializeXML<TeamList>();
                }
                catch (Exception ex)
                {
                    MyExceptionManager.Handle(ex, "Could not load Teams", ExceptionLevel.UI_Advanced);
                    return null;
                }
            }
            return new TeamList();
        }
        public virtual ContextMenuList LoadContextMenus(ref WindowConfigurationLoadModel m)
        {
            string path;
            string content = string.Empty;
            if (m.UserSpecific)
                path = Path.Combine(BasePath, CONTEXTMENU_FILE);
            else
                path = Path.Combine(NetworkPath, NETWORK_CONTEXTMENU_FILE);
            m.Tag = path;
            try
            {
                ContextMenuList cm;
                content = File.ReadAllText(path);
                if (!m.UserSpecific)
                {
                    cm = content.Decrypt(ENC_CK).DeserializeXML<ContextMenuList>();
                    m.Key = ENC_CK;
                }
                else
                    cm = content.DeserializeXML<ContextMenuList>();
                cm.LoadModel = m;
                return cm;
            }
            catch (Exception ex)
            {
                MyExceptionManager.Handle(ex, null, ExceptionLevel.UI_Advanced);
                return null;
            }
        }

        public virtual ContextAddonList LoadContextAddons(ref WindowConfigurationLoadModel m)
        {
            string path;
            string content = string.Empty;
            if (m.UserSpecific)
                path = Path.Combine(BasePath, CONTEXT_ADDONS_FILE);
            else
                path = Path.Combine(NetworkPath, NETWORK_CONTEXT_ADDONS_FILE);
            m.Tag = path;
            try
            {
                ContextAddonList cm;
                content = File.ReadAllText(path);
                if (!m.UserSpecific)
                {
                    cm = content.Decrypt(ENC_CK).DeserializeXML<ContextAddonList>();
                    m.Key = ENC_CK;
                }
                else
                    cm = content.DeserializeXML<ContextAddonList>();
                cm.LoadModel = m;
                return cm;
            }
            catch (Exception ex)
            {
                MyExceptionManager.Handle(ex, null, ExceptionLevel.UI_Advanced);
                return null;
            }
        }

        public virtual WindowAddonList LoadWindowAddons(ref WindowConfigurationLoadModel m)
        {
            string path;
            string content = string.Empty;
            if (m.UserSpecific)
                path = Path.Combine(BasePath, WINDOW_ADDONS_FILE);
            else
                path = Path.Combine(NetworkPath, NETWORK_WINDOW_ADDONS_FILE);
            m.Tag = path;
            try
            {
                WindowAddonList a;
                content = File.ReadAllText(path);
                if (!m.UserSpecific)
                {
                    a = content.Decrypt(ENC_CK).DeserializeXML<WindowAddonList>();
                    m.Key = ENC_CK;
                }
                else
                    a = content.DeserializeXML<WindowAddonList>();
                a.LoadModel = m;
                return a;
            }
            catch (Exception ex)
            {
                MyExceptionManager.Handle(ex, null, ExceptionLevel.UI_Advanced);
                return null;
            }
        }
        #endregion

        #region edit Windows
        /// <summary>
        /// Open a window for editing/creating a new user. 
        /// <para>If editing is successful, should just return the modified user.</para>
        /// <para>Otherwise, return null.</para>
        /// <para>Caller will add or update to the appropriate list depending on <see cref="iWindowConfiguration.ID"/> if the result is not null</para>
        /// </summary>
        /// <param name="toEdit"></param>
        /// <returns></returns>
        public virtual WindowUser AddEditUser(WindowUser toEdit)
        {
            UserEditor ue = new UserEditor(toEdit, !toEdit.TeamID.HasValue || toEdit.TeamID == ConfiguredUser.TeamID);
            var r= ue.ShowDialog();
            if (r ?? false)
                return ue.Edit;
            return null;
        }
        public virtual ContextAddonConfiguration AddEditContextAddon(ContextAddonConfiguration toEdit)
        {
            ContextAddonEditor cae = new ContextAddonEditor(toEdit);
            if (cae.ShowDialog() ?? false)
                return cae.Edit;
            return null;
        }
        public virtual ContextMenuConfiguration.ContextMenuConfiguration AddEditContextMenu(ContextMenuConfiguration.ContextMenuConfiguration toEdit = null)
        {            
            ContextMenuEditor cme = new ContextMenuEditor(toEdit);
            if (cme.ShowDialog() ?? false)
                return cme.Edit;
            return null;
        }
        public virtual Database AddEditDatabaseConfig(Database toEdit)
        {
            DatabaseConnectionEditor dbe = new DatabaseConnectionEditor(toEdit);
            if (dbe.ShowDialog() ?? false)
                return dbe.Edit;
            return null;
        }
        public virtual Query AddEditQuery(Query toEdit)
        {
            QueryEditor qe = new QueryEditor(toEdit);
            if (qe.ShowDialog() ?? false)
                return qe.Edit;
            return null;
        }
        public virtual WindowAddonConfiguration AddEditWindowAddon(WindowAddonConfiguration toEdit)
        {
            WindowAddonEditor wae = new WindowAddonEditor(toEdit);
            if (wae.ShowDialog() ?? false)
                return wae.Edit;
            return null;
        }
        public virtual Team AddEditTeam(Team toEdit)
        {
            TeamEditor te = new TeamEditor(toEdit);
            if (te.ShowDialog() ?? false)
                return te.Edit;
            return null;
        }
        #endregion

        public IEnumerable<iWindowConfiguration> GetLookup(WindowConfigurationScope lookup)
        {
            switch (lookup)
            {
                case WindowConfigurationScope.Q:                
                    return new List<iWindowConfiguration>(Queries.ConfigurationEntries);
                case WindowConfigurationScope.SW:
                case WindowConfigurationScope.CM:                
                case WindowConfigurationScope.D:
                    return ContextMenus.GetSubList(lookup);
                case WindowConfigurationScope.ACM:
                    return new List<iWindowConfiguration>(ContextAddons.ConfigurationEntries);
                //return new List<iWindowConfiguration>(); //Context menus...
                case WindowConfigurationScope.U:
                    return new List<iWindowConfiguration>(Users.ConfigurationEntries);
                case WindowConfigurationScope.TM:
                    return new List<iWindowConfiguration>(Teams.ConfigurationEntries);
                case WindowConfigurationScope.ADML:
                    return new List<iWindowConfiguration>(AdminLevels);
                case WindowConfigurationScope.A:
                    return new List<iWindowConfiguration>(WindowAddons.ConfigurationEntries); //Addon configurations..
                case WindowConfigurationScope.DB:
                    return new List<iWindowConfiguration>(Connections.ConfigurationEntries);
                default:
                    return new List<iWindowConfiguration>();
            }
        }
        //public string AddonDirectory; //Should be set by application, no override?                    
        public QueryList Queries { get; protected set; }
        public ContextMenuList ContextMenus { get; protected set; }
        public DatabaseList Connections { get; protected set; }
        //public QueryConfiguration.QueryConfigurationList MyTeamQueries { get; protected set; } //Load on request.
        public TeamList Teams { get; protected set; }
        public WindowUserCollection Users { get; protected set; }
        public ContextAddonList ContextAddons { get; protected set; }
        public WindowAddonList WindowAddons { get; protected set; }

        IEnumerable<AdminLevel> AdminLevels { get; set; }
        /// <summary>
        /// The real (logged in) user. Should be set by <see cref="LogIn(LoginInfo)"/>.
        /// <para>If implementing impersonation, should set in <see cref="Impersonate(LoginInfo, LoginInfo)"/></para>
        /// <para>Note: the real(original) user will be stored in sessionWindow information</para>
        /// </summary>
        public WindowUser ConfiguredUser { get; protected set; }        
        
        /// <summary>
        /// Called after checking result of Validate. Should populate <see cref="ConfiguredUser"/>
        /// <para>If for some reason the user doesn't exist and cannot be added in the User's Add Method (override as needed)
        /// E.g. no permission, then should return null, and the login fails even if credentials were valid.</para>
        /// <para>May not be called if in SingleUser mode, so should not rely on this being called for any set up...</para>        
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public virtual void LogIn(LoginInfo info)
        {
            ConfiguredUser = null;
            var u = Users[info.ToString()];
            if (u == null)
            {
                u = new WindowUser
                {                    
                    MyPermissions = BasicUser.BasicPermissionSet,
                    domain = info.Domain,
                    userName = info.UserName
                };
                Users.Add(u);
            }
            if (u.ID == null)
                return;
            ConfiguredUser = u;
            AdminLevels = AdminLevel.GetLevels(u, MaxAdminLevel);
            return;
        }
        /// <summary>
        /// Only need to implement if MetaData export says that it's supported.
        /// <para>Return the user to be impersonated, or null if not allowed to impersonate that user for whatever reason.</para>
        /// </summary>
        /// <param name="user"></param>
        /// <param name="toImpersonate"></param>
        /// <returns></returns>
        public abstract WindowUser Impersonate(LoginInfo user, LoginInfo toImpersonate);
        /// <summary>
        /// Called when the user is done impersonating, or when logging in with single user mode<para>
        /// Resets <see cref="ConfiguredUser"/> and calls <see cref="LoadConfigurations"/></para>               
        /// </summary>
        /// <param name="original"></param>
        public void SetUser(WindowUser original)
        {
            ConfiguredUser = original;            
            LoadConfigurations(false, false); //Impersonation cannot be done from single user mode..
            AdminLevels = AdminLevel.GetLevels(ConfiguredUser, MaxAdminLevel);
        }      
        /// <summary>
        /// Check credentials. Default implementation just returns true. 
        /// <para>May not be called if in SingleUser mode, so should not rely on this being called for any set up...</para>
        /// </summary>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public virtual bool ValidateCredential(LoginInfo login, string password)
        {
            return true;
        }
        
        public void SetupUserTeamDescriptions()
        {
            Users.FillTeamKeys(Teams);
        }
        

    }
    public class LoginInfo
    {        
        public string Domain;
        public string UserName;
        public override string ToString()
        {
            return Domain + "\\" + UserName;
        }
        public static LoginInfo Parse(string login)
        {
            LoginInfo rc = new LoginInfo();
            string[] s = login.Split('\\');
            if (s.Length == 1)
            {
                s = login.Split('@');
                if (s.Length == 1)
                {
                    rc.UserName = login;
                    rc.Domain = Environment.UserDomainName;
                }
                else
                {
                    int x = s[1].IndexOf('.');
                    if (x < 0)
                        rc.Domain = s[1];
                    else
                        rc.Domain = s[1].Substring(0, x);
                    rc.UserName = s[0];
                }
            }
            else
            {
                rc.Domain = s[0];
                rc.UserName = s[1];
            }
            return rc;

        }
    }
}
