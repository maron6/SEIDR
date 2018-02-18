using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Configurations;
using SEIDR.Dynamics.Configurations.Encryption;
using SEIDR.Dynamics;
//using SEIDR.Processing.Data.DBObjects;
using SEIDR.DataBase;
using SEIDR.Dynamics.Windows;
using System.Data;
//using SEIDR.Processing.Data;
using System.IO;
using System.ComponentModel.Composition;
using SEIDR.WindowMonitor.MonitorConfigurationHelpers;
using static SEIDR.WindowMonitor.sExceptionManager;
    

namespace SEIDR.WindowMonitor
{
    public static class SettingManager
    {
        #region Constants
        /// <summary>
        /// This should be updated later....
        /// </summary>
        public static string NetworkPath { get; set; } = null;
        public const string MyAppName = "SEIDR.Window";
        public const string QueryFile = "Queries.xml";
        public const string ContextFile = "ContextMenus.xml";
        public const string ConnectionFile = "DBConnections.xml";
        public const string QueryDefault = "Queries.scrl";
        public const string ContextDefault = "ContextMenus.scrl";
        public const string ConnectionDefault = "DBConnections.scrl";
        public const string AddonFile = "Addons.xml";
        public const string AddonDefault = "Addons.scrl";
        public const string UserFile = "Users.scrl";
        private const string Key = "SEIDR_MONITOR_CONFIGURATION";
        public const string SETTING_SUBFOLDER = "Settings";
        #endregion
        public static AddOnLibrary myLibrary { get; private set; }
        public static SEIDR_MenuItemLibrary myMenuLibrary { get; private set; } //Same library location as myLibrary, but for main menu level menu items
        public static Queries myQueries
        {
            get; set;
        }
        public static SEIDR_MenuAddOnConfigs myAddons { get; set; }
        public static ContextMenuItems myContextMenus { get; set; }
        public static DBConnections myConnections { get; set; }
        public static MiscSetting myMiscSettings { get; set; }

        #region User information, post loading
        //public static Users AppUsers { get; private set; }
        static string NAD;
        static Users AppUsers
        {
            get
            {
                return NAD?.Decrypt(Key)?.DeserializeXML<Users>();
            }
            //private 
                set
            {
                if (value == null)
                    NAD = null;
                else
                    NAD = value.SerializeToXML().Encrypt(Key);
            }
        }
        public static void AddUser(User u)
        {
            var tu = AppUsers;
            tu.Add(u);
            AppUsers = tu;
        }
        public static User GetUser(string UserName) { return AppUsers[UserName]; }
        public static bool CheckTeamExist(string Team) => AppUsers.CheckForTeam(Team); 
        /// <summary>
        /// Gets a copy of the users list...
        /// </summary>
        /// <returns></returns>
        public static Users GetUsersCopy() { return AppUsers; }
        public static void SetUsers(Users uList) { AppUsers = uList; }
        /// <summary>
        /// The current user - note that users should not be able to update their own permissions. Should be handled by only allowing users to edit users with lower admin level. May need to have dupe checking
        /// </summary>
        [Obsolete("Should ONLY be used during setup and logging in", true)]
        public static User Me;
        public static UserSessionManager __Session__ = null;
        public static User _ME { get { return __Session__?.CurrentUser; } }

