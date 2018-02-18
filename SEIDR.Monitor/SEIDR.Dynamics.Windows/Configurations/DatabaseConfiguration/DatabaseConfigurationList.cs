using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Dynamics.Configurations.DatabaseConfiguration
{
    public class DatabaseList : WindowConfigurationList<Database>
    {
        public DatabaseList() : base(WindowConfigurationScope.DB) { }
        public override DataTable MyData
        {
            get
            {
                return ConfigurationEntries
                    .ToDataTableLimited(
                        nameof(Database.ID),
                        nameof(Database.Key),
                        nameof(Database.Description),
                        nameof(Database.ConnectionColor),
                        nameof(Database.TextColor));                
            }
        }

        public override WindowConfigurationList<Database> cloneSetup()
        {
            return this.XClone();
        }
        /// <summary>
        /// Basic save - saves to a file specified by the load model.
        /// </summary>
        public override void Save()
        {
            var other = LoadModel.Tag.ToString().DeserializeXML<DatabaseList>();
            if (other != null && other.Version != Version)
                throw new Exception("The record has been changed by another user.");
            string content = this.SerializeToXML();
            if (!LoadModel.UserSpecific)
                content = Encryption.AESWrapper.Encrypt(content, LoadModel.Key);

            System.IO.File.WriteAllText(LoadModel.Tag.ToString(), content);
            ConfigurationEntries.Where(c => c.Altered).ForEach(c => c.Altered = false);
        }
    }
}
