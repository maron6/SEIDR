using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Configurations;
using System.Windows.Media.Imaging;
using SEIDR.Dynamics;
using SEIDR.Dynamics.Configurations.UserConfiguration;
using SEIDR.Dynamics.Configurations.QueryConfiguration;
using SEIDR.Dynamics.Configurations.DatabaseConfiguration;
using SEIDR.Dynamics.Configurations.ContextMenuConfiguration;
using SEIDR.Dynamics.Configurations.AddonConfiguration;
using SEIDR.Dynamics.Windows;

namespace SEIDR.WindowMonitor
{
    public abstract class SessionWindow: BasicSessionWindow
    { 
        
        public UserSessionManager MySession
        {
            get
            {
                return SessionManager as UserSessionManager;
            }
        }
        public MiscSetting myMiscSettings
        {
            get { return MySession.MySettings; }
            set { MySession.MySettings = value; }
        } 

        #region Broker properties
        public ConfigurationListBroker MyBroker { get { return MySession.Broker; } }       
        public QueryList myQueries { get { return MyBroker.Queries; } } 
        public ContextMenuList myContextMenus { get { return MyBroker.ContextMenus; } }
        public DatabaseList myConnections { get { return MyBroker.Connections; } }
        public ContextAddonList myContextAddons { get { return MyBroker.ContextAddons; } }
        public WindowAddonList myWindowAddons { get { return MyBroker.WindowAddons; } }
        #endregion

        public SessionWindow() : base(false)
        {
            //Never create as a safe window


            //Icon = new BitmapImage(new Uri(@"seidr_G9S_icon.ico", UriKind.Relative));
            //Add this in again and copy the icon locally if not seeing the icon in release mode later..
        }
        public SessionWindow(bool SafeWindow) : base(SafeWindow) { }
        public SessionWindow(bool SafeWindow= false, 
            UserAccessMode requiredAccess = UserAccessMode.None,
            UserAccessMode excludedAccess = UserAccessMode.None,
            BasicUserPermissions requirePermission = BasicUserPermissions.None)
            :base(SafeWindow, requiredAccess, excludedAccess, requirePermission) { }
        //public static void SetExceptionManager(ExceptionManager exM)
        //{
        //    WindowExceptionManager = exM;
        //}
        public int? SWID
        {
            get
            {
                return MySession?.CurrentUser?.ID;
            }
        }
        public WindowUser MyCurrentUser
        {
            get
            {
                return MySession?.CurrentUser;
            }
        }
        /// <summary>
        /// Gets a clone of the current user as a BasicUser if the ID is populated
        /// </summary>
        public BasicUser MyBasicUser
        {
            get
            {
                var uw = SessionManager.CurrentBasicUser;
                if (uw.UserID.HasValue)
                    return uw.Clone();
                return null;
                //return SessionManager.CurrentBasicUser;
            }
        }
        /// <summary>
        /// Hides the BasicSessionManager version of ShowDialog and handles InvalidBasicUserSessionExceptions 
        /// <para>that can be thrown by session management</para>
        /// </summary>
        /// <returns></returns>
        public new bool ShowDialog()
        {
            try
            {
                return base.ShowDialog() ?? false;
            }
            catch(InvalidBasicUserSesssionException ex)
            {
                MyBroker.MyExceptionManager.Handle(ex, null, ExceptionLevel.UI_Basic);
                return false;
            }
        }
        //public MiscSetting MySettings { get; set; } //This should be handled by iConfigManager, because it's used in there
    }
}
