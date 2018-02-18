using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Security.Principal;
using System.Windows.Documents;
using SEIDR.Dynamics.Configurations.UserConfiguration;
using System.Data;

namespace SEIDR.Dynamics.Configurations
{
    /// <summary>
    /// Handles interaction with the Session manager and allows access to information like the current BasicUser and the session's cache.
    /// </summary>
    public abstract class BasicSessionWindow : Window
    {
        #region user Access to windows
        public bool CanAccess
        {
            get
            {
                if (!_SessionManager.CurrentBasicUser.CheckPermission(RequiredPermissions))
                    return false;
                if ((CurrentAccessMode & ExcludeAccessMode) != UserAccessMode.None)
                    return false;
                return (CurrentAccessMode & RequiredAccessMode) == RequiredAccessMode;
            }
        }
        public const UserAccessMode MultiUserAccess = UserAccessMode.Impersonation | UserAccessMode.Team;


        UserAccessMode RequiredAccessMode { get; set; } = UserAccessMode.None;
        UserAccessMode ExcludeAccessMode { get; set; } = UserAccessMode.None;
        BasicUserPermissions RequiredPermissions { get; set; } = BasicUserPermissions.None;

        protected ConfigurationListBroker SessionBroker{ get { return _SessionManager.Broker; } }
        #endregion


        /// <summary>
        /// Pages the datatable and returns a copy of the rows from the calculated page as a DataView
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public DataView PageDataTable(DataTable dt,  int page, int pageSize)
        {
            var dr = dt.Rows.Cast<DataRow>();
            if (page == 0)
                return dr.Take(pageSize).CopyToDataTable().AsDataView();
            else
                return dr.Skip(page * pageSize).Take(pageSize).CopyToDataTable().AsDataView();
        }

        /// <summary>
        /// Gets the CurrentAccessMode from session manager.
        /// <para>Note that if the session manager is somehow null, it will return SINGLEUSER mode.</para>
        /// </summary>
        public static UserAccessMode CurrentAccessMode {
            get
            {
                if (SessionManager == null)
                    return UserAccessMode.None; 
                return SessionManager.CurrentAccessMode;
            }
        }
        /// <summary>
        /// Check if the session manager is in single user mode
        /// </summary>
        public static bool SingleUserMode { get { return SessionManager.CurrentAccessMode == UserAccessMode.SingleUser; } }
        #region session color - force background color
        static Brush _sessionColor = null;
        static SolidColorBrush _sessionText = null;
        /// <summary>
        /// Background color for window - forced by session. Defaults to LightSteelBlue if set to null.
        /// </summary>
        public static Brush sessionColor
        {
            get { return _sessionColor ?? Brushes.LightSteelBlue; }
            set
            {
                _sessionColor = value;                
            }
        }
        /// <summary>
        /// Brush color specified for text. Has to be used manually
        /// </summary>
        public static SolidColorBrush sessionText
        {
            get
            {
                return _sessionText ?? Brushes.Black;
            }
            set
            {
                _sessionText = value;
            }
        }
        /// <summary>
        /// Sets the session color for forcing background on pages that have useSessionColor set to true when showDialog is called
        /// <para> or the window activates</para>
        /// <para>Instance method that also sets the session color.</para>
        /// </summary>
        /// <param name="color">String color, should be able to work with BrushConverter.  </param>
        public void SetSessionColor(string color = "LightSteelBlue", string TextColor  = "Black")
        {
            if (color.ToUpper() == "DEFAULT")
                color = "LightSteelBlue";
            if (TextColor.ToUpper() == "DEFAULT")
                TextColor = "Black";
            if (color == null)
            {
                _sessionColor = null;
                _sessionText = null;
                if (UseSessionColor)
                {
                    Background = sessionColor; //default.
                    //SetValue(TextElement.ForegroundProperty, sessionText);
                }
                return;
            }
            try
            {
                Brush b = (Brush)new BrushConverter().ConvertFromString(color);
                SolidColorBrush t = (SolidColorBrush)new BrushConverter().ConvertFromString(TextColor);
                sessionColor = b;
                sessionText = t;
                if (UseSessionColor)
                {
                    Background = b ?? sessionColor; //should not be null
                    //SetValue(TextElement.ForegroundProperty, t ?? sessionText);
                }
            }
            catch(Exception ex)
            {
                Handle(ex, null, ExceptionLevel.Background);
                return;
            }            
        }
        bool _useSessionColor = true;
        /// <summary>
        /// If true, this window will set the background to the value of session color
        /// </summary>
        public bool UseSessionColor
        {
            get { return _useSessionColor; }
            protected set
            {
                if (SafeWindow)
                {
                    _useSessionColor = false;
                    return;
                }                    
                _useSessionColor = value;
                if (value)
                {
                    base.Background = sessionColor;
                    //SetValue(TextElement.ForegroundProperty, sessionText);
                }
            }
        }
        #endregion
        /// <summary>        
        /// As a setter - Sets the session manager if it has not been set yet. <para>Setter does
        /// nothing if session manager is already set.</para>
        /// </summary>
        public static BasicUserSessionManager SessionManager
        {
            get
            {
                return _SessionManager;
            }
            set
            {
                if (_SessionManager != null)
                    return;
                _SessionManager = value;
            }
        }
        static BasicUserSessionManager _SessionManager;        
        #region Event registration and exceptions
        bool _EventsRegistered = false;
        /// <summary>
        /// Window is currently registered with the session manager
        /// </summary>
        public bool Registered = false;
        /// <summary>
        /// Window is active AND registered as so with the session manager
        /// </summary>
        public bool ActiveRegistered = false;
        /// <summary>
        /// Implementation Class's type Name
        /// </summary>
        public string WindowName
        {
            get
            {
                return GetType().Name;
            }
        }
        /// <summary>
        /// Allows logging to exception file for main application. Also displays the error message depending on user settings.
        /// <para>Should be set by the Main Application and not any plug-ins</para>
        /// </summary>
        public static ExceptionManager WindowExceptionManager
        {
            get { return _SessionManager.Broker.MyExceptionManager; }
        }        
        public void Handle(string Message, ExceptionLevel exLevel = ExceptionLevel.UI)
        {
            if(WindowExceptionManager != null)
                WindowExceptionManager.Handle(Message, exLevel, window: this);
        }
        public void Handle(Exception ex, string Message = null, ExceptionLevel exLevel = ExceptionLevel.UI)
        {
            if (WindowExceptionManager != null)
                WindowExceptionManager.Handle(ex, Message, exLevel, window:this);
        }
        #endregion

