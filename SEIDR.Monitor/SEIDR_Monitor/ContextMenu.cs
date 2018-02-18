using System.Collections.Generic;
using System.Linq;
using System.Data;
using System;
//using System.ComponentModel;
using System.Xml.Serialization;
using SEIDR.Dynamics;
using SEIDR.Dynamics.Configurations;
using SEIDR.WindowMonitor.MonitorConfigurationHelpers;
using static SEIDR.Dynamics.Configurations.ConfigListHelper;

namespace SEIDR.WindowMonitor
{
    public sealed class ContextMenuItems : iConfigList, IEnumerable<ContextMenuItem>
    {
        public ContextMenuItems()
        {
            _Version = GetIDName(Scope.CM);
        }
        public Guid Version
        {
            get;
            set;
        } = Guid.NewGuid();
        public string _Version
        {
            get; set;
        }
        public static bool Compare(ContextMenuItems c1, ContextMenuItems c2)
        {
            return c1._Version == c2._Version; //To use if main version isn't being stored for some reason...
        }
        public void NewVersion() { _Version = GetIDName(Scope.CM); }
        public List<ContextMenuItem> MyItems = new List<ContextMenuItem>();
        public void Add(ContextMenuItem cmi)
        {
            if (this[cmi.Name] != null)
                throw new Exception("This internal name is already in use.");
            MyItems.Add(cmi);
        }
        public List<string> GetNameList()
        {
            List<string> rl = new List<string>();
            foreach (var q in MyItems)
            {
                rl.Add(q.Name);
            }
            return rl;
        }
        public bool Contains(string name)
        {
            return this[name] != null;
        }
        public List<string> GetDashboardList()
        {
            return (from q in MyItems
                    where !q.SingleDetail && !string.IsNullOrWhiteSpace(q.Dashboard)
                    select q.Dashboard).Distinct().ToList();
            //Merge distinct dashboards, allow them to share context menus...

            /* 
            List<string> rl = new List<string>();
            foreach (var q in MyItems)
            {
                //No context menu for single detail, except refresh
                if (!string.IsNullOrWhiteSpace(q.Dashboard) && !q.SingelDetail)
                {
                    rl.Add(q.Dashboard);
                }
            }
            return rl;*/
        }
        /// <summary>
        /// Check that owners exist before saving
        /// </summary>
        public void ValidateOwners()
        {
            var a = SettingManager.myAddons.GetNameList();
            var q = SettingManager.myQueries.GetNameList();
            var cm = GetNameList();
            var d = GetDashboardList();
            var invalidOwner = (from c in MyItems
                                where 
                                ( c.OwnerScope == Scope.A && !a.Contains(c.owner) )
                                || ( c.OwnerScope == Scope.CM && !cm.Contains(c.owner) )
                                || ( c.OwnerScope == Scope.D && !cm.Contains(c.owner) )
                                || ( (c.OwnerScope == Scope.Q || c.OwnerScope == Scope.SW) && !cm.Contains(c.owner) )
                                || ( c.OwnerScope == Scope.SW && !cm.Contains(c.Target) )
                                select c
                                ).ToList();
            MyItems.RemoveAll(c => (invalidOwner.Contains(c) && c.IsSwitch));
            invalidOwner.RemoveAll(c => (c.IsSwitch)); //Remove switches
            invalidOwner.ForEach(c => { c.owner = null; c.OwnerScope = Scope.UNK; });
        }
        [XmlIgnore]
        public ContextMenuItem this[string Name]
        {
            get
            {
                foreach (var i in MyItems)
                {
                    if (i.Name == Name)
                        return i;
                }
                return null;
            }
            set
            {
                for (int x = 0; x < MyItems.Count; x++)
                {
                    var i = MyItems[x];
                    if (i.Name == Name)
                    {
                        MyItems[x] = value;
                        return;
                    }
                }
                MyItems.Add(value);
            }
        }
        public ContextMenuItem[] GetChildren(string Owner)
        {
            var _ret = from item in MyItems
                       where item.owner == Owner
                       select item;
            return _ret.ToArray();
        }
        /// <summary>
        /// Get all the children and children's children for the given owner.
        /// <remarks>
        /// Main use should be for the settings editor windows where we want a list of available Menu owners for the selected query
        /// </remarks>
        /// </summary>
        /// <param name="BaseOwner"></param>
        /// <returns></returns>
        public ContextMenuItem[] GetAllChildren(string BaseOwner)
        {
            List<ContextMenuItem> ret = new List<ContextMenuItem>(GetChildren(BaseOwner));
            List<ContextMenuItem> working;
            int bLength = ret.Count;
            for (int i = 0; i < bLength; i++)
            {
                working = new List<ContextMenuItem>(GetChildren(ret[i].Name));
                ret.AddRange(working);
            }
            return ret.ToArray<ContextMenuItem>();
        }
        public string[] GetAllChildrenWithOwner(string BaseOwner)
        {
            List<string> ret = new List<string>();
            ret.Add(BaseOwner);
            var retW = from item in GetAllChildren(BaseOwner)
                       select item.Name;
            ret.AddRange(retW.ToList<string>());
            return ret.ToArray<string>();
        }
        public void RemoveChildren(string BaseOwner)
        {
            var children = GetAllChildren(BaseOwner);
            foreach (ContextMenuItem cmi in children)
            {
                MyItems.Remove(cmi);
            }
        }
        /// <summary>
        /// Remove the passed item. Note: If the item has any children, their owner will be set to that of the record being deleted
        /// </summary>
        /// <param name="Item"></param>
        public void Remove(string Item)
        {
            var cmi = this[Item];
            foreach (var child in GetChildren(Item))
            {
                child.owner = cmi.owner;
            }
            MyItems.Remove(cmi);
        }

