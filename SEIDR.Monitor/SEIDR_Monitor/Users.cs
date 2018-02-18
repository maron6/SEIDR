using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SEIDR.Dynamics.Configurations;
using SEIDR.Dynamics;
using PropertyChanging;
using System.ComponentModel;
using SEIDR.Dynamics.Configurations.Encryption;
using SEIDR.Dynamics.Configurations.UserConfiguration;
using System.Collections;

namespace SEIDR.WindowMonitor
{
    public class Users : IEnumerable<User>, iConfigList
    { 
        public int Count { get { return userList.Count; } }
        [XmlIgnore]
        public List<User> userList = new List<User>();
        public void Add(User u)
        {
            if (u.IMPERSONATION)
            {
                sExceptionManager.Handle("Cannot add Team Impersonations to the Users list.", ExceptionLevel.Background);
                return;
            }
            var uqCheck = (from record in userList
                          let Name = record.Name
                          where Name == u.Name
                          select record).ToArray();
            if(uqCheck.Length > 0)
            {
                //new Alert("Adding this user would create duplicates.", Choice: false).ShowDialog();
                sExceptionManager.Handle("Adding this user would create a duplicate. Try refreshing the settings and trying again", 
                    ExceptionLevel.UI_Basic);                
                return;
            }
            short maxID = 0;
            if (userList.Count > 0 && !u.LMUID.HasValue)
            {
                var idList = (from record in userList
                         let ID = record.LMUID
                         where ID != null
                         orderby ID descending
                         select ID.Value)
                           .ToArray();
                if(idList.Length > 0) //should always be this case... but just to be sure
                    maxID = idList[0];
            }
            maxID++;
            u.LMUID = u.LMUID ?? maxID;
            userList.Add(u);
        } 
        public User this[string Name]
        {
            get
            {
                var ua = (from u in userList
                          where u.Name == Name
                          select u).ToArray<User>();
                if (ua.Length == 0)
                    return null;
                return ua[0];
            }
            set
            {
                int idx = GetIndex(Name);
                if (idx < 0)
                    Add(value);
                else
                {
                    userList[idx] = value;
                }
            }
        }
        #region Obsolete
        [Obsolete("May exclude columns and could lead to loss of data", true)]
        private void UpdateFromDataTable(DataTable dt)
        {
            var update = (GetFromDataTable(dt));
            foreach(var user in update)
            {
                var q = (from u in userList
                        where u.Name == user.Name
                        select userList.IndexOf(u)).ToArray();
                if (q.Length > 0)
                    userList[q[0]] = user;
                else
                    Add(user);
            }
        }
        [Obsolete("May exclude columns and could lead to loss of data", true)]
        private User[] GetFromDataTable(DataTable dt)
        {
            return dt.GetObjectList<User>();
        }
        #endregion

        public bool CheckForTeam(string Team)
        {
            if (Team == null)
                return false;
            var q = (from u in userList
                     where u.MyTeam == Team
                     select u.Name).FirstOrDefault();
            return q != null;
        }
        [XmlIgnore]
        public DataTable MyData
        {
            get
            {
                
                var self = SettingManager.__Session__.CurrentUser;
                if (self == null || userList == null || !self.IsAdmin)
                    return null;
                var q = from user in userList
                        let al = self.AdminLevel.Value
                        where user != self
                        && (user.IsAdmin == false || (user.AdminLevel?? ushort.MaxValue) > al)
                        && CompareTeams(user, self)
                        select user;
                
                return q.ToArray().ToDataTable("LMUID", 
                    "AdminLevel", "ID", "CanEditSettings", "Admin", "SuperAdmin",
                    "EncryptedPassword", "IMPERSONATION", "MyTeam", "Name",
                    "Team");
            }
        }
        private bool CompareTeams(User check, User manager)
        {
            if (manager.AdminLevel <= 1 || check.MyTeam == null)
                return true;
            return check.MyTeam == manager.MyTeam /* || check.DQTeam == manager.DQTeam */;              
        }
        public Guid Version
        {
            get;
            set;
        }
        /// <summary>
        /// Gets the index of the given User in the userList
        /// </summary>
        /// <param name="idx">UserName</param>
        /// <param name="IncludeNew">Ignored</param>
        /// <returns></returns>
        public int GetIndex(string idx, bool IncludeNew = true)
        {
            for(int i =0; i < userList.Count; i++)
            {
                if(userList[i].Name == idx)
                {
                    return i;
                }
            }
            return -1;
        }

        public List<string> GetNameList()
        {
            return(from u in userList
                   select u.Name).ToList<string>();
        }

