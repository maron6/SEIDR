using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.DataBase;
using SEIDR.Dynamics.Configurations.Encryption;

namespace SEIDR.Dynamics.Configurations.UserConfiguration
{
    public class TeamList : WindowConfigurationList<Team>
    {
        public TeamList()
            : base(WindowConfigurationScope.TM) { }            
        public override WindowConfigurationList<Team> cloneSetup()
        {
            return this.XClone();
        }
        /// <summary>
        /// Basic save - saves to a file specified by the load model.
        /// </summary>
        public override void Save()
        {
            var other = LoadModel.Tag.ToString().DeserializeXML<TeamList>();
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