        public ContextMenuItem GetTopOwner(ContextMenuItem cmi)
        {
            ContextMenuItem work = cmi;
            while (true)
            {
                if (string.IsNullOrWhiteSpace(work.owner))
                    break;
                ContextMenuItem temp = this[work.owner];
                if (temp == null)
                    break;
                work = temp;
            }
            return work;
        }
        public List<string> ToStringList(bool AddNew = true)
        {
            List<string> ret = new List<string>();
            if (AddNew)
            {
                ret.Add("(New Context Menu Item)");
            }
            foreach (var i in MyItems)
            {
                ret.Add(i.Name);
            }
            return ret;
        }

        public int GetIndex(string idx, bool IncludeNew = true)
        {
            for (int i = 0; i < MyItems.Count; i++)
            {
                if (MyItems[i].Name == idx)
                {
                    return i + (IncludeNew ? 1 : 0);
                }
            }
            return -1;
        }
        /// <summary>
        /// iConfigDisplay allows cloning
        /// </summary>
        [XmlIgnore]
        public bool Cloneable { get { return true; } }
        /// <summary>
        /// DataTable representation of data
        /// </summary>
        [XmlIgnore]
        public DataTable MyData
        {
            get
            {
                var tempItems = (from item in MyItems
                                 where item.MyName != null
                                  && (item.AddOn == null 
                                    || (SettingManager._ME.CanEditContextAddons && SettingManager._ME.CanUseAddons))
                                 select item).OrderBy(o=> o.owner).OrderBy(o=> o.Name).ToArray();
                return tempItems.ToDataTable("ParameterInfo", "Target", /*"IsSwitch",*/ "owner", "MyName",
                    "Procedure", "ProcID", "ProcIDParameterName", "DetailChangeProc");                
                
                /*
                DataTable dt = new DataTable();
                dt.Columns.Add("Name", typeof(string));
                dt.Columns.Add("Owner", typeof(string));
                dt.Columns.Add("Procedure", typeof(string));
                dt.Columns.Add("MultiSelect", typeof(bool));
                dt.Columns.Add("Dashboard", typeof(string));
                foreach (ContextMenuItem cmi in MyItems)
                {
                    dt.Rows.Add(cmi.Name, cmi.owner, cmi.Procedure, cmi.MultiSelect, cmi.Dashboard);
                }
                return dt;
                */
            }
        }
        /// <summary>
        /// For keeping users from overwriting the configList at the same time
        /// </summary>
        
