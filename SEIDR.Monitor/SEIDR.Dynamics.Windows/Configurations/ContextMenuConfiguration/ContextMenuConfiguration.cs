using SEIDR.Dynamics.Configurations.UserConfiguration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Xml.Serialization;
using SEIDR.Dynamics.Configurations.Encryption;

namespace SEIDR.Dynamics.Configurations.ContextMenuConfiguration
{
    public class ContextMenuList : WindowConfigurationList<ContextMenuConfiguration>
    {
        public ContextMenuList(): base( WindowConfigurationScope.CM){ }

        public override bool CheckScope(ContextMenuConfiguration record)
        => record.MyScope.In(                
                WindowConfigurationScope.D,
                WindowConfigurationScope.SW,
                WindowConfigurationScope.CM);           
        public IEnumerable<ContextMenuConfiguration> GetSubList(WindowConfigurationScope subScope)
            => ConfigurationEntries.Where(ce => ce.MyScope == subScope);

        /// <summary>
        /// Basic save - saves to a file specified by the load model.
        /// </summary>
        public override void Save()
        {
            var other = LoadModel.Tag.ToString().DeserializeXML<ContextMenuList>();
            if (other != null && other.Version != Version)
                throw new Exception("The record has been changed by another user.");
            string content = this.SerializeToXML();
            if (!LoadModel.UserSpecific)
                content = content.Encrypt(LoadModel.Key);
            System.IO.File.WriteAllText(LoadModel.Tag.ToString(), content);
            ConfigurationEntries.Where(c => c.Altered).ForEach(c => c.Altered = false);
        }

        //public override bool Cloneable
        //{
        //    get
        //    {
        //        return true;
        //    }
        //}
        public override DataTable MyData
        {
            get
            {
                return ConfigurationEntries.ToDataTableLimited(
                    nameof(ContextMenuConfiguration.Key), 
                    nameof(ContextMenuConfiguration.Description), 
                    nameof(ContextMenuConfiguration.ParentScope), 
                    nameof(ContextMenuConfiguration.Dashboard),
                    nameof(ContextMenuConfiguration.IsSwitch)
                    );
            }
        }
        public override WindowConfigurationList<ContextMenuConfiguration> cloneSetup()
        {
            return this.XClone();
        }
        public IEnumerable<ContextMenuConfiguration> GetChildren(iWindowConfiguration parent)
        {
            return from c in ConfigurationEntries
                   where c.ParentID == parent.ID
                   && c.ParentScope == parent.MyScope
                   select c;
        }
        public ContextMenuConfiguration[] GetAllChildren(iWindowConfiguration BaseOwner)
        {
            var ret = new List<ContextMenuConfiguration>(GetChildren(BaseOwner));
            List<ContextMenuConfiguration> working;
            int bLength = ret.Count;
            for (int i = 0; i < bLength; i++)
            {
                working = new List<ContextMenuConfiguration>(GetChildren(ret[i]));
                ret.AddRange(working);
            }
            return ret.ToArray();
        }
        public int[] GetAllChildrenWithOwner(iWindowConfiguration BaseOwner)
        {
            List<int> ret = new List<int>();
            ret.Add(BaseOwner.ID.Value);
            var retW = from item in GetAllChildren(BaseOwner)
                       select item.ID.Value;
            ret.AddRange(retW.ToList());
            return ret.ToArray<int>();
        }
        public void RemoveChildren(iWindowConfiguration BaseOwner)
        {
            var children = GetAllChildren(BaseOwner);
            foreach (var cmi in children)
            {
                Remove(cmi.ID.Value);
            }
        }

        public List<string> GetDashboardList()
        {
            return (from q in GetSubList(WindowConfigurationScope.D)
                    where !q.SingleDetail && !string.IsNullOrWhiteSpace(q.Dashboard)
                    select q.Dashboard).Distinct().ToList();            
        }
        public void ValidateOwners(QueryConfiguration.QueryList qList)
        {
            var invalid = (from e in ConfigurationEntries
                           where e.ParentScope == WindowConfigurationScope.Q
                           && !qList.Contains(e.ParentID)
                           select e);
            foreach(var e in invalid)
            {
                e.ParentID = null;
                e.ParentScope = WindowConfigurationScope.UNK;
            }
        }
        public void ValidateAllOwners(IEnumerable<iWindowConfiguration> allConfigurations)
        {
            foreach(var cm in ConfigurationEntries)
            {
                if(allConfigurations.NotExists(a => a.MyScope == cm.ParentScope && a.ID == cm.ParentID))
                {
                    cm.ParentID = null;
                    cm.ParentScope = WindowConfigurationScope.UNK;
                }
            }
        }
        [Obsolete("ContextMenuConfiguration just points to an addon configuration. Don't need to merge, just grab the context menu configurations", true)]
        public static IEnumerable<iWindowConfiguration> MergeAllChildren(iWindowConfiguration owner, ContextMenuList cm, AddonConfiguration.ContextAddonList acm)
        {
            List<iWindowConfiguration> r = new List<iWindowConfiguration>(cm.GetAllChildren(owner));
            r.AddRange(acm.ConfigurationEntries.Where(c => c.Parent == owner.ID && c.ParentScope == owner.MyScope));
            return r;
        }
    }
    [WindowConfigurationEditorTabs(new []{        
        "General", "Switch", "AddOn"}, 
        BasicUserPermissions.ContextMenuEditor, 
        BasicUserPermissions.None, 
        BasicUserPermissions.AddonEditor )]
    public class ContextMenuConfiguration : iWindowConfiguration
    {
        public bool Altered { get; set; }
        
