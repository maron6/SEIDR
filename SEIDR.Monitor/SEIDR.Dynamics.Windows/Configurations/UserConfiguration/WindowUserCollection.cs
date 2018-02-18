using SEIDR.Dynamics.Configurations.Encryption;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Dynamics.Configurations.UserConfiguration
{
    public class WindowUserCollection : WindowConfigurationList<WindowUser>
    {
        public WindowUserCollection() 
            : base(WindowConfigurationScope.U)
        {
        }
        /// <summary>
        /// Basic save - saves to a file specified by the load model.
        /// <para>Should only be called when the user has permission to edit users</para>
        /// </summary>
        public override void Save()
        {
            var other = LoadModel.Tag.ToString().DeserializeXML<WindowUserCollection>();
            if (other != null && other.Version != Version)
                throw new Exception("The record has been changed by another user.");
            string content = this.SerializeToXML();
            if (!LoadModel.UserSpecific)
                content = content.Encrypt(LoadModel.Key);
            System.IO.File.WriteAllText(LoadModel.Tag.ToString(), content);
            ConfigurationEntries.Where(c => c.Altered).ForEach(c => c.Altered = false);
        }

        public override WindowConfigurationList<WindowUser> cloneSetup()
        {
            return this.XClone();
        }
        /// <summary>
        /// Sets the team property on users to be the team's Key
        /// </summary>
        /// <param name="teams"></param>
        public void FillTeamKeys(TeamList teams)
        {
            ConfigurationEntries
                .Where(u=> u.TeamID != null)
                .ForEach(u => u.team = teams[u.TeamID].Key);
        }
        public IEnumerable<WindowUser> GetTeamMembers(int TeamID)
        {
            return ConfigurationEntries.Where(u => u.TeamID == TeamID);
        }
    }
}
