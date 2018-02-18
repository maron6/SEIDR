using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Configurations;
using SEIDR.Dynamics.Configurations.UserConfiguration;
using System.DirectoryServices.AccountManagement;

namespace SEIDR.WindowMonitor.ConfigurationBroker
{
    /// <summary>
    /// To be used when no Brokers are found by the broker library
    /// </summary>
    internal class DefaultBroker : ConfigurationListBroker
    {        
        public override WindowUser Impersonate(LoginInfo user, LoginInfo toImpersonate)
        {
            throw new NotImplementedException();
        }         

        public override bool ValidateCredential(LoginInfo login, string password)
        {
            using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
            {
                return context.ValidateCredentials(login.UserName, password);
            }            
        }
        public static ConfigurationListBrokerMetaData DefaultMetadata { get; } 
            = new DefaultBrokerMetaData();
    }
    internal class DefaultBrokerMetaData : ConfigurationListBrokerMetaData
    {
        public string Description
        {
            get
            {
                return "Default Configuration";
            }
        }

        public string Key
        {
            get
            {
                return "DEFAULT";
            }
        }

        public int LoginDuration
        {
            get
            {
                return 60;
            }
        }

        public string NetworkPath
        {
            get
            {
                return null;
            }
        }

        public bool RequireLogin
        {
            get
            {
                return false;
            }
        }

        public bool SingleUserMode
        {
            get
            {
                return false;
            }
        }

        public bool SupportImpersonation
        {
            get
            {
                return false;
            }
        }

        public double Version
        {
            get
            {
                return 1d;
            }
        }
    }
}
