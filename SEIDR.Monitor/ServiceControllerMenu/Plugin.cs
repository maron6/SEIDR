using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using SEIDR.Dynamics.Configurations.UserConfiguration;
using SEIDR.Dynamics.Windows;
using System.ComponentModel.Composition;
//using SEIDR.Processing.Data.DBObjects;
using SEIDR.DataBase;

namespace ServiceControllerMenu
{
    //[ServiceControllerMenuMetaData]
    [Export(typeof(SEIDR_WindowMenuAddOn))]
    [ExportMetadata("Name", "Service Controller Menu")]
    [ExportMetadata("Singleton", false)]
    [ExportMetadata("RequireSessionCache", false)]
    [ExportMetadata("RequirePermission", false)]
    public class Plugin : SEIDR_WindowMenuAddOn
    {
        static SEIDR_Window caller;        
        public SEIDR_Window callerWindow
        {
            set
            {
                caller = value;
            }
        }
        public const string NameParameter = "Service Name";
        public const string LocationParameter = "Service Location";
        public Dictionary<string, Type> GetParameterInfo()
        {
            return new Dictionary<string, Type>
                {
                    { NameParameter, typeof(string) },
                    {LocationParameter, typeof(string) },
                };
        }

        public MenuItem Setup(BasicUser User, DatabaseConnection Connection, int internalName, Dictionary<string, object> setParameters)
        {
            string ServiceName = (string)setParameters[ServiceControllerMenuMetaData.NameParameter];
            MenuItem root = new MenuItem();
            ServiceInfo i = new ServiceInfo(ServiceName, setParameters[ServiceControllerMenuMetaData.LocationParameter] as string);
            MenuItem mi = new MenuItem { Header = ServiceName + " Status" };
            mi.GotFocus += i.HoverRefresh;
            MenuItem ConSta = new MenuItem
            {
                Header = "Start"
            };
            ConSta.GotFocus += i.EvalSS;
            ConSta.Click += i.StartStop;
            MenuItem Pauco = new MenuItem { Header = "Pause" };
            Pauco.GotFocus += i.EvalPC;
            Pauco.Click += i.PauseContinue;
            root.Items.Add(mi);
            root.Items.Add(ConSta);
            root.Items.Add(Pauco);
            return root;
        }
         
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple =false), MetadataAttribute]
    public class ServiceControllerMenuMetaData : ExportAttribute, SEIDR_WindowMenuAddOn_MetaData
    {

        public const string NameParameter = "Service Name";
        public const string LocationParameter = "Service Location";
        public string Name
        {
            get
            {
                return "Service Controller Menu";
            }
        }

        public Dictionary<string, Type> parameterInfo
        {
            get
            {
                return new Dictionary<string, Type>
                {
                    { NameParameter, typeof(string) },
                    {LocationParameter, typeof(string) },                    
                };
            }
        }

        public bool Singleton
        {
            get
            {
                return false;
            }
        }

        public bool RequirePermission
        {
            get
            {
                return false;
            }
        }

        public string Team
        {
            get
            {
                return null;
            }
        }

        public bool UsesCaller
        {
            get
            {
                return false;
            }
        }

        public bool HasParameterMapping
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool RequireSessionCache
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool Validate(BasicUser user)
        {
            return true;
        }
    }
}
