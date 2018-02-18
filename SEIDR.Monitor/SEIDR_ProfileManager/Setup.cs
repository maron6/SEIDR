using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using Ryan_UtilityCode.Dynamics.Configurations;
using Ryan_UtilityCode.Dynamics.Windows;
using Ryan_UtilityCode.Processing.Data.DBObjects;

namespace SEIDR_ProfileManager
{
    //[SEIDR_PROFILEMANAGER_METADATA]
    [Export(typeof(SEIDR_WindowMenuAddOn)),
        ExportMetadata("Name", "SEIDR.Loader Profile Manager"),
        ExportMetadata("RequirePermission", false),
        ExportMetadata("RequireSessionCache", true),
        ExportMetadata("HasParameterMapping", true),
        ExportMetadata("Team", "ETL")]
    public class SEIDR_PROFILEMANAGER : SEIDR_WindowMenuAddOn
    {
        //[Import(typeof(SEIDR_Window))]
        public SEIDR_Window callerWindow
        {
            set
            {
                MySetup.Window = value;
            }
        }

        public Dictionary<string, Type> GetParameterInfo()
        {
            return new Dictionary<string, Type>
                {
                    { MySetup.ANDROMEDA_BASE_FOLDER, typeof(string) },
                    { MySetup.INTEGRATION_SERVICES, typeof(string) },
                    { MySetup.PACKAGE_FOLDER, typeof(string) },
                    { MySetup.SANDBOX_BASE_FOLDER, typeof(string) },
                    { MySetup.CHECK_FOLDERS, typeof(bool) }
                };
        }

        public MenuItem Setup(BasicUser User, DatabaseConnection Connection, string internalName, Dictionary<string, object> setParameters)
        {
            MySetup.conn = Connection;
            MySetup.internalName = internalName;
            MySetup.user = User;
            MySetup.settings = setParameters;
            return MenuItemBuilder.BuildInitial("Profile Manager", (i, e) => { new ProfileManager().ShowDialog(); });
        }
    }
    public class SEIDR_PROFILEMANAGER_METADATA : ExportAttribute, SEIDR_WindowMenuAddOn_MetaData
    {
        public bool HasParameterMapping
        {
            get
            {
                return true;
            }
        }

        public string Name
        {
            get
            {
                return "SEIDR.Loader Profile Manager";
            }
        }

        public Dictionary<string, Type> parameterInfo
        {
            get
            {
                return new Dictionary<string, Type>
                {
                    { MySetup.ANDROMEDA_BASE_FOLDER, typeof(string) },
                    { MySetup.INTEGRATION_SERVICES, typeof(string) },
                    { MySetup.PACKAGE_FOLDER, typeof(string) },
                    { MySetup.SANDBOX_BASE_FOLDER, typeof(string) },
                    { MySetup.CHECK_FOLDERS, typeof(bool) }
                };
            }
        }

        public bool RequirePermission
        {
            get
            {
                return false; //Require team instead. Note that super admin will be able to use either way
            }
        }

        public bool RequireSessionCache
        {
            get
            {
                return true;
            }
        }

        public bool Singleton
        {
            get
            {
                return true;
            }
        }

        public string Team
        {
            get
            {
                return "ETL";
            }
        }

        public bool UsesCaller
        {
            get
            {
                return false;
            }
        }
    }
    internal static class MySetup
    {
        public static DatabaseConnection conn { get; set; }
        public static BasicUser user { get; set; }
        public static string internalName { get; set; }
        public static Dictionary<string, object> settings;
        public static SEIDR_Window Window { get; set; }
        public const string CHECK_FOLDERS = "Check Folder Setup";
        public const string PACKAGE_FOLDER = "Package Folder";
        public const string INTEGRATION_SERVICES = "Integration Services Server";
        public const string TEMPLATE_FOLDER = "Template Folder";
        public const string ANDROMEDA_BASE_FOLDER = "Andromeda Base Folder";
        public const string SANDBOX_BASE_FOLDER = "Sandbox Base Folder";
    }
}
