using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.OperationServiceModels;
using System.Data;
using SEIDR.DataBase;

namespace SEIDR.JobExecutor
{
    class OperationLibrary
    {
        public bool IsValidState { get { return maps != null; } }
        CompositionContainer _container = null;
        public readonly string LibraryLocation = @"C:\SEIDR.Operator\Plugin_Library\";
        [ImportMany(typeof(iOperation), AllowRecomposition = true)]
        IEnumerable<Lazy<iOperation, iOperationMetaData>> maps = null;
        #region gets
        public iOperation GetOperation(string OperationName, string Schema, int Version, out iOperationMetaData MetaData)
        {
            MetaData = null;
            if (maps == null || maps.Count() == 0)
                RefreshLibrary();
            if (maps == null)
                return null;
            foreach (var kv in maps)
            {
                iOperationMetaData md = kv.Metadata;
                if (md.Operation == OperationName
                    && (md.OperationSchema ?? "SEIDR") == (Schema ?? "SEIDR")
                    && md.Version == Version)                    
                {
                    MetaData = md;
                    return kv.Value;
                }
            }
            return null;
        }
        #endregion
        ~OperationLibrary()
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
        public OperationLibrary(string location)
        {
            LibraryLocation = location;
            Compose();
        }
        public void ValidateOperationTable(DatabaseManager mgr)
        {
            var model = new DatabaseManagerHelperModel("usp_Operation_Validate")
            {
                RetryOnDeadlock = true,                
            };            
            DataTable dt = new DataTable("SEIDR.udt_Operation");
            dt.AddColumns<iOperationMetaData>();
            maps.ForEach(m => dt.AddRow(m.Metadata));
            model.SetKey("OperationList", dt);
            mgr.ExecuteNonQuery(model);
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
            CheckDispose();
            try
            {
                var catalog = new AggregateCatalog();
                catalog.Catalogs.Add(new AssemblyCatalog(System.Reflection.Assembly.GetExecutingAssembly()));
                catalog.Catalogs.Add(new DirectoryCatalog(LibraryLocation));
                _container = new CompositionContainer(catalog);
                _container.ComposeParts(this);

                maps = _container.GetExports<iOperation, iOperationMetaData>();
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