        public void Remove(string NameKey)
        {
            userList.RemoveAt(GetIndex(NameKey));
        }
        /// <summary>
        /// Grabs the user names as a list of string since name is not used in the same way as the actual iConfig
        /// </summary>
        /// <param name="AddNew"></param>
        /// <returns></returns>
        public List<string> ToStringList(bool AddNew = true)
        {
            return GetNameList();
        }

        #region Cloneable + Enumerator...
        public bool Cloneable { get { return false; } }

        public IEnumerator<User> GetEnumerator()
        {
            return ((IEnumerable<User>)userList).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<User>)userList).GetEnumerator();
        }

        public iConfigList cloneSetup()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
    [ImplementPropertyChanging]
    public class User :BasicUser
    {
        public static string[] GetDomains()
        {
            List<string> x = new List<string>(LoginSplash.VALID_DOMAINS);
            if (!x.Contains(Environment.UserDomainName))
                x.Add(Environment.UserDomainName);
            return x.ToArray();
        }
        [DefaultValue(false)]
        public bool IMPERSONATION { get; set; } = false;
        public const string DEFAULT_TEAM_GUI_PLACEHOLDER = "(DEFAULT)";
        #region Abstract class implementations.
        [XmlIgnore]
        public override short? ID
        {
            get
            {                
                return LMUID;
            }
        }
        [XmlIgnore]
        public override bool CanEditSettings { get { return CanEditAddons || CanEditContextAddons || CanEditContextMenus; } }
        [XmlIgnore]
        public override string UserName
        {
            get
            {
                return _Name;
            }
        }
        [XmlIgnore]
        public override string Team
        {
            get
            {
                return MyTeam;
            }
        }
        [XmlIgnore]
        public override bool Admin
        {
            get
            {
                return IsAdmin;
            }
        }
        [XmlIgnore]
        public bool AddonsAdmin
        {
            get
            {
                return AdminLevel.HasValue && AdminLevel <= 3;
            }
        }
        public override bool SuperAdmin
        {
            get
            {
                return AdminLevel.HasValue && AdminLevel <= 1;
            }
        }
        #endregion
        /// <summary>
        /// Casts the user as a BasicUser, but only if the ID is populated.
        /// </summary>
        /// <returns></returns>
        public BasicUser AsBasicUser()
        {
            if (LMUID == null)
                return null;
            return (BasicUser)this;
            /*
            BasicUser b = new BasicUser
            {
                UserName = Name,
                AdminLevel = AdminLevel,
                CanEditSettings = CanEditAddons || CanEditContextAddons,                
                AddonPermissions = AddonPermissions,
                CanUseCache = CanUseCache,
                Team=Team,
                ID = LMUID.Value
            };            
            return b;
            */
        }
        #region Permissions for addons
        /// <summary>
        /// For editing combo box, addon permissions available will be a list of the addon Names
        /// </summary>
        string[] _AddonPermissions { get; set; } = new string[0]; //Avoid null
        public string[] GetPermissions()
        {
            string[] cLib = SettingManager.myLibrary.GetAddonList();
            string[] aLib = SettingManager.myMenuLibrary.GetAddonNames(AsBasicUser());
            List<string> permissionList = new List<string>(_AddonPermissions);
            permissionList.AddRange(aLib);
            permissionList.AddRange(cLib);
            return permissionList.Distinct().ToArray();
        }
        [XmlIgnore]
        public override string[] AddonPermissions { get { return new List<string>(_AddonPermissions).ToArray(); } }
        public void SetPermissions(string[] list)
        {
            _AddonPermissions = list;
        }
        public string[] GetInvisiblePermissions(User editor)
        {
            if (editor.IsAdmin && editor.AdminLevel <= 1)
            {
                return new string[0];
            }
            var check = editor.AddonPermissions;
            if (check == null)
                return AddonPermissions;
            var q = (from addon in AddonPermissions
                     where !check.Contains(addon)
                     select addon);
            return q.ToArray();
        }
        public string[] GetVisiblePermissions(User Editor)
        {
            if(Editor.IsAdmin && Editor.AdminLevel <= 1)
            {
                return AddonPermissions;
            }
            var check = Editor.AddonPermissions;
            if (check == null)
                return new string[0];
            var q = (from addon in AddonPermissions                     
                     where check.Contains(addon)
                     select addon);
            return q.ToArray();
        }
        #endregion
        public short? LMUID { get; set; }
        string _Name = null;
        public string Name { get { return _Name; }
            set
            {
                if(LMUID.HasValue && _Name != null)
                {
                    //new Alert("Cannot modify name of existing User.").ShowDialog();
                    sExceptionManager.Handle("Cannot modify the name of an existing user.", ExceptionLevel.UI_Basic);
                    return;
                }
                _Name = value;
            }
        }
        public string Domain { get; set; }
        [DefaultValue(null)]
        public string MyTeam { get; set; }
        [XmlIgnore]
        public bool IsAdmin
        {
            get
            {
                return AdminLevel.HasValue;
            }
            set
            {
                CanEditConnections = value;
                CanEditQueries = value;
                CanEditContextMenus = value;
                //CanOpenDashboard = value;
                //CanUseCache = value;
                CanUseAddons = value;
                CanEditAddons = value;
                CanExportData = value;
                if (!value)
                    AdminLevel = null; //Don't set the value if true, but do allow removing it
            }
        }
        [DefaultValue(null)]
        public ushort? AdminLevel { get; set; } = null;
        
        bool _editConn;
        bool _editQueries;
        bool _EditContext;
        public bool CanEditContextAddons
        {
            get { return _CanEditContextAddons; }
            set { _CanEditContextAddons = value; }
        }
        public bool CanEditConnections { get { return _editConn; } set { _editConn = value; } }
        public bool CanEditQueries { get { return _editQueries; } set { _editQueries = value; } }
        public bool CanEditContextMenus
        {
            get { return _EditContext; }
            set { _EditContext = value; }
        }
        
        bool _CanEditContextAddons;
        
        
        
        
        
        public bool CanUseAddons { get; set; }
        public bool CanExportData { get; set; }
        [XmlIgnore]
        public static User SingleUserMode
        {
            get
            {
                return new User
                {
                    LMUID=-1,
                    AdminLevel = 0,
                    CanEditConnections = true,
                    CanEditContextMenus = true,
                    CanEditContextAddons = true,
                    CanEditQueries = true,
                    CanEditAddons = true,
                    CanUseAddons = true,
                    CanUseCache = true,
                    MyTeam = null,
                    IMPERSONATION = false,
                    //CanOpenContextMenus = true,
                    CanExportData = true,                    
                    Name = GetEnvironmentName(),
                    Domain = Environment.UserDomainName //Just used for tracking purposes in editor
                };
            }
        }
        [XmlIgnore]
        public static User DefaultUser
        {
            get
            {
                //var self = SettingManager.Me;
                //var self = SettingManager._ME; //.__Session__?.CurrentUser;
                //string defTeam = self == null ? null : self.MyTeam; //Creating new users defaults to the same team as the app user (null if the user is being
                //created for the app user
                string defTeam = SettingManager._ME?.MyTeam;                
#if DEBUG 
                var temp = new User
                {
                    AdminLevel = 0,
                    CanEditConnections = true,
                    CanEditContextMenus = true,
                    CanEditContextAddons = true,
                    CanEditQueries = true,
                    CanEditAddons = true,
                    CanUseAddons = true,
                    CanUseCache = true,
                    MyTeam = defTeam,                                       
                    //CanOpenContextMenus = true,
                    CanExportData = true,
                    IMPERSONATION = false,                    
                    Name = GetEnvironmentName(),
                    Domain = Environment.UserDomainName //Just used for tracking purposes in editor, since used for adding...
                };                
                return temp;
#else
                return new User
                {
                    //IsAdmin = true,
                    AdminLevel = null,     //Remove admin
                    Name = string.Empty,
                    Domain = Environment.UserDomainName,
                    CanEditConnections = true,
                    CanEditQueries = true,
                    CanUseAddons = true,
                    CanEditContextAddons = false,
                    CanEditAddons = false,
                    CanEditContextMenus = true,
                    CanUseCache = true,
                    MyTeam = defTeam,
                    //CanRunQueries = true,
                    //CanOpenContextMenus = true,
                    CanExportData = false,
                    //CanOpenDashboard = true,
                    IMPERSONATION = false,                    
                    //,EncryptedPassword = "NewPass!".Encrypt(LoginSplash.LoginKey)
                };
#endif
            }
        }

        public bool CanEditAddons { get; set; }

        public DataColumn GetLMUIDColumn()
        {
            return new DataColumn
            {
                ColumnName = "LMUID",
                DataType = typeof(ushort?),
                DefaultValue = LMUID
            };
        }
        public static string GetEnvironmentName()
        {
            return Environment.UserDomainName + "\\" + Environment.UserName;
        }
        /*string _EncryptedPassword;
        public string EncryptedPassword { get { return _EncryptedPassword; } set { _EncryptedPassword = value; } }
        */
    }
}