        public int? ID { get; set; }

        //[LookupSource(WindowConfigurationScope.D, nameof(ParentScope))] //Will be grabbed when getting CM parents
        [LookupSource(WindowConfigurationScope.Q, nameof(ParentScope))]
        [LookupSource(WindowConfigurationScope.CM, nameof(ParentScope))]        
        [LookupSource(WindowConfigurationScope.A, nameof(ParentScope))]
        //[WindowConfigurationEditorElementInfo("Owner", 0, true)]
        //[WindowConfigurationEditorElementInfo("Switch-From Query", 1, true)]
        public int? ParentID { get; set; }

        public string Key { get; set; }
        public string Description { get; set; }
        [DefaultValue(null)]
        //[WindowConfigurationEditorElementInfo(nameof(Dashboard), 0, false)]
        public string Dashboard { get; set; }
        //[WindowConfigurationEditorElementInfo("Single Record Detail", 0, false)]
        [DefaultValue(false)]
        public bool SingleDetail { get; set; } = false;
        [DefaultValue(null)]
        //[WindowConfigurationEditorElementInfo("Single Detail 'Save' procedure", 0)]
        public string DetailChangeProc { get; set; }
        [DefaultValue(null)]
        //Empty context menus okay - can just be used for hierarchy
        //[WindowConfigurationEditorElementInfo("Procedure to Call", 0, false)]
        public string ProcedureCall { get; set; }
        //[WindowConfigurationEditorElementInfo("ID Parameter", 0)]
        [DefaultValue(null)]
        public string ProcIDParameterName { get; set; }
        //[WindowConfigurationEditorElementInfo("ID Parameter Value", 0)]
        [DefaultValue(null)]
        public long? ProcID { get; set; }

        [DefaultValue(false)]
        public bool CreateNewRecord { get; set; } = false;
        //[WindowConfigurationEditorElementInfo("Multi Select",  0)]
        //[WindowConfigurationEditorElementInfo("Multi Select", 2)]
        [DefaultValue(false)]
        public bool MultiSelect { get; set; }
        //[WindowConfigurationEditorElementInfo("Use Queue", 0)]
        //[WindowConfigurationEditorElementInfo("Use Queue", 2)]
        [DefaultValue(false)]
        public bool UseQueue { get; set; }
        //Should have similar handling to editing Queries... This is for context menus callign Addons, though
        //(calling procedures from context menu just uses the datagrid as parameters.. Although maybe just allow specifying extra parameters?
        //Could probably do the same as Query configuration after all, just call as though it's run time.
        [DefaultValue(null)]
        //[WindowConfigurationEditorElementInfo("Parameters", 2)]
        public List<ParameterInfo> ParameterInfo { get; set; }

        /// <summary>
        /// Target a Query. A sort of unique case for a lookup... so 
        /// </summary>
        [LookupSource(WindowConfigurationScope.Q, forCloning:false)]
        //[WindowConfigurationEditorElementInfo("Switch-To Query", 1, true)]
        public int? SwitchID { get; set; }


        [LookupSource(WindowConfigurationScope.ACM)]
        //[WindowConfigurationEditorElementInfo("Plugin", 2, true)]
        public int? AddonID { get; set; }

        [XmlIgnore]
        [WindowConfigurationEditorIgnore]
        public bool IsSwitch { get { return SwitchID.HasValue; } }
        [WindowConfigurationEditorIgnore]
        public WindowConfigurationScope ParentScope { get; set; } = WindowConfigurationScope.UNK;
        public WindowConfigurationScope MyScope
        {
            get
            {
                if (SwitchID != null)
                    return WindowConfigurationScope.SW;
                if (string.IsNullOrWhiteSpace(Dashboard))
                    return WindowConfigurationScope.CM;
                return WindowConfigurationScope.D;
            }
        }

        public int RecordVersion { get; set; }
    }
}