        /// <summary>
        /// I don't remember what this was going to be for!!!
        /// </summary>
        bool SafeWindow;        
        /// <summary>
        /// Basic Session window
        /// </summary>
        /// <param name="SafeWindow">If true, does not contribute to active window limit. Will also force background to be light steel blue</param>        
        /// <param name="requiredAccessMode">If non null, will check to make sure that current Access mode matches when Showing or reactivating the window</param>
        public BasicSessionWindow(bool SafeWindow = false, 
            UserAccessMode requiredAccessMode = UserAccessMode.None, 
            UserAccessMode ExcludedAccessMode = UserAccessMode.None, 
            BasicUserPermissions requiredPermission = BasicUserPermissions.None)
        {
            RequiredAccessMode = requiredAccessMode;
            ExcludeAccessMode = ExcludedAccessMode ;
            RequiredPermissions = requiredPermission;
            //WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (SafeWindow)
            {
                UseSessionColor = false;
                Background = Brushes.LightSteelBlue;                
            }            
            RegisterEvents();
            this.SafeWindow = SafeWindow;            
        }
        /// <summary>
        /// Close the window after setting DialogResult, if showing Modally.
        /// </summary>
        /// <param name="success">Dialog result to be returned by ShowDialog, if the window was opened that way</param>
        protected void Finish(bool success = true)
        {
            if (_Dialog)
                DialogResult = success;
            Close();
        }
        /// <summary>
        /// Parameterless Constructor
        /// </summary>
        public BasicSessionWindow()
            :this(false, UserAccessMode.None, UserAccessMode.None, BasicUserPermissions.None)
        {            
        }        
        private void RegisterEvents()
        {
            if (_EventsRegistered)
                return;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Unloaded += SessionWindow_Unloaded;
            Activated += SessionWindow_Activated;
            Deactivated += SessionWindow_Deactivated;
            Closed += SessionWindow_Closed;
            _EventsRegistered = true;
        }
        private void SessionWindow_Closed(object sender, EventArgs e)
        {           
            if (Registered)            
                SessionManager.UnregisterWindow(this);                            
        }
        /// <summary>
        /// Called when session is stopped by a dispatcher. Sets the result to false (if it's a Dialog) and closes.         
        /// </summary>
        public void SessionStop()
        {
            if (Registered)
            {
                if(_Dialog)
                    DialogResult = false;
                Close();
            }
            else            
                CanShowWindow = false;       //I'm not sure how this would actually happen but I did see some stuff with Alerts...Maybe timing with opening the window and causing a breakpoint or something...            
        }
        private void SessionWindow_Deactivated(object sender, EventArgs e)
        {
            //if (SessionManager == null)
            //    return; //ActiveRegistered only true if session manager exists
            if (ActiveRegistered)            
                SessionManager.DeativateWindow(this);                
            
            //if (unreg)
            //{
            //    SessionManager.UnregisterWindow(this);
            //    Registered = false;
            //}
        }

