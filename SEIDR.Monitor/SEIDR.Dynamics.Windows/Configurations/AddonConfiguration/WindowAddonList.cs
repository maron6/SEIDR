using SEIDR.Dynamics.Configurations.Encryption;
using SEIDR.Dynamics.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SEIDR.Dynamics.Configurations.AddonConfiguration
{
    public class WindowAddonList : WindowConfigurationList<WindowAddonConfiguration>
    {
        public WindowAddonList(): base(WindowConfigurationScope.A) { }
        
        public override bool Add(WindowAddonConfiguration newRecord)
        {
            if (this[newRecord.ID] != null)
            {
                return false;               
            }
            else if (newRecord.IsConfigurationSingleton)
            {
                var count = (from a in ConfigurationEntries
                             where a.AddonName == newRecord.AddonName
                             select a).Count();
                if (count > 0)
                {
                    //new Alert($"Singleton addon {setting.AddonName} is already set up.", Choice: false).ShowDialog();
                    return false;
                }
            }
            ConfigurationEntries.Add(newRecord);
            return true;
        }

        public override WindowConfigurationList<WindowAddonConfiguration> cloneSetup()
        {
            return this.XClone();
        }

        /*
public MenuItem GetMenu(MenuItem root, Windows.SEIDR_Window caller)
{
   root.Items.Clear();
   var usedAddons = GetUsedAddons(); //Permissions handled by Library.
   foreach (string addon in usedAddons)
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
       if (subRoot != null)
           root.Items.Add(subRoot);
   }

   return root;
} */
        /// <summary>
        /// Basic save - saves to a file specified by the load model.
        /// </summary>
        public override void Save()
        {
            var other = LoadModel.Tag.ToString().DeserializeXML<WindowAddonList>();
            if (other != null && other.Version != Version)
                throw new Exception("The record has been changed by another user.");
            string content = this.SerializeToXML();
            if (!LoadModel.UserSpecific)
                content = content.Encrypt(LoadModel.Key);
            System.IO.File.WriteAllText(LoadModel.Tag.ToString(), content);
            ConfigurationEntries.Where(c => c.Altered).ForEach(c => c.Altered = false);
        }
    }
}
