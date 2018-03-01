using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SEIDR.Dynamics.Configurations.UserConfiguration;

namespace SEIDR.Dynamics.Configurations
{
    public abstract class BasicUserSessionManager
    {        
        public ConfigurationListBroker Broker { get; private set; }
        public bool ImpersonationAllowed { get; private set; }
        public virtual void Setup(ConfigurationListBroker broker, 
            ConfigurationListBrokerMetaData brokerMetaData)
        {
            if (brokerMetaData == null)
                ImpersonationAllowed = false;
            else
                ImpersonationAllowed = brokerMetaData.SupportImpersonation;
            Broker = broker;
        }
        public abstract UserAccessMode CurrentAccessMode { get; }
        public const int DEFAULT_LOGOUT_TIME_MINUTES = 60;
        public const int CACHE_DURATION_MINUTES = 35;        
        /// <summary>
        /// Get Current basic User. Based on actual implementation class
        /// </summary>
        public abstract BasicUser CurrentBasicUser { get; } 
        //Note, don't need to do this for the non basic version because it's not available outside of the SEDIR.Window project
        public BasicUserSessionManager(int minutes = DEFAULT_LOGOUT_TIME_MINUTES)
        {            
            ActiveWindowList = new List<BasicSessionWindow>();
            LogIn(minutes);
        }
        
        List<BasicSessionWindow> ActiveWindowList;
        public bool CheckLogout()
        {
            if (LoginTime < LogOutTime  //If LoginTime is equal to or later than the logout time, ignore it.
                && DateTime.Now > LogOutTime)
            {
                LogOut();
                return true;
            }
            return false;
        }
        public void LogOut()
        {            
            CheckCache();
            LoginTime = null;
            lock (ActiveLock)
            {
                //Go backwards through registered windows and close
                for (int i = ActiveWindowList.Count - 1; i >= 0; i -= 1)
                {
                    BasicSessionWindow w = ActiveWindowList[i];                    
                    w.Dispatcher.Invoke(w.SessionStop); //Windows are registered when calling show dialog 
                                                        //should not have to worry about exception from calling this
                                                        //w.Close();
                    if(w.Registered)
                        UnregisterWindow(w); //Don't unregister a window that isn't registerd... although it really shouldn't be in the active list...
                }
            }
        }
        public void LogIn(int minutes = DEFAULT_LOGOUT_TIME_MINUTES)
        {
            ActiveWindowList.Clear();            
            ActiveWindows = 0;
            LoginTime = DateTime.Now;
            SlideLogoutTime(minutes);            
        }
        /// <summary>
        /// Sets the LogoutTime to 'minutes' minutes after the login time.
        /// </summary>
        /// <param name="minutes"></param>
        protected void SlideLogoutTime(int minutes =  DEFAULT_LOGOUT_TIME_MINUTES, bool SetTotalMinutes = true)
        {
            if (LoggedIn)
            {
                if(minutes < 0)
                {
                    LogOutTime = DateTime.MaxValue;
                    return;
                }
                if (SetTotalMinutes)
                    LogOutTime = LoginTime.Value.AddMinutes(minutes);
                else
                    LogOutTime = DateTime.Now.AddMinutes(minutes);
            }
        }
        #region caching
        public void CheckCache()
        {
            var keys = (from kv in cache
                        where kv.Value.CheckDeath()
                        && !kv.Key.StartsWith("_")
                        select kv.Key).ToArray();
            foreach(var key in keys)
            {
                if(cache.ContainsKey(key))
                    cache.Remove(key);
            }
        }
        protected class CacheObject
        {
            object _content;
            public object Content
            {
                get
                {
                    //SlideCacheDeath(); //Only setting should slide cache duration
                    return _content;
                }
                set
                {
                    _content = value;
                    SlideCacheDeath();
                }      
            }            
            public DateTime StartTime { get; private set; }
            public CacheObject(object myContent, int duration = CACHE_DURATION_MINUTES)
            {
                _content = myContent;
                SlideCacheDeath(duration);
                StartTime = DateTime.Now;
            }
            DateTime cacheDeath;
            public bool CheckDeath()
            {
                return DateTime.Now >= cacheDeath;
            }
            public void SlideCacheDeath(int minutes = CACHE_DURATION_MINUTES)
            {
                cacheDeath = DateTime.Now.AddMinutes(minutes);
            }

        }
        /// <summary>
        /// Sets the object in the cache with a specified duration, if the user has permission to use the cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="duration">Duration of cache in minutes. Cache is checked when a window is registered or re-activated</param>
        public void SetCache(string key, object value, int duration = CACHE_DURATION_MINUTES)
        {
            if (!CurrentBasicUser.CanUseCache)
                return;
            this[key] = new CacheObject(value, duration); 
        }
        /// <summary>
        /// Checks when the value associated with a cache key was set, or null if the key does not have an associated value. 
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public DateTime? CheckCacheStartTime(string Key)
        {
            if (Key == null || !cache.ContainsKey(Key))
                return null;
            return cache[Key].StartTime;
        }
        /// <summary>
        /// Gets or sets an obejct in the cache with the default duration.
        /// <para>Cache objects are checked for lifetime when a window is registered or re-activated - should not rely on the cache having a value.</para>
        /// <para>Note that cache values will ONLY be set if the user has permission. Otherwise it will be ignored</para>
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public object this[string cacheKey]
        {
            get
            {
                if (cacheKey == null 
                    || cacheKey.StartsWith("_")) //Limit "_" to the project that implements the actual session manager
                    return null;
                if (!cache.ContainsKey(cacheKey) || cache[cacheKey] == null)
                    return null;
                return cache[cacheKey].Content; //Property will take care of sliding at an individual level
                //SlideCacheDeath(CACHE_DURATION_HOURS);
                //return cache[cacheKey];
            }
            set
            {                
                if (CurrentBasicUser.CanUseCache 
                    && cacheKey != null 
                    && !cacheKey.StartsWith("_")) //Limit "_" to implementation project
                {
                    CacheObject temp = value as CacheObject; //Separate handling if the passed value is 
                                                             //already a CacheObject (So setting cache with a specific duration)

                    if (value == null)
                        cache.Remove(cacheKey);//Note that if we're setting to null and the key doesn't exist in cache, it will return null anyway
                                               //So to reduce amount of storage, remove from cache when setting to null                               
                    else if ( temp != null)
                    {
                        if (cache.ContainsKey(cacheKey) && temp.Content == null)
                            cache.Remove(cacheKey);
                        else if(temp.Content != null)
                            cache[cacheKey] = temp;
                        return;
                    }
                    else if (cache.ContainsKey(cacheKey))
                    {
                        cache[cacheKey].Content = value;
                    }             
                    else
                    {
                        cache[cacheKey] = new CacheObject(value);                        
                    }
                }
            }
        }
        