        /// <summary>
        /// Gets a string list of teams the user can see. null team will be replaced with the default placeholder for the the GUI
        /// </summary>
        /// <returns></returns>
        public static string[] GetTeams(bool includeDefault = true)
        {
            var q = (from u in AppUsers
                     where (_ME?.AdminLevel != null && _ME.AdminLevel <= 1)
                            || (
                                u.MyTeam != null
                                || (includeDefault && u.MyTeam == null)
                                || u.MyTeam == _ME.MyTeam
                                )
                     select u.MyTeam ?? User.DEFAULT_TEAM_GUI_PLACEHOLDER)
                    .Distinct();
            return q.ToArray();
        }
        #endregion
        public static bool Setup(UserSessionManager session)
        {
            __Session__ = session;
            if (_ME == null)
                return false;            
            Team = _ME.MyTeam;
            InitFilePaths(); //Set initial paths, going forward will only set when team changes
            //try loading these guys from a file. Either the default, or local. Depending on permissions/existence of existing local. use a new setting if nothing
            SetupVariables();
            if(_ME.ID == null )
            {
                Users tu = AppUsers;
                tu.Add(_ME);                
                SaveUsers(tu);
            }
            return true;
        }
        static SettingManager()
        {

            /*
             * Moved to login splash... has a readonly array of strings at top of window class. Default is empty, should be populated if you
             * want to limit the login domains of windows users. (E.g., prevent login from home computer
            bool b = User.GetDomains().Contains(Environment.UserDomainName);
            if(!b)
            {
                new Alert("Invalid domain: '" + (Environment.UserDomainName?? "(UNK)") + "'" , Choice: false).ShowDialog();
                App.Current.MainWindow.Close();
                return;
            }
            */
            try
            {
                myMiscSettings = MiscSetting.LoadFromFile();
            } catch
            {
                myMiscSettings = new MiscSetting();
                Handle("Creating new misc Setting", ExceptionLevel.Background);
            }
            InitFilePaths();
            LoadUsers();

            /*
            if (Me == null)
            {
                (Me = User.DefaultUser).Name = User.GetEnvironmentName();
                Handle("Creating new User");
            }
            */
            /*
                 Me = new User
                 {
                     //AdminLevel = null, //Setting IsAdmin= false will also set this to null. 
                     //Note- setting IsADmin to true will not set the admin level
                     IsAdmin = DefaultAdmin,
                     LoaderControl = DefaultAdmin,
                     ETLTeam = DefaultAdmin,
                     Name = Environment.UserName
                 };*/

        }
        public static void InitLoad() { }
        static string Team = null;
        #region Actual File Paths
        private static void InitFilePaths()
        {
            Func<string, string> GetCheckNetwork = ((fName) => {
                string x = GetNetworkPath(fName, Team);
                if (!File.Exists(x))
                    x = GetNetworkPath(fName); //If the team file does not exist, get the default path for the file.
                return x;
            });
            #region Library folders
            if (LibraryFolder == null)
                LibraryFolder = Path.Combine(ConfigFolder.GetFolder(MyAppName, true), "Addons");
            else if (!Directory.Exists(LibraryFolder))
                Directory.CreateDirectory(LibraryFolder);
            if (WindowLibraryFolder == null)
                WindowLibraryFolder = Path.Combine(ConfigFolder.GetFolder(MyAppName), "WindowAddons");
            else if (!Directory.Exists(WindowLibraryFolder))
                Directory.CreateDirectory(WindowLibraryFolder);
            #endregion


            //Network Source files for defaults - .SCRL
            NetworkQueries = GetCheckNetwork(QueryDefault);
            NetworkContext = GetCheckNetwork(ContextDefault);
            NetworkDBs = GetCheckNetwork(ConnectionDefault);
            NetworkAddons = GetCheckNetwork(AddonDefault);
            //NetworkQueries = GetNetworkPath(QueryDefault, Team);            
            //NetworkContext = GetNetworkPath(ContextDefault, Team);
            //NetworkDBs = GetNetworkPath(ConnectionDefault, Team);
            //NetworkAddons = GetNetworkPath(AddonDefault, Team);


            //Local XML
            LocalQueries = GetFilePath(QueryFile); 
            LocalContext = GetFilePath(ContextFile);
            LocalDBs = GetFilePath(ConnectionFile);
            LocalAddons = GetFilePath(AddonFile);


            //Local copy of Network files to minimize network loading - .SCRL
            DefaultQueries = GetFilePath(QueryDefault);
            DefaultContext = GetFilePath(ContextDefault);
            DefaultDBs = GetFilePath(ConnectionDefault);
            DefaultAddons = GetFilePath(AddonDefault);


            //Users - Network + local copy - .SCRL
            NetworkUsers = GetNetworkPath(UserFile);
            DefaultUsers = GetFilePath(UserFile);            
        }

