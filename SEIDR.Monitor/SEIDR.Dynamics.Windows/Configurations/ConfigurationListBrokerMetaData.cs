using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Dynamics.Configurations
{
    [InheritedExport]
    public interface ConfigurationListBrokerMetaData
    {
        /// <summary>
        /// Short Description
        /// </summary>
        string Key { get; }
        /// <summary>
        /// Version of broker. Defaults to 1d
        /// </summary>
        [DefaultValue(1d)]
        double Version { get; }
        /// <summary>
        /// Description that users will see when choosing the configuration broker.
        /// <para>Will only be seen if user has more than one configuration broker DLL in the folder.</para>
        /// </summary>
        string Description { get; }
        /// <summary>
        /// Requires that the user credentials are validated. 
        /// <para>If false, will skip calling Validate and go straight to requesting the user object.</para>
        /// <para>Default: true.</para>
        /// </summary>
        [DefaultValue(true)]
        bool RequireLogin { get; }
        ///// <summary>
        ///// Allows the user to access single user mode from their misc settings.
        ///// <para>Overrides <paramref name="RequireLogin"/> and instantly grabs the singleton SingleUser. </para>
        ///// <para>Probably should not be used unless using a local DB or the default (loading from serialization)</para>
        ///// <para>Default: false</para>
        ///// </summary>
        //[DefaultValue(false)]
        //bool AllowSingleUserMode { get; }
        /// <summary>
        /// Require the user to go to single user mode.
        /// <para>Use if no support for teams or multiple users - will be local mode only</para>
        /// </summary>
        [DefaultValue(false)]
        bool SingleUserMode { get; }
        /// <summary>
        /// Amount of time for user to stay logged in while application is inactive. Negative value is no logout.
        /// <para>Default value is 60 (minutes)</para>
        /// </summary>
        [DefaultValue(60)]
        int LoginDuration { get; }
        /// <summary>
        /// If true, supports use of <see cref="ConfigurationListBroker.Impersonate(LoginInfo, LoginInfo)"/>
        /// </summary>
        [DefaultValue(false)]
        bool SupportImpersonation { get; }
        /// <summary>
        /// Populate if non single user mode and using default saving/loading for configurations
        /// </summary>
        [DefaultValue(null)]
        string NetworkPath { get; }
    }
}
