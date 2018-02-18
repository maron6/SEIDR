using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Controls;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Configurations;
using SEIDR.Dynamics;
using SEIDR.Dynamics.Windows;
using System.Xml.Serialization;
//using static SEIDR.WindowMonitor.SettingManager;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SEIDR.WindowMonitor.MonitorConfigurationHelpers
{
    public class SEIDR_MenuAddOnConfigs: IEnumerable<SEIDR_MenuAddOnConfiguration>, iConfigList
    {
        [XmlIgnore]
        public bool Cloneable
        {
            get
            {
                return false;
            }
        }
        public void Add(SEIDR_MenuAddOnConfiguration setting)
        {
            if(this[setting.Name] != null)
            {
                new Alert($"Internal Name {setting.Name} is already in use.", Choice: false).ShowDialog();
                return;
            }
            else if (setting.IsConfigurationSingleton)
            {
                var count = (from a in items
                             where a.AddonName == setting.AddonName
                             select a).Count();
                if(count > 0)
                {
                    new Alert($"Singleton addon {setting.AddonName} is already set up.", Choice:false).ShowDialog();
                    return;
                }
            }
            items.Add(setting);
        } 
        [XmlIgnore]
        public DataTable MyData
        {
            get
            {
                return items.ToArray().ToDataTable("ParameterInfo", "Parameters", "Item");
                //Note: I have no idea where Item comes from but it was blowing stuff up. I'm pretty sure it's the indexer
            }
        }
        [XmlIgnore]
        public SEIDR_MenuAddOnConfiguration this[string Name]
        {
            get
            {
                foreach (var i in items)
                {
                    if (i.Name == Name)
                        return i;
                }
                return null;
            }
            set
            {
                for(int i= 0; i < items.Count; i++)
                {
                    if (items[i].Name == Name)
                    {
                        items[i] = value;
                        return;
                    }
                }
                Add(value); //Use method that has some validations in it already
                //items.Add(value);
            }
        }
        /// <summary>
        /// Appends children to the root.
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public MenuItem GetMenu(MenuItem root, SEIDR.Dynamics.Windows.SEIDR_Window caller)
        {
            root.Items.Clear();
            var usedAddons = GetUsedAddons(); //Permissions handled by Library.
            /*
            var q = (from a in usedAddons
                     let md = GetAddon(a)
                     where md != null
                     select SEIDR_MenuAddOnConfiguration.ToMenuItem(a, md, items.ToArray()));
            * /
            MenuItem root = new MenuItem
            {
                Header = "_PlugIns",
                Icon = new Image
                {
                    Source = new BitmapImage(new Uri(@"Icons\PlugInMenu.png", UriKind.Relative)),
                    Height = 20,
                    Width = 20
                }
            };*/
            foreach(string addon in usedAddons)
            {
                SEIDR_WindowMenuAddOn_MetaData pluginMetaData;
                var plugin = GetAddon(addon, out pluginMetaData);                                
                if (plugin == null)
                    continue; //Unable to access - could be permission or not having the dll or issues with loading.
                plugin.callerWindow = caller; //add to plugin before setting up windows
                var setups = (from s in items
                             where s.AddonName == addon
                             select s).ToArray();
                //For each of the addons, get the setups for that particular addon and set up the MenuItem, which gets added to the root.
                MenuItem subRoot = SEIDR_MenuAddOnConfiguration.ToMenuItem(addon, plugin, pluginMetaData, setups);                
                if(subRoot != null)
                    root.Items.Add(subRoot);
            }
            
            return root;
        }
        

        public List<string> NamesToAdd(string Edit)
        {
            var check = GetUsedAddons();
            return (from addon in myMenuLibrary.GetMetaData(_ME.AsBasicUser())
                    where !addon.Singleton ||
                    addon.Name == Edit || !check.Contains(Edit)
                    select addon.Name).ToList();
        }
        public Guid Version
        {
            get;set;
        }

        List<SEIDR_MenuAddOnConfiguration> items { get; set; }
        public SEIDR_MenuAddOnConfigs()
        {
            items = new List<SEIDR_MenuAddOnConfiguration>();            
            Version = new Guid();
            
        }
        public IEnumerator<SEIDR_MenuAddOnConfiguration> GetEnumerator()
        {

            var bu = _ME.AsBasicUser();

            //Get list of addon meta data that we have permission to use without actually loading the actual plugin
            var mdl = myMenuLibrary.GetMetaData(bu);             

            var q = (from item in items
                     join md in mdl
                     on item.AddonName equals md.Name
                     select item).ToList();
            return ((IEnumerable<SEIDR_MenuAddOnConfiguration>)q).GetEnumerator();
            
            //return ((IEnumerable<SEIDR_MenuAddOnConfiguration>)items).GetEnumerator();
        }

        public int GetIndex(string idx, bool IncludeNew = true)
        {
            for(int i=  0; i < items.Count; i++)
            {
                if (items[i].Name == idx)
                    return i;
            }
            return -1;
        }

        public List<string> GetNameList()
        {
            return (from item in items
                    select item.Name).ToList();
        }

        public void Remove(string NameKey)
        {
            int x = GetIndex(NameKey);
            if (x > -1)
                items.RemoveAt(x);
        }
        public List<string> GetAddonInfo()
        {
            return myMenuLibrary.GetAddonNames(_ME.AsBasicUser()).ToList();
        }
        public SEIDR_WindowMenuAddOn GetAddon(string AddonName)
        {
            return myMenuLibrary.GetAddOn(_ME.AsBasicUser(), AddonName);
        }
        public SEIDR_WindowMenuAddOn GetAddon(string AddonName, out SEIDR_WindowMenuAddOn_MetaData metaData)
        {
            var b = _ME.AsBasicUser();
            metaData = myMenuLibrary.GetMetaData(b, AddonName);
            return myMenuLibrary.GetAddOn(b, AddonName);
        }
        public List<string> ToStringList(bool AddNew = true)
        {
            return GetNameList();
        }        
        IEnumerator IEnumerable.GetEnumerator()
        {
            var bu = _ME.AsBasicUser();
            var mdl = myMenuLibrary.GetMetaData(bu);
            var q = (from item in items                     
                     join md in mdl 
                     on item.AddonName equals md.Name
                     select item).ToList();
            return ((IEnumerable<SEIDR_MenuAddOnConfiguration>)q).GetEnumerator(); //Change to using filter so that only 
                                                                                   //addons with permission to use show up
            //return ((IEnumerable<SEIDR_MenuAddOnConfiguration>)items).GetEnumerator();
        }
        public string[] GetUsedAddons()
        {
            return (from p in items
                    select p.AddonName).Distinct().ToArray();
        }

        public iConfigList cloneSetup()
        {
            SEIDR_MenuAddOnConfigs clone = new SEIDR_MenuAddOnConfigs();
            clone.items = new List<SEIDR_MenuAddOnConfiguration>(items);
            return clone;
        }
    }
    public class ParameterInfo
    {
        public string Key { get; set; }
        public object Value { get; set; }
    }
    public class SEIDR_MenuAddOnConfiguration :iConfigList_Singleton
    {
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public string AddonName { get; set; }
        public string DBConnection { get; set; }
        public List<ParameterInfo> ParameterInfo { get; set; }
        [XmlIgnore]
        public DateTime? NextCallback { get; set; } = null;
        public SEIDR_MenuAddOnConfiguration()
        {
            //ParameterInfo = new Dictionary<string, object>(); //Ensure not null...
            ParameterInfo = new List<ParameterInfo>();
            Name = ConfigListHelper.GetIDName(ConfigListHelper.Scope.A);
        }
        [XmlIgnore]
        public Dictionary<string, object> Parameters
        {
            get
            {
                Dictionary<string, object> temp = new Dictionary<string, object>();
                foreach(var info in ParameterInfo)
                {
                    //temp.Add(info.Key, info.Value);
                    temp[info.Key] = info.Value;
                }
                return temp;
            }
        }
        [XmlIgnore]
        public object this[string Key]
        {
            get
            {
                var q = (from param in ParameterInfo
                         where param.Key == Key
                         select param.Value).ToArray();
                if (q.Length == 1)
                    return q[0];
                return null;
            }
            set
            {
                ParameterInfo p = new MonitorConfigurationHelpers.ParameterInfo { Key = Key, Value = value };
                var existing = (from param in ParameterInfo
                                where param.Key == Key
                                select param).FirstOrDefault();
                if (existing == null)
                    ParameterInfo.Add(p);
                else
                {
                    ParameterInfo.Remove(existing);
                    ParameterInfo.Add(p);
                }
            }
        }
        [XmlIgnore]
        public bool IsConfigurationSingleton
        {
            get
            {
                if (_ME?.AsBasicUser() == null) //Treat as singleton if _ME hasn't been set up yet...
                    return true;
                var md = myMenuLibrary.GetMetaData(_ME.AsBasicUser(), AddonName); //Just grabbing from a dictionary, should not be expensive
                if (md == null)
                    return true; //If no access... shouldn't really be accessing it, but treat as singleton to reduce damage. I guess.
                return md.Singleton;
            }
        } //Set by the meta data actually....When true, will only setup the first item.

        public static MenuItem ToMenuItem(string Addon, SEIDR_WindowMenuAddOn plugin, SEIDR_WindowMenuAddOn_MetaData pluginMeta, 
            SEIDR_MenuAddOnConfiguration[] items)
        {            
            if (pluginMeta.Singleton)
            {
                var item = items[0];
                MenuItem m = plugin.Setup(_ME.AsBasicUser(), myConnections[item.DBConnection].InternalDBConn, item.Name, item.Parameters);
                if (m == null)
                    return null;
                m.Header = item.DisplayName;
                return m;
            }
            MenuItem root = new MenuItem
            {
                Header = Addon
            };
            foreach(var item in items)
            {                
                //Permissions handled by Library. should not need to worry about them once the setup has been done. Note that it might blow up                                
                MenuItem rootBase = plugin.Setup(_ME.AsBasicUser(), myConnections[item.DBConnection].InternalDBConn, item.Name, item.Parameters /*item.ParameterInfo*/);
                if (rootBase == null) 
                    continue; //Shouldn't happen, but you never know...
                rootBase.Header = item.DisplayName;                
                root.Items.Add(rootBase);
            }
            if (root.Items.Count == 0)
                return null; //If for whatever reason, the plugin doesn't add menu items.... don't add it to the plugin menu.
            return root;
        }
        
    }
}