        //Network SCRL. Might be modified to a team path
        static string NetworkQueries;
        static string NetworkContext;
        static string NetworkDBs;
        static string NetworkAddons;
        static string NetworkUsers;
        //Local XML
        static string LocalQueries;
        static string LocalContext;
        static string LocalDBs;
        static string LocalAddons;
        //Local SCRL
        static string DefaultQueries;
        static string DefaultContext;
        static string DefaultDBs;
        static string DefaultAddons;
        static string DefaultUsers;
        //Addon/plugin folders
        public static string LibraryFolder { get; private set; }
        public static string WindowLibraryFolder { get; private set; }
        #endregion
        static DateTime lastDefaultUpdate = DateTime.MinValue;
        //static double lastDefaultDateCheck = DateTime.MinValue.ToOADate();
        public static void ForceAddonRefresh()
        {
            if (LibraryFolder != null)
            {
                if (myLibrary == null)
                {
                    try
                    {
                        myLibrary = new AddOnLibrary(LibraryFolder);
                    }
                    catch (Exception e) { Handle(e, "Unable to set up Context Addon Library", ExceptionLevel.Background); }
                }
                else myLibrary.RefreshLibrary();
            }
            if (WindowLibraryFolder != null)
            {
                if (myMenuLibrary == null)
                {
                    try
                    {
                        myMenuLibrary = new SEIDR_MenuItemLibrary(WindowLibraryFolder);
                    }
                    catch (Exception e) { Handle(e, "Unable to set up Window Plugin Library", ExceptionLevel.Background); }
                }
                else myMenuLibrary.RefreshLibrary();
            }
        }
        private static void SetupVariables()
        {
            if (_ME == null)
                return;
            //LoadUsers(); //Move out to make sure Me is populated for grabbing team
            var check = DateTime.Now.AddMinutes(-myMiscSettings.FileRefresh);

            //if(check - lastDefaultDateCheck > .004)// ~ 10 mins
            if (check > lastDefaultUpdate)
            {
                if (_ME.CanUseAddons
                    //Only update addon libraries if they haven't been set yet.
                    && (myLibrary == null || myMenuLibrary == null) 
                    //Only need to check if at least one of the library folders has a location to check
                    && (LibraryFolder != null || WindowLibraryFolder != null) 
                )
                {
                    //Values shouldn't be null.
                    if (LibraryFolder == null) 
                        myLibrary = null;// new AddOnLibrary(LibraryFolder);
                    if (WindowLibraryFolder == null)
                        myMenuLibrary = null;                           
                    ForceAddonRefresh();
                }

                Action<string, string> r = ((a, b) => File.Copy(a, b, true));
                //Action<string> r2 = r.Curry("Source"); //Would be useful if repeatedly copying and sometimes changing the source
                r.WithoutErrorHandling(NetworkQueries, DefaultQueries);
                r.WithoutErrorHandling(NetworkContext, DefaultContext);
                r.WithoutErrorHandling(NetworkDBs, DefaultDBs);
                r.WithoutErrorHandling(NetworkAddons, DefaultAddons);                
                lastDefaultUpdate = DateTime.Now;
                //lastDefaultDateCheck = lastDefaultUpdate.ToOADate();               
            }
            #region Queries
            try
            {
                if (!_ME.CanEditQueries || !File.Exists(LocalQueries))
                    myQueries = File.ReadAllText(DefaultQueries).Decrypt(Key).DeserializeXML<Queries>();
                else
                    myQueries = ConfigFolder.DeSerializeFile<Queries>(LocalQueries);
            }
            catch { myQueries = new Queries(); }
            #endregion
            #region DBConnections
            try
            {
                if (!_ME.CanEditConnections || !File.Exists(LocalDBs))
                    myConnections = File.ReadAllText(DefaultDBs).Decrypt(Key).DeserializeXML<DBConnections>();
                else
                    myConnections = ConfigFolder.DeSerializeFile<DBConnections>(LocalDBs);
                foreach (DBConnection db in myConnections)
                {
                    DatabaseConnectionManager.AddConnections(db.Name, db.InternalDBConn, true); //Make sure it's useable for DBObjects
                }
            }
            catch { myConnections = new DBConnections(); }
            #endregion
            #region Context Menus
            try
            {

                if (!_ME.CanEditContextMenus || !File.Exists(LocalContext))
                    myContextMenus = File.ReadAllText(DefaultContext).Decrypt(Key).DeserializeXML<ContextMenuItems>();
                else
                    myContextMenus = ConfigFolder.DeSerializeFile<ContextMenuItems>(LocalContext);
            }
            catch { myContextMenus = new ContextMenuItems(); }
            #endregion
            #region Plugins/Addon settigns
            try
            {
                if (!_ME.CanEditAddons || !File.Exists(LocalAddons))
                    myAddons = File.ReadAllText(DefaultAddons).Decrypt(Key).DeserializeXML<SEIDR_MenuAddOnConfigs>();
                else
                    myAddons = ConfigFolder.DeSerializeFile<SEIDR_MenuAddOnConfigs>(LocalAddons);
            }
            catch { myAddons = new SEIDR_MenuAddOnConfigs(); }
            #endregion
        }
        public static void UpdatedUserCheck()
        {
            if (_ME == null)
                return;
            if (Team != _ME.MyTeam)
            {
                if (Team != null)
                    Handle("Team changed", ExceptionLevel.Background);

                Team = _ME.MyTeam; //If team has been changed, need to reset team paths
                InitFilePaths();
            }
        }

