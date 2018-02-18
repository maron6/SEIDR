using System.Collections.Generic;
using System.Linq;
using System.Data;
using System;
//using System.ComponentModel;
using System.Xml.Serialization;
using Ryan_UtilityCode.Dynamics;

namespace Ryan_UtilityCode.Dynamics.Configurations
{
    public sealed class ContextMenuItems:IEnumerable<ContextMenuItem>, iConfigList
    {
        public List<ContextMenuItem> MyItems = new List<ContextMenuItem>();
        public void Add(ContextMenuItem cmi)
        {
            if (this[cmi.Name] != null)
                throw new Exception("This name is already in use.");
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
        public List<string> GetDashboardList()
        {
            List<string> rl = new List<string>();
            foreach (var q in MyItems)
            {
                //No context menu for single detail, except refresh
                if (!string.IsNullOrWhiteSpace(q.Dashboard) && !q.SingelDetail)
                {
                    rl.Add(q.Dashboard);
                }
            }
            return rl;
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
            return _ret.ToArray<ContextMenuItem>();
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
                ContextMenuItem temp = this[cmi.owner];
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
        public bool Cloneable { get { return true; } }
        /// <summary>
        /// DataTable representation of data
        /// </summary>
        [XmlIgnore]
        public DataTable MyData
        {
            get
            {
                return MyItems.ToArray().ToDataTable();
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
        public Guid Version
        {
            get;
            set;
        }
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
    }
    /// <summary>
    /// Representation of context m enus in SEIDR.Window
    /// </summary>
    public sealed class ContextMenuItem
    {
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
                return DisplayName ?? Name.Replace("cm_", "");
            }
        }
        /// <summary>
        /// Display name for showing in the actual context menu
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Internal name for parenting
        /// </summary>
        public string Name { get; set; }
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
        public List<string> DataRowMappings { get; set; }
        //public Dictionary<string, string> Mappings { get; set; } //Data row mappings ONLY
        /// <summary>
        /// If true, context menu will take multiple data rows
        /// </summary>
        public bool MultiSelect { get; set; }
        /// <summary>
        /// If true, will open an EditableDashboard display to see the details from the procedure (which should return a single row)
        /// </summary>
        public bool SingelDetail { get; set; } 
        /// <summary>
        /// Parameter name for the sql procedure
        /// </summary>
        public string ProcIDParameterName { get; set; } //E.g., for set position = 1
        /// <summary>
        /// value associated with the parameter name, for when you want to set an ID that is not associated with the data itself
        /// </summary>
        public int? ProcID { get; set; }
        /// <summary>
        /// Name the dashboard, or null if a dashboard should not be opened.
        /// </summary>
        public string Dashboard { get; set; }
        /// <summary>
        /// AcceptProc for a single detail dashboard
        /// </summary>
        public string DetailChangeProc { get; set; }
        /// <summary>
        /// Column from datagrid to try opening as a file or program or somethin somethin
        /// </summary>
        //public string ColumnOpen { get; set; }
    }
}
