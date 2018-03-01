using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Dynamic;
using System.Timers;
using System.DirectoryServices.AccountManagement;
using SEIDR.Dynamics.Configurations;
using SEIDR.WindowMonitor.ConfigurationBroker;
using SEIDR.Dynamics.Configurations.UserConfiguration;
using SEIDR.WindowMonitor.MonitorConfigurationHelpers;
//using Ryan_UtilityCode.Dynamics.Configurations.Encryption;
//using System.DirectoryServices.AccountManagement;
//using System.DirectoryServices;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for LoginSplash.xaml. Probably will go unused but for practice/as something to think about
    /// </summary>
    public partial class LoginSplash : Window
    {
        //ToDO: load broker library.
        ConfigurationListBroker Broker = new DefaultBroker();
        ConfigurationListBrokerMetaData BrokerMetaData = null;
        BrokerLibrary library;
        
        //probably going to move impersonation to a menu in the user manager or something..
        const string WARNING_BAD_IMPERSONATION = "NOT AUTHENTICATED - UNABLE TO IMPERSONATE A BASIC TEAM MEMBER TEAM WITH THIS LOGIN.";
        //const string WARNING_BAD_DOMAIN = "NOT AUTHENTICATED - THIS BUILD OF SEIDR.WINDOW HAS NOT BEEN CONFIGURED FOR YOUR LOGIN DOMAIN.";
        const string WARNING_BAD_LOGIN = "NOT AUTHENTICATED - INVALID CREDENTIALS";
        const string WARNING_NO_USER = "NOT AUTHENTICATED - UNABLE TO FIND USER";
        public static readonly string[] VALID_DOMAINS = new string[] { };
        //Timer logout;
        Timer sessionTimer;
        dynamic login;
        //User self;
        WindowUser self;
        
        /// <summary>
        /// Something to think about but Will probably go unused, but... if used, it should replace MainWindow page by updating the startup uri in App.xaml
        /// <para>
        /// Should load users and then try to set iConfigManager.Me using the login and pw. Would need to add a non visible password
        /// to user then...
        /// </para>
        /// </summary>
        public LoginSplash()
        {
            #region setup login validation dynamic object.
            login = new ExpandoObject();
            login.UserName = Environment.UserDomainName + "\\" + Environment.UserName;
            login.DefaultDomain = Environment.UserDomainName + "\\";
            login.CanTryLogin = false;
            login.GetInfo = new Func<LoginInfo>(() => LoginInfo.Parse(login.UserName));
            /*
            login.CheckName = new Func<string>(() => 
            {
                string v = login.UserName;                
                if (login.UserName.IndexOf("\\") < 0)
                {
                    if(v.Contains("@"))
                    {
                        string[] parts = v.Split('@');
                        v = parts[1] + "\\" + parts[0];
                    }
                    else
                        v = login.DefaultDomain + login.UserName;
                }
                return v;
            });
            login.CheckDomain = new Func<string, bool>((string v) =>
            {
                if (v[0] == '#')
                {
                    //Only allow if the team exists and the user is an admin. 
                    if(SettingManager.CheckTeamExist(v.Substring(1)))
                    {
                        User check = SettingManager.GetUser(User.GetEnvironmentName());
                        if (check == null || !check.IsAdmin)
                            return false;
                        return true;
                    }
                    return false;                        
                }
                string[] x = v.Split('\\');
                if (x.Length != 2)
                    return false;
                if (VALID_DOMAINS == null || VALID_DOMAINS.Length == 0)
                    return true;
                return VALID_DOMAINS.Contains(x[0]);
            });
            */
            #endregion                        
            
            sessionTimer = new Timer();
            var MinuteConversion = 60 * 1000; //MS -> Minutes
            sessionTimer.Interval = 1d * MinuteConversion;
            sessionTimer.Elapsed += SessionTimer_Elapsed;

            InitializeComponent();
            /*if (VALID_DOMAINS.Length > 0 && !VALID_DOMAINS.Contains(Environment.UserDomainName))
            {
                sExceptionManager.Handle("Invalid Computer domain!", SEIDR.Dynamics.ExceptionLevel.UI_Basic);
                this.Close();
                return;
            }*/
            pwBox.Focus();
            Warning.Visibility = Visibility.Collapsed;
            if (Keyboard.IsKeyToggled(Key.CapsLock))            
                CAPSLOCK_WARNING.Visibility = Visibility.Visible;            
            else            
                CAPSLOCK_WARNING.Visibility = Visibility.Collapsed;            
            DataContext = login;            
            //self = iConfigManager.Me;
            Application myApp = Application.Current;
            myApp.MainWindow = this;
            library = LibraryManagement.GetBrokerLibrary();
            if (library.BrokerCount > 0)
                LoginMenu.Visibility = Visibility.Visible;
            // If BrokerCount == 1, will use that meta data. otherwise, start with the default
            UpdateForBroker(library.DefaultBrokerMetaData, library.BrokerCount == 0);           
            
            //myApp.Activated += MyApp_Activated;
            //myApp.Deactivated += MyApp_Deactivated;
            //myApp.SessionEnding += MyApp_Deactivated;
            //SettingManager.InitLoad();
            /*
            if (SettingManager.myMiscSettings?.SkipLogin ?? false)
            {
                //Check for valid user...
                User u = SettingManager.GetUser($@"{Environment.UserDomainName}\{Environment.UserName}");
                if (u == null)
                {
                    switch (SettingManager.myMiscSettings.MyAccessMode)
                    {
                        case UserAccessMode.SINGLEUSER:
                            {
                                u = User.SingleUserMode;
                                break;
                            }
                        case UserAccessMode.Impersonation:
                        case UserAccessMode.Team:                            
                        default:
                            {
                                sExceptionManager.Handle("User not found!", SEIDR.Dynamics.ExceptionLevel.UI_Basic);
                                Close();
                                return;
                            }
                    }
                }
                //session = new UserSessionManager(u, SettingManager.myMiscSettings.MyAccessMode, - 1);                
                session = new UserSessionManager(self, UserAccessMode.Team, -1);
                session.MySettings = localSettings;
                session.Setup(Broker, BrokerMetaData);                
                BasicSessionWindow.SessionManager = session;
                //SettingManager.Setup(session);
                //SessionWindow.SetExceptionManager(Broker.MyExceptionManager);
                ShowInTaskbar = false;
                Visibility = Visibility.Hidden;
                var mw = new MainWindow();
                myApp.MainWindow = mw;
                var r= mw.ShowDialog();
                Close();
            }
            */
        }
        UserSessionManager session {
            //get { return SettingManager.__Session__; }
            //set { SettingManager.__Session__ = value; }
            get { return BasicSessionWindow.SessionManager as UserSessionManager; }
            set { BasicSessionWindow.SessionManager = value; }
        }
        MiscSetting localSettings { get; set; } = MiscSetting.LoadFromFile();
        public void UpdateForBroker(ConfigurationListBrokerMetaData info, bool skipLoginButton = false)
        {
            Broker = library.GetBroker(info);
            login.UserName = Environment.UserDomainName + "\\" + Environment.UserName;
            pwBox.Clear();
            Warning.Visibility = Visibility.Hidden;
            bool ForcedDefault = false;            
            if (Broker != null)            
                BrokerMetaData = info;                            
            else
            {                
                Broker = new DefaultBroker();
                BrokerMetaData = DefaultBroker.DefaultMetadata;                
                if (info != null)
                    ForcedDefault = true;                    
            }            
            Broker.Setup(
                ConfigFolder.GetFolder(MiscSetting.APP_NAME, MiscSetting.SETTING_SUBFOLDER),
                BrokerMetaData.NetworkPath /*localSettings?.NetworkFolder*/, //Don't let users choose..
                new Dynamics.ExceptionManager(MiscSetting.APP_NAME,
                                                localSettings.ErrorLog,
                                                localSettings.MyExceptionAlertLevel)
                                            );
            Broker.LoadConfigurations(true, BrokerMetaData.SingleUserMode);
            if(ForcedDefault)
                Broker
                    .MyExceptionManager
                    .Handle("Unable to load specified Configuration!", Dynamics.ExceptionLevel.UI_Basic);

            //Meta data changes.
            pwBox.IsEnabled = BrokerMetaData.RequireLogin;
            //pwBox.Visibility = BrokerMetaData.RequireLogin ? Visibility.Visible : Visibility.Hidden;
            loginNameTB.IsEnabled = BrokerMetaData.RequireLogin;
            if (!BrokerMetaData.RequireLogin)
            {
                Login.Content = "Ready";
                CAPSLOCK_WARNING.Visibility = Visibility.Collapsed;
                login.CanTryLogin = true;
                Login.Focus();
            }
            else
            {
                Login.Content = "Log In";
                pwBox.Focus();
            }
            if (skipLoginButton)
                OpenMain();
        }
        private void SessionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (session.CheckLogout())
            {
                localSettings = MiscSetting.LoadFromFile();   
                Dispatcher.Invoke(RestoreLoginCheck);
            }
        }
        public void RestoreLoginCheck()
        {
            pwBox.Clear();//Should already be cleared as soon as login is successful actually.
            Warning.Visibility = Visibility.Hidden;
            Visibility = Visibility.Visible;
            ShowInTaskbar = true;
            sessionTimer.Stop();
        }        
        [Obsolete("See the SessionManager logout")]
        private void Logout_Timer_Callback(object state)
        {
            //deprecated
            var windowList = Application.Current.Windows;
            foreach(Window w in windowList)
            {
                if (w.GetType() == typeof(LoginSplash))
                    continue;
                w.DialogResult = false;
                w.Close();
            }
        }
       
        void SetUserWaiting(bool MustWait = true)
        {
            IsEnabled = !MustWait;
            if (MustWait)           
                Cursor = Cursors.Wait;                            
            else            
                Cursor = Cursors.Arrow;                           
        }
        
        public void OpenMain()
        {
            SetUserWaiting();            
            LoginInfo info;
            if (BrokerMetaData.SingleUserMode)
            {
                self = WindowUser.SingleUserMode;
                info = new LoginInfo
                {
                    Domain = self.Domain,
                    UserName = self.UserName
                };
                //Broker.SetUser(self);
            }
            else
            {
                info = login.GetInfo();
                if (BrokerMetaData.RequireLogin
                    && !Broker.ValidateCredential(info, pwBox.Password)
                    //&& !Validate() 
                    )
                {
                    Warning.Text = WARNING_BAD_LOGIN;
                    Warning.Visibility = Visibility.Visible;
                    SetUserWaiting(false);
                    return;
                }
                pwBox.Clear();
                Broker.LogIn(info);
                self = Broker.ConfiguredUser;
                if (self == null) /* Credentials work, but user doesn't exist and cannot be added */
                {
                    Warning.Text = WARNING_NO_USER;
                    Warning.Visibility = Visibility.Visible;
                    SetUserWaiting(false);
                    return;
                }
            }            
            Broker.SetUser(self, BrokerMetaData.SingleUserMode);
            Broker.PostLoginSetup();
            Visibility = Visibility.Collapsed;
            if (session == null 
                || info.UserName != session.CurrentUser.UserName
                || info.Domain != session.CurrentUser.Domain) //if first login or login has changed. 
                    //Login name changing probably won't be used unless there's a big change to iConfigManager, though
            {                
                //Note: no impersonation from login.
                session =
                    new UserSessionManager(self, //Broker.LogIn(login.GetInfo()), 
                    BrokerMetaData.SingleUserMode ? UserAccessMode.SingleUser : UserAccessMode.Team,
                    /*SettingManager.myMiscSettings.MyAccessMode,*/
                    BrokerMetaData.LoginDuration
                    //localSettings.LogoutTime
                    );                         
                session.ActiveUserEvent += Session_ActiveUserEvent;
                session.InactiveUserEvent += Session_InactiveUserEvent;
                session.Setup(Broker, BrokerMetaData);
                BasicSessionWindow.SessionManager = session;
                
                //SettingManager.Setup(session);                
                //SessionWindow.SetExceptionManager(
                //        new SEIDR.Dynamics.ExceptionManager(SettingManager.MyAppName,
                //                                                        SettingManager.myMiscSettings.ErrorLog,
                //                                                        SettingManager.myMiscSettings.MyExceptionAlertLevel)
                //        );
            }
            else
            {
                //if (login.UserName != session.CurrentUser.UserName)                
                //    session.UpdateUserFromLogin(Broker.LogIn(info), BrokerMetaData.SingleUserMode);
                session.LogIn();
            }
            SetUserWaiting(false);
            ShowInTaskbar = false;
            var r = new MainWindow().ShowDialog();            
            if (r  || !session.LoggedIn)
            {
                ShowInTaskbar = true;
                Visibility = Visibility.Visible;
                return;//Closed by timeout, logout, or other message.                 
                /*
                 * if no value ( ?? false ), closed by user closing with X button or something, 
                 * then this if block won't be entered. Unless also logged out by session manager.
                 */
            }
            Close(); //done.
        }

        private void Session_InactiveUserEvent(object sender, UserActivityEventArgs e)
        {
            sessionTimer.Start(); //runs before the reactivate, so timer will be stopped again if user is switching to another window
        }

        private void Session_ActiveUserEvent(object sender, UserActivityEventArgs e)
        {
            /*
            if (session.IsUserActive) //if user is already active, don't need to worry about this...
                return;*/
            sessionTimer.Stop();
            //session.SlideLogoutTime(); //Use the default slide time to extend user session
            //session.UpdateLogoutTime(SettingManager.myMiscSettings.LogoutTime);
            session.UpdateLogoutTime(session.MySettings.LogoutTime);
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            OpenMain();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            /*
            if (login.UserName.IndexOf("\\") < 0)
                login.UserName = Environment.UserDomainName + "\\" + login.UserName;
            */
            if (string.IsNullOrWhiteSpace(login.UserName))
                login.CanTryLogin = false;
            else if (!string.IsNullOrWhiteSpace(pwBox.Password))
                login.CanTryLogin = true;
        }

        private void pwBox_PasswordChanged_Check_CanTryLogin(object sender, RoutedEventArgs e)
        {            
            if (string.IsNullOrWhiteSpace(pwBox.Password))
                login.CanTryLogin = false;
            else if (!string.IsNullOrWhiteSpace(login.UserName))
                login.CanTryLogin = true;
            if(Console.CapsLock)
            //if (Keyboard.IsKeyToggled(Key.CapsLock))
            {
                CAPSLOCK_WARNING.Visibility = Visibility.Visible;
            }
            else
            {
                CAPSLOCK_WARNING.Visibility = Visibility.Collapsed;
            }
        }

        private void pwBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyToggled(Key.CapsLock))
            {
                CAPSLOCK_WARNING.Visibility = Visibility.Visible;
            }
            else
            {
                CAPSLOCK_WARNING.Visibility = Visibility.Collapsed;
            }
            if (login.CanTryLogin && e.Key == Key.Enter)
            {
                //Login_Click(sender, RoutedEventArgs.Empty as RoutedEventArgs);
                OpenMain();
                
            }            
        }

        private void BrokerPicker_Click(object sender, RoutedEventArgs e)
        {            
            if (library.BrokerCount > 0)
            {
                ConfigurationBrokerPicker picker = new ConfigurationBrokerPicker(library.GetBrokerInfo());
                if (picker.ShowDialog().Value)
                {
                    UpdateForBroker(picker.Picked);                    
                    ResetBroker.IsEnabled = picker.Picked != null;
                }                
            }
            else
            {
                //sExceptionManager.Handle("No configurations found! Using Default.", Dynamics.ExceptionLevel.UI_Basic);                
                UpdateForBroker(null, true);
                Broker.MyExceptionManager.Handle("No Configurations found! Using Default.", Dynamics.ExceptionLevel.Background);
                ResetBroker.IsEnabled = false;
                Configuration.Visibility = Visibility.Hidden;
            }
            e.Handled = true;
        }

        private void ResetBroker_Click(object sender, RoutedEventArgs e)
        {
            UpdateForBroker(null);
            ResetBroker.IsEnabled = false;
            e.Handled = true;
        }
        /*
private void Password_Click(object sender, RoutedEventArgs e)
{
SaveNewPassword(Password.Content.ToString());
}*/
    }
}
