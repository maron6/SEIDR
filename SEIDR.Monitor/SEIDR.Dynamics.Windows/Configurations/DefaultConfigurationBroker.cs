using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Configurations.Encryption;
using System.ComponentModel.Composition;
using SEIDR.Dynamics.Configurations.UserConfiguration;

namespace SEIDR.Dynamics.Configurations
{
    
    [Export(typeof(ConfigurationListBroker))]
    [ExportMetadata(nameof(ConfigurationListBrokerMetaData.SingleUserMode), false),
        ExportMetadata(nameof(ConfigurationListBrokerMetaData.Description), "Default Configuration Broker - Single User Mode"),
        ExportMetadata(nameof(ConfigurationListBrokerMetaData.Key), "Default -SingleUser"),
        ExportMetadata(nameof(ConfigurationListBrokerMetaData.LoginDuration), -1),
        ExportMetadata(nameof(ConfigurationListBrokerMetaData.RequireLogin), false),
        ExportMetadata(nameof(ConfigurationListBrokerMetaData.SupportImpersonation), false)]
    public class DefaultConfigurationBrokerSingleUser : ConfigurationListBroker
    {        
        /// <summary>
        /// 
        /// </summary>
        public DefaultConfigurationBrokerSingleUser()
            : base(7)
        {
        }
        /// <summary>
        /// Not supported in default broker.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="toImpersonate"></param>
        /// <returns></returns>
        public override WindowUser Impersonate(LoginInfo user, LoginInfo toImpersonate)
        {
            throw new NotImplementedException();
        }
    }
}
