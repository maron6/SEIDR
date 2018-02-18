using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Configurations;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using SEIDR.Dynamics.Configurations.UserConfiguration;

namespace SEIDR.Dynamics.Windows
{
    public class SEIDR_MenuItemLibrary:IDisposable
    {
        CompositionContainer _container = null;
        readonly string _LibraryLocation;
        public bool IsValidState { get { return apps != null; } }
        [ImportMany(typeof(SEIDR_WindowMenuAddOn), AllowRecomposition = true)]
        IEnumerable<Lazy<SEIDR_WindowMenuAddOn, SEIDR_WindowMenuAddOn_MetaData>> apps;

        #region gets
        public SEIDR_WindowMenuAddOn_MetaData[] GetMetaData(BasicUser caller)
        {
            if(apps == null || apps.Count() == 0)
                RefreshLibrary();
            if (apps == null)
                return null;
            return (from plugin in apps
                    let md = plugin.Metadata
                    where caller.BasicValidate(md.RequireSessionCache, md.RequirePermission, md.Name, md.Team)
                    select md).ToArray();
        }
        public string[] GetAddonNames(BasicUser caller, string Name = null)
        {
            if (apps == null || apps.Count() == 0)
                RefreshLibrary();
            if (apps == null)
                return null;
            return (from md in GetMetaData(caller)
                    where Name == null || md.Name == Name
                    select md.Name).ToArray();
        }
        public SEIDR_WindowMenuAddOn_MetaData GetMetaData(BasicUser caller, string Name)
        {
            if (apps == null || apps.Count() == 0)
                RefreshLibrary();
            if (apps == null)
                return null;
            var q = (from plugin in apps
                    let md = plugin.Metadata
                    where md.Name == Name && caller.BasicValidate(md.RequireSessionCache, md.RequirePermission, md.Name, md.Team)
                     select md).ToArray();
            if (q.Count() == 0)
                return null;
            return q[0];
        }
        public SEIDR_WindowMenuAddOn GetAddOn(BasicUser caller, string Name)
        {
            if (apps == null || apps.Count() == 0)
                RefreshLibrary();
            if (apps == null)
                return null;
            var q = (from plugin in apps
                     let md = plugin.Metadata
                     where md.Name == Name && caller.BasicValidate(md.RequireSessionCache, md.RequirePermission, md.Name, md.Team)
                     select plugin).ToArray();
            if (q.Length == 0)
                return null; //No permission, or else DLL is not in the addon folder.
            return q[0].Value;
        }
        public SEIDR_WindowMenuAddOn[] GetAddons(BasicUser caller)
        {
            if (apps == null || apps.Count() == 0)
                RefreshLibrary();
            if (apps == null)
                return null;
            var q = (from plugin in apps
                     let md = plugin.Metadata
                     where caller.BasicValidate(md.RequireSessionCache, md.RequirePermission, md.Name, md.Team)
                     select plugin.Value).ToArray();
            return q;
        }
        #endregion
        private void checkApps()
        {
            if (apps.Count() == 0)
                Compose();
        }
        public void Dispose()
        {
            Release();
        }
        ~SEIDR_MenuItemLibrary()
        {
            Release();
        }
        public SEIDR_MenuItemLibrary(string LibraryLocation)
        {
            _LibraryLocation = LibraryLocation;
            Compose();
        }
        public SEIDR_MenuItemLibrary(string LibraryLocation, bool doSetup)
        {
            _LibraryLocation = LibraryLocation;
            if (doSetup)
                Compose();
            else
                apps = null;
        }
        private void Release()
        {
            if (_container != null)
            {
                if (apps != null)
                {
                    _container.ReleaseExports(apps);
                    apps = null;
                }
                _container.Dispose();
            }
        }
        private void Compose()
        {
            Release();
            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(System.Reflection.Assembly.GetExecutingAssembly()));
            #if TEST
            var dir = new System.IO.DirectoryInfo(_LibraryLocation);
            var fList = dir.GetFiles("*.dll", System.IO.SearchOption.AllDirectories);            
            foreach(var library in fList)
            {
                AssemblyCatalog temp = new AssemblyCatalog(Assembly.LoadFile(library.FullName));
                var p = temp.
            }
#else
            catalog.Catalogs.Add(new DirectoryCatalog(_LibraryLocation)); //Get child directories and add their directory catalogs as well?
#endif
            try
            {
                _container = new CompositionContainer(catalog);
                _container.ComposeParts(this);
           
                apps = _container.GetExports<SEIDR_WindowMenuAddOn, SEIDR_WindowMenuAddOn_MetaData>();
            }
            catch(Exception e)
            {
                _container.Dispose();
                apps = null;                
                string s = e.Message;                
                throw;
            }
        }
        public void RefreshLibrary()
        {
            Compose();
        }
    }
}
