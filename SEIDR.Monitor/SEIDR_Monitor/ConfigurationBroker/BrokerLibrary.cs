using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Configurations;
using System.ComponentModel.Composition;

namespace SEIDR.WindowMonitor.ConfigurationBroker
{
    public class BrokerLibrary
    {        
        public bool IsValidState { get { return maps != null; } }
        CompositionContainer _container = null;
        public readonly string LibraryLocation = null;
        [ImportMany(typeof(ConfigurationListBroker), AllowRecomposition = true)]
        IEnumerable<Lazy<ConfigurationListBroker, ConfigurationListBrokerMetaData>> maps = null;
        #region gets
        public ConfigurationListBroker GetBroker(ConfigurationListBrokerMetaData check)
        {
            if(BrokerCount == 0 || check == null)
                return null;
            return maps
                .FirstOrDefault(l => 
                    l.Metadata == check
                    //l.Metadata.Key == Key 
                    //&& l.Metadata.Version == version
                    //&& l.Metadata.Description == Description
                    )
                ?.Value;            
        }
        public ConfigurationListBrokerMetaData DefaultBrokerMetaData
        {
            get
            {
                if (BrokerCount == 1)
                    return maps.First().Metadata;
                return null;
            }
        }
        

        public int BrokerCount
        {
            get
            {
                if (maps == null || maps.Count() == 0)
                    RefreshLibrary();
                return maps.Count();
            }
        }
        public IEnumerable<ConfigurationListBrokerMetaData> GetBrokerInfo()
        {
            if(BrokerCount == 0)
                yield break;
            foreach (var mtdt in maps)
                yield return mtdt.Metadata;
            //return (from mtdt in maps                    
            //        select mtdt.Metadata);
        }        
        public IEnumerable<string> GetBrokerNameList()
        {            
            return (from mtdt in GetBrokerInfo()                    
                    select mtdt.Key);
        }        
        #endregion
        ~BrokerLibrary()
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
        public BrokerLibrary(string location)
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
            CheckDispose();
            try
            {                
                _container = new CompositionContainer(new DirectoryCatalog(LibraryLocation));
                _container.ComposeParts(this);

                maps = _container.GetExports<ConfigurationListBroker, ConfigurationListBrokerMetaData>();
            }
            catch
            {
                _container.Dispose();               
                throw;
            }
        }
    }
}