        #region Saving and loading
        #region Users
        /// <summary>
        /// Attempt to Update and save the App's users.
        /// </summary>
        /// <param name="update">If not null, will be used instead of the SettingManager's variable, for use after opening the UserEditor.
        /// <para>Note: Setting manager will update its variable if the save is successful.
        /// </para></param>
        public static void SaveUsers(Users update = null)
        {
            /*
            if (dt != null)
                AppUsers.UpdateFromDataTable(dt);*/

            Users tAppUsers = update ?? AppUsers;
            if (ShortCircuitSave(NetworkUsers, tAppUsers, true))
            {
                AppUsers = tAppUsers;
                return;
            }
            Users compare;
            try {
                compare = File.ReadAllText(NetworkUsers).Decrypt(Key).DeserializeXML<Users>();
            }
            catch
            {
                //new Alert("Unable to update Users.", Choice: false).ShowDialog();
                Handle("Unable to update Users", ExceptionLevel.UI_Basic);
                return;
            }
            if (compare.Version != tAppUsers.Version)
            {
                Alert a = new Alert("User settings have been modified since you loaded. Reload?", mode: AlertMode.Confirmation);
                var r = a.ShowDialog();
                if (r.HasValue && r.Value)
                {
                    LoadUsers();
                }
                return;
            }

            tAppUsers.Version = Guid.NewGuid();
            string data = tAppUsers.SerializeToXML().Encrypt(Key);
            File.WriteAllText(NetworkUsers, data);
            File.WriteAllText(DefaultUsers, data);
            NAD = data;
        }
        public static void CheckUser(User u)
        {
            if (AppUsers[u.Name] != null)
                return;
            AppUsers.Add(u);
        }

        
        public static void LoadUsers()
        {
            AppUsers = null;
            try
            {
                string text = File.ReadAllText(NetworkUsers);
                AppUsers = text.Decrypt(Key).DeserializeXML<Users>();                 
                File.Copy(NetworkUsers, DefaultUsers, true);
            }
            catch
            {
                if (AppUsers == null)
                {
                    try
                    {
                        string text = File.ReadAllText(DefaultUsers);
                        AppUsers = text.Decrypt(Key).DeserializeXML<Users>();
                    }
                    catch
                    {
                        AppUsers = new Users();
                        Handle("New users object created", ExceptionLevel.Background);
                    }
                }
            }
            if (_ME == null 
                || __Session__.CurrentAccessMode == UserAccessMode.SingleUser 
                || _ME.IMPERSONATION )
            {
                //If session isn't started up or it's single user mode, don't do any user updating.
                //Team = null; //should not need to set Team to  null - if Team paths only need to be changed if the team ACTUALLY changes
                return;
            }            
            User temp = AppUsers[_ME.Name];
            if (temp == null)
            {
                AppUsers.Add(_ME); //Handle creating a new user from login page...            
                //Should be safe to add to the user list here, since have already checked that we're not impersonating
            }
            else
                __Session__.UpdateUser(temp); //Update user with more current information.
            /*
            if (temp == null)
            {
                temp = User.DefaultUser;
                temp.Name = User.GetEnvironmentName();
                AppUsers.Add(temp);
            }*/
            
            UpdatedUserCheck(); //Note: may have already been called by UpdateUser, but still safe to call
            
            /*          
            if (Team != _ME.Team) //Note that Team starts out as 'null' when class starts up
            {
                if (Team != null)
                    Handle("Team changed", ExceptionLevel.Background);

                Team = _ME.Team; //If team has been changed, need to reset team paths
                InitFilePaths();
            }

            */
        }
        #endregion
        static string GetNetworkPath(string FileName, string Team = null)
        {
            string tempPath = myMiscSettings.NetworkFolder;
            if (tempPath == null)
                return null;
            if (Team != null)
                return ConfigFolder.GetNetworkSubPath(tempPath, MyAppName, Team, FileName);
            return ConfigFolder.GetNetworkPath(tempPath, MyAppName, FileName);
        }
        static string GetFilePath(string FileName)
        {
            if (FileName == null)
                return null;
            return ConfigFolder.GetSafePath(MyAppName, SETTING_SUBFOLDER, FileName);
        }
        /// <summary>
        /// Saves the settings as locals - Note that if the user can't edit the setting, it won't matter what the value of local actually is
        /// </summary>
        public static void Save()
        {
            myQueries.SerializeToFile(GetFilePath(QueryFile));
            myContextMenus.SerializeToFile(GetFilePath( ContextFile));
            myConnections.SerializeToFile(GetFilePath(ConnectionFile));
            myAddons.SerializeToFile(GetFilePath(AddonFile));
            myMiscSettings.Save();
        }
        #region Default saving
        public static void SaveDefaultQuery(Queries update, string Team)
        {
            string NetworkQueries = GetNetworkPath(QueryDefault, Team);
            if (ShortCircuitSave(NetworkQueries, update))
                return;
            Queries compare = File.ReadAllText(NetworkQueries).Decrypt(Key).DeserializeXML<Queries>();
            if (update.Version == compare.Version)
            {
                update.Version = Guid.NewGuid();
                File.WriteAllText(NetworkQueries, update.SerializeToXML().Encrypt(Key));
            }
            else
                throw new Exception("Default Queries have been modified by someone else.");
        }
        public static void SaveDefaultContextMenus(ContextMenuItems update, string Team )
        {
            update.ValidateOwners();
            string NetworkContext = GetNetworkPath(ContextDefault, Team);
            if (ShortCircuitSave(NetworkContext, update))
                return;
            ContextMenuItems compare = File.ReadAllText(NetworkContext).Decrypt(Key).DeserializeXML<ContextMenuItems>();
            if (update.Version == compare.Version)
            {
                update.Version = Guid.NewGuid();
                File.WriteAllText(NetworkContext, update.SerializeToXML().Encrypt(Key));
            }
            else
                throw new Exception("Default Context Menus have been modified by someone else.");
        }
        public static void SaveDefaultDBConnections(DBConnections update, string Team)
        {
            string NetworkDBs = GetNetworkPath(ConnectionDefault, Team);
            if (ShortCircuitSave(NetworkDBs, update))
                return;
            DBConnections compare = File.ReadAllText(NetworkDBs).Decrypt(Key).DeserializeXML<DBConnections>();
            if (update.Version == compare.Version)
            {
                update.Version = Guid.NewGuid();
                File.WriteAllText(NetworkDBs, update.SerializeToXML().Encrypt(Key));
            }
            else
                throw new Exception("Default Database Connections have been modified by someone else.");

        }
        public static void SaveDefaultAddons(SEIDR_MenuAddOnConfigs update, string Team)
        {
            string NetworkConfigs = GetNetworkPath(AddonDefault, Team);
            if (ShortCircuitSave(NetworkConfigs, update))
                return;
            SEIDR_MenuAddOnConfigs compare = File.ReadAllText(NetworkConfigs).Decrypt(Key).DeserializeXML<SEIDR_MenuAddOnConfigs>();
            if (update.Version == compare.Version)
            {
                update.Version = Guid.NewGuid();
                File.WriteAllText
                    (NetworkConfigs, update.SerializeToXML().Encrypt(Key));
            }
            else
                throw new Exception("Configurations have been modified by someone else already.");
        }
        #endregion
        /// <summary>
        /// Attempt to short circuit, and simply save the file if there's no existing version
        /// </summary>
        /// <param name="path"></param>
        /// <param name="toSave"></param>
        /// <param name="encrypt"></param>
        /// <returns></returns>
        private static bool ShortCircuitSave(string path, iConfigList toSave, bool encrypt = true)
        {
            if (File.Exists(path))
                return false;
            toSave.Version = Guid.NewGuid();
            string data = toSave.SerializeToXML();
            if (encrypt)
                data = data.Encrypt(Key);
            File.WriteAllText(path, data);
            return true;
        }
        public static iConfigList LoadDefault(iConfigListType type, string Team = null)
        { 
            string FilePath;
            switch (type)
            {
                case iConfigListType.Query:
                    {
                        FilePath = GetNetworkPath(QueryDefault, Team);
                        break;
                    }
                case iConfigListType.ContextMenu:
                    {
                        FilePath = GetNetworkPath(ContextDefault, Team);
                        break;
                    }
                case iConfigListType.DatabaseConnection:
                    {
                        FilePath = GetNetworkPath(ConnectionDefault, Team);
                        break;
                    }
                case iConfigListType.SEIDR_MenuAddOn:
                    {
                        FilePath = GetNetworkPath(AddonDefault, Team);
                        break;
                    }
                default:
                    {
                        return null; //invalid, shouldn't happen
                    }
            }
            try
            {
                
                string data = File.ReadAllText(FilePath).Decrypt(Key);
            
                switch (type)
                {
                    case iConfigListType.Query:
                        return data.DeserializeXML<Queries>();
                    case iConfigListType.ContextMenu:
                        return data.DeserializeXML<ContextMenuItems>();
                    case iConfigListType.DatabaseConnection:
                        return data.DeserializeXML<DBConnections>();
                    case iConfigListType.SEIDR_MenuAddOn:
                        return data.DeserializeXML<SEIDR_MenuAddOnConfigs>();
                    default:
                        return null;//can't happen because we would have already returned, but to avoid compiler error
                }
            }
            catch(Exception e)
            {
                if (e is DirectoryNotFoundException)
                    Handle(e, $"Unable to find Network directory", ExceptionLevel.UI_Basic);
                else if (e is UnauthorizedAccessException)
                    Handle(e, $"Unable to open file - Insufficient security permission to edit file", ExceptionLevel.UI_Basic);
                else if (e is FileNotFoundException)
                {
                    Handle(e, "Setting file did not exist, creating new Setting object", ExceptionLevel.UI_Advanced);
                    switch (type)
                    {
                        case iConfigListType.Query:
                            return new Queries();
                        case iConfigListType.ContextMenu:
                            return new ContextMenuItems();
                        case iConfigListType.DatabaseConnection:
                            return new DBConnections();
                        case iConfigListType.SEIDR_MenuAddOn:
                            return new SEIDR_MenuAddOnConfigs();
                        default:
                            return null;
                    }
                }
                else
                    Handle(e, $"Unable to read {type.ToString()} Network data", ExceptionLevel.UI_Basic);
                /*
                if(File.Exists(ErrorLog))
                    File.AppendAllText(ErrorLog, e.Message);
                new Alert("Unable to read network data", Choice: false).ShowDialog(); */

                return null;
            }
        }
        public static void Reload()
        {
            LoadUsers();
            SetupVariables();            
            myMiscSettings = MiscSetting.LoadFromFile(); //typically handled separately, but shouldn't hurt to reload...
            //Don't reload in saveRefresh because it isn't affected by the call to Save
        }
        public static void SaveRefresh()
        {
            LoadUsers();
            Save();
            SetupVariables();            
        }
#endregion

    }
}
