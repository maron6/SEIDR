using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Xml.Serialization;
using SEIDR.Dynamics;
using System.ComponentModel;
using SEIDR.Dynamics.Configurations.QueryConfiguration;

namespace SEIDR.Dynamics.Configurations
{
    [XmlInclude(typeof(QueryList)),
        XmlInclude(typeof(ContextMenuConfiguration.ContextMenuList)),
        XmlInclude(typeof(UserConfiguration.WindowUserCollection)),
        XmlInclude(typeof(UserConfiguration.TeamList)),
        XmlInclude(typeof(AddonConfiguration.ContextAddonList)),
        XmlInclude(typeof(AddonConfiguration.WindowAddonList)),
        XmlInclude(typeof(DatabaseConfiguration.DatabaseList)),]
    public abstract class WindowConfigurationList<WindowConfiguration>
        : iWindowConfigurationList<WindowConfiguration>
        where WindowConfiguration : iWindowConfiguration
    {

        public bool Contains(string Key)
            => ConfigurationEntries.Exists(e => e.Key == Key);
        public bool Contains(int ID)
            => ConfigurationEntries.Exists(e => e.ID == ID);        
        public bool Contains(int? ID)
        {
            if (ID == null)
                return false;
            return Contains(ID.Value);
        }
        public List<WindowConfiguration> ConfigurationEntries { get; set; }
        /// <summary>
        /// Gets the entries that have been altered( set to true by add/update base methods. reset by saving)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<WindowConfiguration> GetAltered()
            => ConfigurationEntries.Where(ce => ce.Altered);
        /// <summary>
        /// Return true if the Configuration Entries list has any records flagged as <see cref="iWindowConfiguration.Altered"/> = true
        /// </summary>
        [XmlIgnore]
        public bool HasAltered
        {
            get
            {
                return ConfigurationEntries.FirstOrDefault(e => e.Altered) != null;
            }
        }
        public IEnumerable<string> GetNameList()
            => ConfigurationEntries.Select(e => e.Key);
        /// <summary>
        /// If limiting configurationEntries - make use of 'RePage' instead of overriding this logic.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<WindowConfiguration> GetEnumerator()
            => ConfigurationEntries;
        /// <summary>
        /// Indexer on ID. Getter returns the first record that matches on ID.<para>
        /// Setter will call <see cref="Add(WindowConfiguration)"/> if not found (null ID will never find a match).</para>
        /// <para>If the value is null, the setter will call <see cref="Remove(int)"/></para>
        /// <para>If the value is not null and the ID was found, will call <see cref="Update(WindowConfiguration, int, int)"/></para>
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        [XmlIgnore]
        public WindowConfiguration this[int? ID]
        {
            get
            {
                return ConfigurationEntries.FirstOrDefault(i => i.ID == ID);
            }
            set
            {
                var wc = ConfigurationEntries.FirstOrDefault(i => i.ID == ID);
                if (ID.HasValue && wc != null)
                {
                    int i = ConfigurationEntries.IndexOf(wc);
                    if (value != null)
                        Update(value, ID.Value, i);
                    else
                        Remove(ID.Value);
                }
                else if(value != null)
                {
                    Add(value);
                }
            }
        }
        [XmlIgnore]
        public WindowConfiguration this[int ID]
        {
            get { return this[(int?)ID]; }
            set { this[(int?)ID] = value; }
        }
        [XmlIgnore]
        public WindowConfiguration this[string Key]
        {
            get
            {
                return ConfigurationEntries.FirstOrDefault(c => c.Key == Key);
            }
        }
        /// <summary>
        /// Calls internal update method to update based on ID. Could potentially cause a null reference depending on usage..
        /// </summary>
        /// <param name="entry"></param>
        public void Update(WindowConfiguration entry)
            => Update(entry, entry.ID.Value);
        /// <summary>
        /// Default method for updating list entries. Just updates the ConfigurationEntries list object
        /// <para>By default, also increments the entry's RecordVersion</para>
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="ID">Probably the most useful if using a database instead of XML serialization</param>
        /// <param name="ListIndex">Index of the entry to update.<para>
        /// If &lt; 0, will find the index by looking for the entry with the same ID</para></param>
        protected virtual void Update(WindowConfiguration entry, int ID, int ListIndex = -1)
        {
            if (ListIndex < 0)
            {
                ListIndex = ConfigurationEntries.FindIndex(e => e.ID == ID);
                if (ListIndex < 0)
                    throw new InvalidOperationException("Unable to update a record that isn't in the Configuration list by default.");
            }
            int v = ConfigurationEntries[ListIndex].RecordVersion;
            entry.RecordVersion = v + 1;
            entry.Altered = true;
            ConfigurationEntries[ListIndex] = entry;
        }
        public WindowConfigurationList(WindowConfigurationScope scope)
        {
            Version = Guid.NewGuid();
            ValidationScope = scope;
            ConfigurationEntries = new List<WindowConfiguration>();
            //SetPage(0);
        }
        public WindowConfigurationList()
        {
            Version = Guid.NewGuid();
            ValidationScope = WindowConfigurationScope.UNK;
            ConfigurationEntries = new List<WindowConfiguration>();
        }
        /// <summary>
        /// Remove from the list where ID and key match.<para> If ID passed does not have a value, 
        /// then it will remove based on a Key  match only</para>
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Key"></param>
        protected void Remove(int? ID, string Key)
        {
            foreach (var e in ConfigurationEntries)
                if (ID.HasValue && e.ID == ID && e.Key == Key 
                    || !ID.HasValue && e.Key == Key)
                    ConfigurationEntries.Remove(e);
        }
        /// <summary>
        /// Basic add functionality. Should override if using a DB or individual record saving.
        /// <para>However, it would be good to call the base method for its validations(AllowDuplicate Key, check scope, etc),
        /// and to then call your own logic. If your save logic can undo adding it to <see cref="ConfigurationEntries"/>. </para>
        /// <para>E.g., call the protected/non virtual <see cref="Remove(int?, string)"/></para>
        /// </summary>
        /// <param name="newRecord"></param>
        /// <returns></returns>
        public virtual bool Add(WindowConfiguration newRecord)
        {
            if (!CheckScope(newRecord))
                return false;

            if (!AllowDuplicateConfigurationKey
                && ConfigurationEntries.Exists(w => w.Key == newRecord.Key))
                return false;

            if (newRecord.ID == null)
                newRecord.ID = ConfigurationEntries.Max(e => e.ID) + 1;
            else if (ConfigurationEntries.Exists(w => w.ID == newRecord.ID))
                return false;
            newRecord.Altered = true;
            //newRecord.IDName = WindowConfigurationHelper.GetIDName(ScopeValidation);
            ConfigurationEntries.Add(newRecord);
            return true;
        }
        /// <summary>
        /// Checks the scope and confirms that the record matches the container's scope
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public virtual bool CheckScope(WindowConfiguration record)
            => record.MyScope == ValidationScope;
                
        /// <summary>
        /// Should be used to confirm that the Configuration Record being added 
        /// <para>is for the same scope as the ConfigurationList.</para>
        /// </summary>
        [XmlIgnore]
        public WindowConfigurationScope ValidationScope { get; }
        /// <summary>
        /// Returns the data table for the ConfigurationEntries.
        /// <para>If supporting paging, data table should be changed by calling RePage</para>
        /// </summary>        
        /// <returns></returns>
        [XmlIgnore]
        public virtual DataTable MyData
        {
            get
            {
                return ConfigurationEntries
                    .ToDataTable(nameof(iWindowConfiguration.MyScope), 
                                 nameof(iWindowConfiguration.RecordVersion),
                                 nameof(iWindowConfiguration.Altered)
                                 );
            }
        }
        /// <summary>
        /// If true, will avoid adding records if the Key is already in use.
        /// <para>If using paging, will need to implement a more advanced check, but</para>
        /// <para> the basic validation may still be useful for reducing DB hits trying to add a duplicate or something</para>
        /// </summary>
        [XmlIgnore]
        public bool AllowDuplicateConfigurationKey { get; } = false;
        /*
        /// <summary>
        /// If <paramref name="SupportPaging"/> is true, to be called when a parent scope's current record is changed..<para></para>
        /// E.g., the Seidr Window menu changes  its query, CM scope should be re-paged then
        /// </summary>
        /// <param name="Key"></param>
        public abstract void RePage(long? Key);
        /// <summary>
        /// Sets the page to specified page #. Default 0.
        /// <para>Only used if SupportPaging is set to true.</para>
        /// <para>Otherwise, should do nothing. However, it is called in the base constructor and should not throw exceptions.</para>
        /// </summary>
        /// <param name="Page"></param>
        public abstract void SetPage(int Page);
        */
        /// <summary>
        /// To be called when deleting a record
        /// </summary>
        /// <param name="ID"></param>
        public virtual void Remove(int ID)
        {
            //ConfigurationEntries.Exclude(w => w.ID == ID);
            ConfigurationEntries.RemoveAt(ConfigurationEntries.FindIndex(w => w.ID == ID));
        }
        public Guid Version { get; set; }

        /// <summary>
        /// Returns true if the WindowConfiguration has Lookups
        /// </summary>
        [XmlIgnore]
        public bool Cloneable
        {
            get
            {
                return typeof(WindowConfiguration)
                    .GetCustomAttributes(typeof(LookupSourceAttribute), false)
                    .Where(l => ((LookupSourceAttribute)l).ForCloning)
                    .HasMinimumCount(1);
            }
        }
        //[XmlIgnore]
        //public virtual bool Cloneable { get; } = false;
        /*
        /// <summary>
        /// If true, will not expect everything to be populated in the list. Should really only be used for users probably..        
        /// </summary>
        [XmlIgnore]
        public bool SupportPaging { get; }* /
        
            //TODO: Save/Loading logic should be exclusively in the Broker
            */
        /// <summary>
        /// Called to update the records in the list. If using a database, can limit to records with Altered = true and check record versions.. 
        /// <para>but may want to do updates before waiting for save to be called then.</para>
        /// </summary>
        public abstract void Save();        
        /*
        /// <summary>
        /// Gets/sets the location for saving to file with the default implementation 
        /// <para>Value should be ignored if saving to a DB...</para>
        /// </summary>
        [XmlIgnore]
        public string RawFileLocation { get; set; }
        */
        /// <summary>
        /// For storing information about the source of the WindowConfiguration.
        /// <para>E.g., when using file serialization, FilePath</para>
        /// <para>When using a database, any identification needed for saving/re-loading.</para>
        /// </summary>
        [XmlIgnore]
        public WindowConfigurationLoadModel LoadModel { get; set; }
        /// <summary>
        /// Simplest method - call Serialize, then Deserialize
        /// </summary>
        /// <returns></returns>
        public abstract WindowConfigurationList<WindowConfiguration> cloneSetup();
    }