        protected Dictionary<string, CacheObject> cache = new Dictionary<string, CacheObject>();
        #endregion

        #region Activity tracking
        public int ActiveWindows { get; private set; }
        public void RegisterWindow(BasicSessionWindow w, bool SafeWindow = false)
        {
            if (LoginTime == null)
                throw new InvalidBasicUserSesssionException("User is not logged in", CurrentBasicUser, w.Name, LoggedIn:false);
            if (ActiveWindowList.Count > 14 && !SafeWindow)
                throw new InvalidBasicUserSesssionException("Too many windows open. Close another window first", CurrentBasicUser, w.Name);
            //Should be called when a window is activated..
            lock (ActiveLock)
            {                
                CheckCache();
                ActiveWindowList.Add(w);
                ActiveWindows++;

                w.Registered = true;
                w.ActiveRegistered = true;
                if (IsUserActive && ActiveUserEvent != null)
                    ActiveUserEvent(this, new UserActivityEventArgs(CurrentBasicUser, true, UserActivityEventArgs.ActivityState.Registered));
            }
        }
        public void UnregisterWindow(BasicSessionWindow w)
        {            
            w.Registered = false;
            w.ActiveRegistered = false;
            lock (ActiveLock)
            {
                for (int i = ActiveWindows - 1; i >= 0; i--)
                {
                    if (ActiveWindowList[i] == w)
                    {
                        ActiveWindowList.RemoveAt(i);
                        ActiveWindows--;
                        break;
                    }
                }                
                if (ActiveWindowList.Count <= 0 && InactiveUserEvent != null)
                    InactiveUserEvent(this, new UserActivityEventArgs(CurrentBasicUser, false, UserActivityEventArgs.ActivityState.Unregistered));
            }                    
        }
        bool Active;
        /// <summary>
        /// TODO: Switch to using Application deactivation instead of Window... but that's pretty low priority. 
        /// <para>Only reason it would matter is for plugins that open forms or don't inherit from SessionWindow. 
        /// </para>
        /// <para>But A) They would need separate logic for registration so that the session manager can tell them to close</para>
        /// <para>and B) using the window events is a way of forcing them to either use session window or not have a GUI that takes focus from the main window, since the session can be ended early</para>
        /// </summary>
        /// <param name="w"></param>
        public void DeativateWindow(BasicSessionWindow w)
        {
            lock (ActiveLock)
            {
                ActiveWindows--;
                w.ActiveRegistered = false;
                //Should actually probably always fire, like the Reactivate event... because a user can only use one window at a time...
                if (ActiveWindows <= 0 && InactiveUserEvent != null)  //Called when window is deactivated, not when closing. So will only be called when switching
                    InactiveUserEvent(this, new UserActivityEventArgs(CurrentBasicUser, true, UserActivityEventArgs.ActivityState.Deactivated));
            }
        }
        object ActiveLock = new object();
        public void ReactivateWindow(BasicSessionWindow w)
        {
            lock (ActiveLock)
            {
                ActiveWindows++;
                CheckCache();
                w.ActiveRegistered = true;
                ActiveUserEvent?.Invoke(this, new UserActivityEventArgs(CurrentBasicUser, true, UserActivityEventArgs.ActivityState.Activated));
            }
        }
        public bool IsUserActive
        {
            get
            {
                //return Active;
                return ActiveWindows > 0;
            }
        }        
        /// <summary>
        /// Raised when a user activates a window.
        /// </summary>
        public event EventHandler<UserActivityEventArgs> ActiveUserEvent;
        /// <summary>
        /// Event raised when all windows are deactivated by user.
        /// <para>
        /// Potential to be raised when switching windows due to the fact that only one window can be active at a time, depending on the order for window activation vs deactivation events in WPF
        /// </para>
        /// <para>Note that in the case of switching windows, ActiveUserEvent will fire AFTER this one.</para>
        /// </summary>
        public event EventHandler<UserActivityEventArgs> InactiveUserEvent;
        #endregion
        public DateTime? LoginTime { get; private set; }
        public DateTime LogOutTime { get; private set; }
        public bool LoggedIn { get { return LoginTime != null; } }
    }
    public class InvalidBasicUserSesssionException : Exception
    {
        public BasicUser SessionUser { get; private set; }
        public bool IsLoggedIn { get; private set; }
        public string sourceWindow { get; private set; }
        public InvalidBasicUserSesssionException(string Message, BasicUser user, string source, bool LoggedIn = true):base(Message)
        {
            SessionUser = user;
            IsLoggedIn = LoggedIn;
            sourceWindow = source;
        }
    }
    public class UserActivityEventArgs : EventArgs
    {
        public readonly BasicUser User;
        public readonly DateTime EventTime;
        /// <summary>
        /// True if the window is still registered. Otherwise, the event was raised while unregistering a window
        /// </summary>
        public readonly bool WindowIsRegistered;
        
        //Note, don't need to have an isActive bool because the events raised indicate whether the user is active or not        
        public UserActivityEventArgs(BasicUser user, bool reg, ActivityState userState)
        {
            User = user;
            EventTime = DateTime.Now;
            WindowIsRegistered = reg;
            UserState = userState;            
        }
        /// <summary>
        /// The most recent activity state.
        /// </summary>
        public readonly ActivityState UserState;
        public enum ActivityState
        {
            /// <summary>
            /// Window has just been registered
            /// </summary>
            Registered,
            /// <summary>
            /// Window was reactivated
            /// </summary>
            Activated,
            /// <summary>
            /// Window was deactivated
            /// </summary>
            Deactivated,
            /// <summary>
            /// Window has been closed and unregistered
            /// </summary>
            Unregistered
        }
    }

}
