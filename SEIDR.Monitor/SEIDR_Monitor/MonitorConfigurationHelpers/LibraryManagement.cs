using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Configurations;
using SEIDR.WindowMonitor.ConfigurationBroker;
using System.IO;
using SEIDR.Dynamics.Windows;
using SEIDR.Dynamics.Configurations.UserConfiguration;
using SEIDR.Dynamics.Configurations.AddonConfiguration;
using System.Windows.Controls;
using SEIDR.Dynamics;

namespace SEIDR.WindowMonitor.MonitorConfigurationHelpers
{
    static class LibraryManagement
    {
        public const string CONTEXT_ADDON_FOLDER = "ContextPlugins";
        public const string WINDOW_ADDON_FOLDER = "WindowPlugins";
        public const string BROKER_FOLDER = "ConfigurationBrokers";
        public static string LibraryFolder { get; private set; }
        public static string WindowLibraryFolder { get; private set; }
        public static AddOnLibrary myLibrary { get; private set; }
        public static SEIDR_MenuItemLibrary myMenuLibrary { get; private set; }
        static LibraryManagement()
        {
            if (LibraryFolder == null)
                LibraryFolder = ConfigFolder.GetFolder(MiscSetting.APP_NAME, CONTEXT_ADDON_FOLDER);
            else if (!Directory.Exists(LibraryFolder))
                Directory.CreateDirectory(LibraryFolder);
            if (WindowLibraryFolder == null)
                WindowLibraryFolder = ConfigFolder.GetFolder(MiscSetting.APP_NAME, WINDOW_ADDON_FOLDER);
            else if (!Directory.Exists(WindowLibraryFolder))
                Directory.CreateDirectory(WindowLibraryFolder);
        }
        public static UserSessionManager __SESSION__ { get { return BasicSessionWindow.SessionManager as UserSessionManager; } }
        static DateTime lastDefaultUpdate = DateTime.MinValue;
        //static double lastDefaultDateCheck = DateTime.MinValue.ToOADate();
        public static void ForceAddonRefresh()
        {
            if (LibraryFolder != null)
            {
                if (myLibrary == null)
                {
                    try
                    {
                        myLibrary = new AddOnLibrary(LibraryFolder);
                    }
                    catch (Exception e)
                    {
                        __SESSION__.Broker.MyExceptionManager.Handle(e, "Unable to set up Context Addon Library",
                            Dynamics.ExceptionLevel.Background);
                    }
                }
                else myLibrary.RefreshLibrary();
            }
            if (WindowLibraryFolder != null)
            {
                if (myMenuLibrary == null)
                {
                    try
                    {
                        myMenuLibrary = new SEIDR_MenuItemLibrary(WindowLibraryFolder);
                    }
                    catch (Exception e)
                    {
                        __SESSION__.Broker.MyExceptionManager.Handle(e, "Unable to set up Window Plugin Library",
                            Dynamics.ExceptionLevel.Background);
                    }
                }
                else myMenuLibrary.RefreshLibrary();
            }
        }
        public static BrokerLibrary GetBrokerLibrary()
        {
            string lib = ConfigFolder.GetFolder(MiscSetting.APP_NAME, BROKER_FOLDER);
            if (!Directory.Exists(lib))
                Directory.CreateDirectory(lib);
            return new BrokerLibrary(lib);
        }
        public static ConfigurationListBroker SessionBroker
        {
            get { return __SESSION__.Broker; }
        }
        public static void Handle(string Message, ExceptionLevel handlingLevel = ExceptionLevel.UI)
            => SessionBroker.MyExceptionManager.Handle(Message, handlingLevel);
        public static void Handle(Exception ex, string Message, ExceptionLevel handling = ExceptionLevel.UI)
            => SessionBroker.MyExceptionManager.Handle(ex, Message, handling);
        public static void Handle(Exception ex, ExceptionLevel handling = ExceptionLevel.UI)
            => Handle(ex, null, handling);
        public static List<string> GetWindowAddonNames()
        {

            if (myMenuLibrary == null)
                return new List<string>();
            BasicUser b = __SESSION__.CurrentUser.Clone();
            return myMenuLibrary.GetAddonNames(b)?.ToList()?? new List<string>();
        }
        
