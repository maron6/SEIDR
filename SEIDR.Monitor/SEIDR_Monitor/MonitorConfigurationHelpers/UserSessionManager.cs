using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Configurations;
using SEIDR.WindowMonitor.MonitorConfigurationHelpers;
using SEIDR.Dynamics.Configurations.UserConfiguration;
using SEIDR.Dynamics.Configurations.QueryConfiguration;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Extends the basic User session manager to have the user actually used in the SEIDR.WindowMonitor project
    /// <para>Then exposes the current User to every page that inherits from SessionWindow. </para>
    /// <para>
    /// Note that it will not be available to windows inheriting from just the BasicSessionWindow</para>
    /// </summary>
    public class UserSessionManager : BasicUserSessionManager
    {
        internal Query _currentQuery
        {
            //get{return MySession.CurrentQuery;}
            get { return this["_CurrentQuery"] as Query; }
            //set { MySession.CurrentQuery = value; }
            set
            {
                SetCache("_CurrentQuery", value);
            }
        }
        internal QueryParameter[] _lastParameters
        {
            get { return this["_LastParams"] as QueryParameter[]; }
            set { SetCache("_LastParams", value); }
        }
        public MiscSetting MySettings { get; set; }
        //User _RealUser;
        WindowUser _RealUser;        
        UserAccessMode _Access;
        public WindowUser CurrentUser
        {
            get
            {
                switch (CurrentAccessMode)
                {
                    case UserAccessMode.SingleUser:
                        //User u = User.SingleUserMode;
                        WindowUser u = WindowUser.SingleUserMode;
                        //u.MyTeam = _RealUser.MyTeam; //Keep the same team as _RealUser to avoid having extra file path evaluation. 
                        //Especially since it doesn't matter for single user
                        u.team = _RealUser.Team;
                        u.TeamID = _RealUser.TeamID;
                        return u;
                    case UserAccessMode.Impersonation:
                        return Broker.ConfiguredUser;
                    default:
                        return _RealUser;
                }
                //return iConfigManager.Me;                
            }
        }
        public void UpdateUserFromLogin(
            //User update
            WindowUser update, bool singleUser
            )
        {
            _RealUser = update;
            _Access = singleUser ? UserAccessMode.SingleUser : UserAccessMode.Team;
            //Broker login should have already  been called...
            //Broker.LogIn(new LoginInfo { Domain = update.Domain, UserName = update.UserName });
            Broker.LoadConfigurations(false, singleUser /*_Access == UserAccessMode.SINGLEUSER*/);
            //SettingManager.UpdatedUserCheck();
        }
        /// <summary>
        /// Impersonate user. Cannot be called from single user mode.
        /// </summary>
        /// <param name="impersonate">User to impersonate</param>
        public void Impersonate(WindowUser impersonate)
        {
            _Access = UserAccessMode.Impersonation;
            Broker.SetUser(impersonate);
        }
        public bool SingleUserMode
        {
            get { return _Access.HasFlag(UserAccessMode.SingleUser); }
        }
        /// <summary>
        /// End impersonation. Cannot be called from single user mode.
        /// </summary>
        public void EndImpersonation()
        {
            _Access = UserAccessMode.Team;
            Broker.SetUser(_RealUser);
        }
        public override BasicUser CurrentBasicUser
        {
            get
            {
                //return CurrentUser.AsBasicUser();
                return CurrentUser;
            }
        }        
        public void UpdateLogoutTime(int minutes)
        {
            SlideLogoutTime(minutes, false);
        }
        public void SetAlertLevel(Dynamics.ExceptionLevel newExceptionLevel)
        {
            Broker.MyExceptionManager.alertLevel = newExceptionLevel;
        }
        public void SetAccessMode(UserAccessMode mode)
        {
            bool needUpdate = _Access != mode; //When changing, this also forces the current user to change...
            _Access = mode;
            if (needUpdate)
            {
                Broker.LoadConfigurations(false, _Access == UserAccessMode.SingleUser);
                //SettingManager.UpdatedUserCheck();
            }
            
        }
        public override UserAccessMode CurrentAccessMode
        {
            get
            {
                return _Access;
            }            
        }
        
        public UserSessionManager(WindowUser u, UserAccessMode initialAccessMode, int LogOutMinutes) 
            : base(LogOutMinutes)
        {
            _RealUser = u;
            _Access = initialAccessMode;
        }
        //public Query CurrentQuery { get; set; } = null;
        //public MainQueryParamSetup lastQuerySetup { get; set; } = null;
        //public SEIDR_MenuAddOnConfiguration CurrentAddon { get; set; } = null;
        /// <summary>
        /// Allow internal Windows to set cache objects regardless of permission by hiding the base method which checks permission
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="value"></param>
        /// <param name="duration">Duration of object to stay in cache</param>
        new public void SetCache(string Key, object value, int duration = CACHE_DURATION_MINUTES)
        {
            cache[Key] = new CacheObject(value, duration); //Allow SEIDR.window pages to set cache without requiring user permission to use the cache
        }

        public new object this[string cacheKey]
        {
            get
            {
                if (cache.ContainsKey(cacheKey))
                    return cache[cacheKey].Content;
                if(cacheKey[0] != '_')
                    Broker
                        .MyExceptionManager
                        .Handle("Cache Miss: " + cacheKey, SEIDR.Dynamics.ExceptionLevel.Background);
                return null;
                /*
                if (cacheKey.StartsWith("_") && cache.ContainsKey(cacheKey))
                    return cache[cacheKey].Content;
                else
                    return base[cacheKey];
                */
            }
            set
            {
                if (cacheKey != null)
                {
                    if (value == null)
                        cache.Remove(cacheKey);//Note that if we're setting to null and the key doesn't exist in cache, it will return null anyway
                                               //So to reduce amount of storage, remove from cache when setting to null
                    else if (cache.ContainsKey(cacheKey))
                    {
                        cache[cacheKey].Content = value;
                    }
                    else
                    {
                        cache[cacheKey] = new CacheObject(value); //Note that cache duration won't matter for "_"
                    }
                }
                else
                {
                    Broker
                        .MyExceptionManager
                        .Handle("Null cache Key", SEIDR.Dynamics.ExceptionLevel.Background);
                }
            }
        }
    }
}