        private void SessionWindow_Activated(object sender, EventArgs e)
        {            
            //if (SessionManager == null || ActiveRegistered)
            //{
            //    if (RequiredAccessMode != CurrentAccessMode && RequiredAccessMode != UserAccessMode.None)                
            //        SessionStop();                                    
            //    return;
            //}
            if(SessionManager != null && ! ActiveRegistered)
                SessionManager.ReactivateWindow(this);
            if (RequiredAccessMode != CurrentAccessMode && RequiredAccessMode != UserAccessMode.None)
            {
                SessionStop();
                return;
            }            
            //SessionManager.RegisterWindow(this, SafeWindow);
            //Registered = true;            
        }

        private void SessionWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (Registered)            
                SessionManager.UnregisterWindow(this);                                
            
        }
        /// <summary>
        /// If set to false during window setup, will immediately return false in ShowDialog and not register or show the new window
        /// </summary>
        public bool CanShowWindow { get; protected set; } = true;
        /// <summary>
        /// Try to Override the page setting for using SessionColor and shows the dialog.
        /// </summary>
        /// <param name="UseSessionColor">Overrides UseSessionColor<see cref="BasicSessionWindow.UseSessionColor"/></param>
        /// <returns></returns>
        public bool? ShowDialog(bool UseSessionColor)
        {
            if(!SafeWindow)
                this.UseSessionColor = UseSessionColor;
            return ShowDialog();
        }
        
        /// <summary>
        /// Hides the <see cref="Window.ShowDialog"/> to interact with the session manager
        /// </summary>
        /// <returns></returns>
        public new bool? ShowDialog()
        {
            _Dialog = true;
            try
            {                
                if (!CanShowWindow 
                    //|| ((RequiredAccessMode ?? CurrentAccessMode) != CurrentAccessMode)
                    //|| ExcludeAccessMode == CurrentAccessMode
                    || !CanAccess
                    )
                    return false;
                RegisterEvents();
                if (SessionManager != null)
                    SessionManager.RegisterWindow(this, SafeWindow);
                if (UseSessionColor)
                {
                    Background = sessionColor;
                    //SetValue(TextElement.ForegroundProperty, sessionText); //Wasn't finding the property, "property not found on 'object'"
                    //TextElement.SetForeground(T, sessionText);
                }
                /* Owner is not set...
                if (Owner != null)
                {                    
                    Topmost = Topmost || Owner.Topmost; //Doesn't work well to have a TopMost open a non Topmost
                }*/
                return base.ShowDialog();
                /*  To enable below, comment out this line and the above...
                bool ? result = base.ShowDialog();
                _Dialog = false;
                return result; 
                // */
            }
            catch (Exception ex)
            {
                if(WindowExceptionManager != null)
                    Handle(ex, exLevel:ExceptionLevel.Background);
                return false; //Prevent app from crashing as a result of not catching after logging.
                //throw; 
                
                //Make sure that if we aren't able to open the window because of too many being open, then
                // we don't try to continue to show the dialog after the error is handled
            }
        }
        /// <summary>
        /// Try to override the useSessionColor setting on the page and then calls BasicSessionWindow's <see cref="Show"/>
        /// </summary>
        /// <param name="UseSessionColor">Overrides <see cref="BasicSessionWindow.UseSessionColor"/>, unless <see cref="SafeWindow"/> is true</param>
        /// <returns></returns>
        public bool Show(bool UseSessionColor)
        {
            if (!SafeWindow)
                this.UseSessionColor = UseSessionColor;
            return Show();
        }
        /// <summary>
        /// Does preparation for showing window, then calls <see cref="Window.Show"/> - returns false if the window cannot be shown, otherwise true.
        /// </summary>
        /// <returns></returns>
        public new bool Show()
        {
            _Dialog = false;
            try
            {
                if (!CanShowWindow 
                    || !CanAccess
                    )                    
                    return false;
                RegisterEvents();
                if (SessionManager != null)
                    SessionManager.RegisterWindow(this, SafeWindow);
                if (UseSessionColor)
                {
                    Background = sessionColor;
                    //SetValue(TextElement.ForegroundProperty, sessionText);
                }
                if (Owner != null)
                {
                    //Left = Owner.Left;
                    //Top = Owner.Top;
                    Topmost = Topmost || Owner.Topmost;
                }
                base.Show();
                return true;
            }
            catch (Exception ex)
            {
                if (WindowExceptionManager != null)
                    Handle(ex, exLevel: ExceptionLevel.Background);
                return false; //Prevent app from crashing as a result of not catching
                              //throw; 

                //Make sure that if we aren't able to open the window because of too many being open, then
                // we don't try to continue to show the dialog after the error is handled
            }
        }
        /// <summary>
        /// Set when show methods are called: true if the window was started by <see cref="ShowDialog"/>
        /// </summary>
        protected bool _Dialog { get; private set; } = false;
    }
}
