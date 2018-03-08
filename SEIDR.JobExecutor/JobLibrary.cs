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
using SEIDR.JobBase;

namespace SEIDR.JobExecutor
{
    class JobLibrary
    {
        public bool IsValidState { get { return maps != null; } }
        CompositionContainer _container = null;
        public readonly string LibraryLocation = @"C:\SEIDR.JobExecutor\Plugin_Library\";
        [ImportMany(typeof(IJob), AllowRecomposition = true)]
        IEnumerable<Lazy<IJob, IJobMetaData>> maps = null;
        #region gets
        public IJob GetOperation(string OperationName, string Schema, out IJobMetaData MetaData)
        {
            MetaData = null;
            if (maps == null || maps.Count() == 0)
                RefreshLibrary();
            if (maps == null)
                return null;
            foreach (var kv in maps)
            {
                IJobMetaData md = kv.Metadata;
                if (md.JobName == OperationName && (md.NameSpace ?? "SEIDR") == (Schema ?? "SEIDR"))                    
                {
                    MetaData = md;
                    return kv.Value;
                }
            }
            return null;
        }
        public IJobMetaData GetJobMetaData(string jobName, string NameSpace)
        {            
            if (maps == null || maps.Count() == 0)
                RefreshLibrary();
            if (maps == null)
                return null;
            foreach (var kv in maps)
            {
                IJobMetaData md = kv.Metadata;
                if (md.JobName == jobName && (md.NameSpace ?? "SEIDR") == (NameSpace ?? "SEIDR"))
                {                    
                    return md;
                }
            }
            return null;
        }
        #endregion
        ~JobLibrary()
        {
            CheckDispose();
        }
        private void CheckDispose()
        {
            if (_container != null)
            {
                if(maps != null)
                _container.ReleaseExports(maps);
                _container.Dispose();
            }
        }
        public JobLibrary(string location)
        {
            LibraryLocation = location;
            Compose();
        }
        public void ValidateOperationTable(DatabaseManager mgr)
        {
            var model = new DatabaseManagerHelperModel("usp_Job_Validate")
            {
                RetryOnDeadlock = true,                
                Schema = "SEIDR"
            };            
            DataTable dt = new DataTable("udt_JobMetaData");
            dt.AddColumns<IJobMetaData>(
                nameof(IJobMetaData.SafeCancel), 
                nameof(IJobMetaData.RerunThreadCheck)); //Fields we don't need in the database.            
            maps.ForEach(m => dt.AddRow(m.Metadata));
            //model.SetKey("OperationList", dt);
            model.SetKey("JobList", dt);
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

                maps = _container.GetExports<IJob, IJobMetaData>();
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