        /// <summary>
        /// Enumerate the context menu items. Allow using with Linq
        /// </summary>
        /// <returns></returns>
        public IEnumerator<ContextMenuItem> GetEnumerator()
        {
            return MyItems.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return MyItems.GetEnumerator();
        }

        public iConfigList cloneSetup()
        {
            ContextMenuItems clone = new ContextMenuItems();
            clone.MyItems = new List<ContextMenuItem>(MyItems);
            return clone;
        }
    }
    /// <summary>
    /// Representation of context menus in SEIDR.Window
    /// </summary>
    public sealed class ContextMenuItem
    {
        /// <summary>
        /// Display name for showing in the actual context menu
        /// </summary>
        public string DisplayName { get; set; }
        [System.ComponentModel.DefaultValue(Scope.UNK)]
        public Scope OwnerScope { get; set; } = Scope.UNK;
        /// <summary>
        /// Parameter mappings for AddOn
        /// </summary>
        [System.ComponentModel.DefaultValue(null)]
        public List<ParameterInfo> ParameterInfo { get; set; }
        /// <summary>
        /// Null for standard procedure. 
        /// </summary>
        public string AddOn { get; set; }
        /// <summary>
        /// Returns the display name or internal name without 'cm_'
        /// </summary>
        [XmlIgnore]
        public string MyName
        {
            get
            {
                if (IsSwitch)
                {
                    return SettingManager.myQueries[Target]?.MyName; //Target
                }
                return DisplayName ?? Name.Replace("cm_", "");
            }
        }
        [XmlIgnore]
        public bool IsSwitch
        {
            get
            {
                return Target != null;
            }
        }
        [System.ComponentModel.DefaultValue(false)]
        public bool UseQueue { get; set; } = false;
        
        /// <summary>
        /// Internal name for parenting
        /// </summary>
        public string Name { get; set; }

        public ContextMenuItem()
        {
            Name = GetIDName(Scope.CM);
        }
        /// <summary>
        /// Top level owner should be the actual query
        /// </summary>
        public string owner { get; set; }
        /// <summary>
        /// Name of sql stored procedure to call
        /// </summary>
        public string Procedure { get; set; }
        /// <summary>
        /// Populate via selected data row
        /// </summary>        
        //public List<string> DataRowMappings { get; set; }
        //public Dictionary<string, string> Mappings { get; set; } //Data row mappings ONLY
        /// <summary>
        /// If true, context menu will take multiple data rows
        /// </summary>
        public bool MultiSelect { get; set; }
        /// <summary>
        /// If true, will open an EditableDashboard display to see the details from the procedure (which should return a single row)
        /// </summary>
        public bool SingleDetail { get; set; }
        /// <summary>
        /// Parameter name for the sql procedure
        /// </summary>
        public string ProcIDParameterName { get; set; } = null; //E.g. 'Position', for set position = 1 (ProcID)
        /// <summary>
        /// value associated with the parameter name, for when you want to set an ID that is not associated with the data itself
        /// </summary>
        [System.ComponentModel.DefaultValue(null)]
        public int? ProcID { get; set; } = null;
        /// <summary>
        /// Name the dashboard, or null if a dashboard should not be opened.
        /// </summary>
        public string Dashboard { get; set; } = null;
        /// <summary>
        /// AcceptProc for a single detail dashboard
        /// </summary>
        public string DetailChangeProc { get; set; }
        

        /// <summary>
        /// Target for Switches
        /// </summary>
        public string Target { get; set; }
    }
}
