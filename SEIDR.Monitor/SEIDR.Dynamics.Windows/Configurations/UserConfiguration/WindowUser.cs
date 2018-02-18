using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static SEIDR.Dynamics.Configurations.UserConfiguration.BasicUserPermissions;
namespace SEIDR.Dynamics.Configurations.UserConfiguration
{
    public class WindowUser : BasicUser, iWindowConfiguration
    {
        static WindowUser()
        {
            SingleUserMode = new WindowUser
            {
                domain = Environment.UserDomainName,
                userName = Environment.UserName,
                Description = "SINGLE USER MODE",
                MyPermissions = SuperAdminPermissions,
                AdminLevel = 0
            };
        }     
        public bool CheckPermission(WindowConfigurationScope scope, WindowConfigurationLoadModel listModel)
        {
            if(scope == WindowConfigurationScope.U)
                return CheckPermission(UserEditor);
            if(scope == WindowConfigurationScope.TM)
                return CheckPermission(TeamEditor);
            
            if (!listModel.UserSpecific && !CheckPermission(TeamSettingEditor))
                return false; 
            switch (scope)
            {
                case WindowConfigurationScope.A:
                    {
                        //if (listModel.UserSpecific)
                        //    return CheckPermission(AddonEditor, AddonUser);
                        return CheckPermission(AddonEditor);                        
                    }
                case WindowConfigurationScope.Q:
                    {
                        //if (listModel.UserSpecific)
                        //    return CheckPermission(QueryEditor, DatabaseConnectionEditor);
                        return CheckPermission(QueryEditor);
                    }
                case WindowConfigurationScope.D:
                case WindowConfigurationScope.CM:
                case WindowConfigurationScope.SW:
                    {
                        //if (listModel.UserSpecific)
                        //    CheckPermission(QueryEditor, DatabaseConnectionEditor, ContextMenuEditor);
                        return CheckPermission(ContextMenuEditor);
                    }
                case WindowConfigurationScope.ACM:
                    {
                        //if (listModel.UserSpecific)
                        //    CheckPermission(DatabaseConnectionEditor, 
                        //        AddonEditor, AddonUser, 
                        //        QueryEditor, ContextMenuEditor);
                        return CheckPermission(AddonEditor, ContextMenuEditor);
                    }
                case WindowConfigurationScope.DB:
                    return CheckPermission(DatabaseConnectionEditor);                                    
                default:
                    return false;
            }
        }           
        [XmlIgnore]
        public static WindowUser SingleUserMode { get; private set; }
        [XmlIgnore]
        public override int? UserID { get { return ID;} }
        [XmlIgnore]
        public override string Domain
        {
            get
            {
                return domain;
            }
        }
        [XmlIgnore]
        public override string UserName
        {
            get
            {
                return userName;
            }
        }
        [XmlIgnore]
        public bool Altered { get; set; }
        [DefaultValue(null)]
        public string Description { get; set; } = null;
        [XmlIgnore]
        public string domain;
        [XmlIgnore]
        public string userName;
        public string Key
        {
            get
            {
                return Domain + "\\" +  UserName;
            }

            set
            {
                string[] s = value.Split('\\');
                if (s.Length == 1)
                {
                    userName = value;
                }
                else
                {
                    domain = s[0];
                    userName = s[1];
                }
            }
        }
        [XmlIgnore]
        public WindowConfigurationScope MyScope
        {
            get
            {
                return WindowConfigurationScope.U;
            }
        }
        [DefaultValue(0)]
        public int RecordVersion { get; set; } = 0;
        [DefaultValue(null)]
        public int? ID { get; set; } = null;
        [DefaultValue(null)]
        public List<string> Addons { get; set; }
        [XmlIgnore]
        public override string[] AddonPermissions
        {
            get
            {
                return Addons.ToArray();
            }
        }
        [LookupSource(WindowConfigurationScope.TM)]
        [DefaultValue(null)]
        public int? TeamID { get; set; } = null;
        [XmlIgnore]
        public string team { get; set; }
        /// <summary>
        /// Team's Key
        /// </summary>
        [XmlIgnore]
        public override string Team
        {
            get
            {
                return team ?? "Default";
            }
        }
    }
}
