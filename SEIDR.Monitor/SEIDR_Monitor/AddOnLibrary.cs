using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using SEIDR.Dynamics.Configurations;
//using static SEIDR.WindowMonitor.SettingManager;
using SEIDR.Dynamics.Configurations.UserConfiguration;

namespace SEIDR.WindowMonitor
{

    public class MiniAddonMetaData
    {
        public string Name { get; private set; }
        public string ID { get; private set; }
        public string Description { get; private set; }
        public MiniAddonMetaData(string Name, string id, string Desc)
        {
            this.Name = Name;
            ID = id ?? string.Empty;
            Description = Desc;
        }
        public override string ToString()
        {
            return Name;
        }
        public override bool Equals(object obj)
        {
            var m = obj as MiniAddonMetaData;
            if (m != null) {
                return Name == m.Name && ID == m.ID;
            }
            return false;            
        }
        public override int GetHashCode()
        {
            return (ID + "_____" + Name).GetHashCode();
        }
    }
    public class AddOnLibrary
    {
        public bool IsValidState { get { return maps != null; } }
        CompositionContainer _container = null;
        public readonly string LibraryLocation = null;
        [ImportMany(typeof(SEIDR_WindowAddOn), AllowRecomposition = true)]
        IEnumerable<Lazy<SEIDR_WindowAddOn, SEIDR_WindowAddon_MetaData>> maps = null;
        #region gets
        public SEIDR_WindowAddOn GetApp(string Name, string ID, BasicUser user)
        {
            
            if (maps == null || maps.Count() == 0)
                RefreshLibrary();
            if (maps == null)
                return null;
            return (from kv in maps
                    let m = kv.Metadata
                    where m.AddonName == Name && m.Guid == ID
                    && user.BasicValidate(m.RequireSessionCache, m.RequirePermission, m.AddonName, m.Team)
                    select kv.Value
                    ).FirstOrDefault();
            //foreach (var kv in maps)
            //{
            //    SEIDR_WindowAddon_MetaData dt = kv.Metadata;                   
            //    if (kv.Metadata.AddonName == Name 
            //        && kv.Metadata.Guid == ID
            //        && user.BasicValidate(dt.RequireSessionCache, 
            //            dt.RequirePermission, dt.AddonName, dt.Team)) 
            //    {
            //        return kv.Value;
            //    }
            //}
            //return null;
        }
        public IEnumerable<SEIDR_WindowAddon_MetaData> GetAppInfo(BasicUser configured)
        {
            if (maps == null || maps.Count() == 0)
                RefreshLibrary();
            if (maps == null)
                return null;
            return (from mtdt in maps
                    let dt = mtdt.Metadata
                    where configured.BasicValidate(dt.RequireSessionCache, 
                                    dt.RequirePermission, dt.AddonName, dt.Team)
                    select mtdt.Metadata);
        }
        public SEIDR_WindowAddon_MetaData GetAppInfo(string name, string ID, BasicUser configured)
        {
            if (maps == null || maps.Count() == 0)
                RefreshLibrary();
            if (maps == null)
                return null;
            return (from mtdt in maps
                        let dt = mtdt.Metadata
                        where dt.AddonName == name 
                        && dt.Guid == ID
                        && configured.BasicValidate(dt.RequireSessionCache, dt.RequirePermission, dt.AddonName, dt.Team)
                        select dt).FirstOrDefault();            
        }
        public IEnumerable<string> GetAddonList(BasicUser u)
        {
            if (maps == null || maps.Count() == 0)
                RefreshLibrary();
            if (maps == null)
                return null;
            return (from mtdt in GetAppInfoMini(u)
                    let appName = mtdt.Name
                    select appName);
        }
        public IEnumerable<MiniAddonMetaData> GetAppInfoMini(BasicUser u)
        {
            if (maps == null || maps.Count() == 0)
                RefreshLibrary();
            if (maps == null)
                return null;
            return (from mtdt in maps
                    let m = mtdt.Metadata                    
                    where u.BasicValidate(m.RequireSessionCache, m.RequirePermission, m.AddonName, m.Team)
                    select new MiniAddonMetaData(m.AddonName, m.Guid, m.Description));
        }
        #endregion
        ~AddOnLibrary()
        {
            CheckDispose();
        }
        private void CheckDispose()
        {
            if (_container != null)
            {
                _container.ReleaseExports(maps);
                _container.Dispose();
            }
        }
        public AddOnLibrary(string location)
        {
            LibraryLocation = location;
            Compose();
        }
        /// <summary>
        /// Refreshes the library content.
        /// </summary>
        public void RefreshLibrary()
        {
            _container.ReleaseExports(maps);
            //maps.Clear();
            maps = null;
            Compose();
        }
        void Compose()
        {
            //if (maps.Count > 0)
                //return;
            /*
            if (_container != null)
            {
                if (maps != null)
                    _container.ReleaseExports(maps);
                _container.Dispose();
            }*/
            CheckDispose();
            try
            {
                var catalog = new AggregateCatalog();
                catalog.Catalogs.Add(new AssemblyCatalog(System.Reflection.Assembly.GetExecutingAssembly()));
                catalog.Catalogs.Add(new DirectoryCatalog(LibraryLocation));
                _container = new CompositionContainer(catalog);
                _container.ComposeParts(this);
            
                maps = _container.GetExports<SEIDR_WindowAddOn, SEIDR_WindowAddon_MetaData>();
            }
            catch
            {
                _container.Dispose();
                maps = null;
                throw;
            }
        }    
    }
}
