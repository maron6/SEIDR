using SEIDR.Dynamics.Configurations.Encryption;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Dynamics.Configurations.AddonConfiguration
{
    public class ContextAddonList : WindowConfigurationList<ContextAddonConfiguration>
    {
        public ContextAddonList() : base(WindowConfigurationScope.ACM) { }
        public override WindowConfigurationList<ContextAddonConfiguration> cloneSetup()
        {
            return this.XClone();
        }
        /// <summary>
        /// Basic save - saves to a file specified by the load model.
        /// </summary>
        public override void Save()
        {
            var other = LoadModel.Tag.ToString().DeserializeXML<ContextAddonList>();
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