    public interface iWindowConfigurationList<T> where T: iWindowConfiguration
    {
        bool Contains(string Key);
        bool Contains(int ID);
        bool Contains(int? ID);
        List<T> ConfigurationEntries { get; set; }
        bool HasAltered { get; }
        T this[int? ID] { get; set; }
        T this[int ID] { get; set; }
        T this[string key] { get; }
        DataTable MyData{ get;}
        void Update(T entry);
        bool Add(T newRecord);
        void Remove(int ID);
        void Save();
        WindowConfigurationScope ValidationScope { get; }
    }

    /*
    public static class WindowConfigurationHelper    
    {
        public static string GetIDName(WindowConfigurationScope s)
        {
            Guid g = Guid.NewGuid();
            return "_" + s.ToString() + "_" + g.ToString().Replace('-', '_').Replace("{", "").Replace("}", "");
        }
        public static WindowConfigurationScope GuessScope(string IDName)
        {
            if (IDName[0] == '_')
                IDName = IDName.Substring(1);
            var s = WindowConfigurationScope.UNK;
            if (Enum.TryParse(IDName.Substring(0, IDName.IndexOf('_')), out s))
                return s;
            return WindowConfigurationScope.UNK;
        }        
    }*/

    public enum WindowConfigurationScope
    {
        /// <summary>
        /// Unknown
        /// </summary>
        [Description("Any")]        
        UNK = 0,
        /// <summary>
        /// Query
        /// </summary>
        [Description("Query (Stored Procedure Call)")]
        Q,
        /// <summary>
        /// Context Menu
        /// </summary>
        [Description("Basic Context Menu Item")]
        CM, //Context Menu - will never be top level scope
        [Description("Context Menu Plugin")]
        ACM,
        /// <summary>
        /// Dashboard
        /// </summary>
        [Description("Detail Menu Item")]
        D, //Dashboard
        /// <summary>
        /// Database connection
        /// </summary>
        [Description("Database Connection")]
        DB,        
        /// <summary>
        /// Window Addon
        /// </summary>
        [Description("Application Plugin")]
        A, //Window addon   
        /// <summary>
        /// Switch
        /// </summary>
        [Description("Switch Menu Item")]
        SW, //Switch (Switches menu - that menu could be considered under query, then the actual switches are scoped as 'SW'. But they're also ignored by this class
        /// <summary>
        /// User 
        /// </summary>
        [Description("User")]
        U,
        /// <summary>
        /// Team
        /// </summary>
        [Description("Team")]
        TM,
        /// <summary>
        /// Admin level (used for lookup)
        /// </summary>
        [Description("Admin Level")]
        ADML
    }


}