        public static SEIDR_WindowMenuAddOn GetWindowAddon(string AddonName)
        {

            if (myMenuLibrary == null)
                return null;
            BasicUser b = __SESSION__.CurrentUser;
            return myMenuLibrary.GetAddOn(b.Clone(), AddonName);
        }
        public static SEIDR_WindowMenuAddOn GetWindowAddon(string AddonName, out SEIDR_WindowMenuAddOn_MetaData metaData)
        {
            metaData = null;
            if (myMenuLibrary == null)
                return null;
            BasicUser b = __SESSION__.CurrentUser.Clone();
            metaData = myMenuLibrary.GetMetaData(b, AddonName);
            return myMenuLibrary.GetAddOn(b, AddonName);
        }
        public static List<string> GetContextAddonNames()
        {
            if (myMenuLibrary == null)
                return null;
            return new List<string>(myLibrary.GetAddonList( __SESSION__.CurrentBasicUser.Clone()));
        }
        public static SEIDR_WindowAddOn GetContextAddon(string AddonName, string id)
        {
            if (myMenuLibrary == null)
                return null;
            return myLibrary.GetApp(AddonName, id, __SESSION__.CurrentBasicUser.Clone());
        }
        public static SEIDR_WindowAddOn GetContextAddon(ContextAddonConfiguration cm)
        {
            if (myMenuLibrary == null)
                return null;
            return myLibrary.GetApp(cm.AppName, cm.Guid, __SESSION__.CurrentBasicUser.Clone());
        }
        public static SEIDR_WindowAddOn GetContextAddon(ContextAddonConfiguration cm, out SEIDR_WindowAddon_MetaData metaData)
        {
            metaData = null;
            if (myMenuLibrary == null)
                return null;
            BasicUser b = __SESSION__.CurrentBasicUser.Clone();
            metaData = myLibrary.GetAppInfo(cm.AppName, cm.Guid, b);
            return myLibrary.GetApp(cm.AppName, cm.Guid, b);
        }
        public static MenuItem ToMenuItem(string Addon,
            SEIDR_WindowMenuAddOn plugin,
            SEIDR_WindowMenuAddOn_MetaData pluginMeta,
            WindowAddonConfiguration[] items)
        {
            if (pluginMeta.Singleton)
            {
                var item = items[0];
                MenuItem m = plugin.Setup(__SESSION__.CurrentUser,
                    __SESSION__.Broker.Connections[item.DatabaseID].Connection,
                    item.ID ?? -1,
                    item.Parameters);
                if (m == null)
                    return null;
                m.Header = item.Key;
                return m;
            }
            MenuItem root = new MenuItem
            {
                Header = Addon
            };
            foreach (var item in items)
            {
                //Permissions handled by Library. should not need to worry about them once the setup has been done. Note that it might blow up                                
                MenuItem rootBase = plugin.Setup(__SESSION__.CurrentUser,
                    __SESSION__.Broker.Connections[item.DatabaseID].Connection,
                    item.ID?? -1,
                    item.Parameters /*item.ParameterInfo*/);
                if (rootBase == null)
                    continue; //Shouldn't happen, but you never know...
                rootBase.Header = item.Key;
                root.Items.Add(rootBase);
            }
            if (root.Items.Count == 0)
                return null; //If for whatever reason, the plugin doesn't add menu items.... don't add it to the plugin menu.
            return root;
        }

        public static MenuItem GetMenu(this WindowAddonList config, 
            MenuItem root, SEIDR_Window caller)
        {
            root.Items.Clear();
            var usedAddons = GetWindowAddonNames(); //Permissions handled by Library.            
            foreach (string addon in usedAddons)
            {
                SEIDR_WindowMenuAddOn_MetaData pluginMetaData;
                var plugin = GetWindowAddon(addon, out pluginMetaData);
                if (plugin == null)
                    continue; //Unable to access - could be permission or not having the dll or issues with loading.
                plugin.callerWindow = caller; //add to plugin before setting up windows
                var setups = (from s in config.ConfigurationEntries
                              where s.AddonName == addon
                              select s).ToArray();
                //For each of the addons, get the setups for that particular addon and set up the MenuItem, which gets added to the root.
                MenuItem subRoot = ToMenuItem(addon, plugin, pluginMetaData, setups);
                if (subRoot != null)
                    root.Items.Add(subRoot);
            }

            return root;
        }
    }
}
