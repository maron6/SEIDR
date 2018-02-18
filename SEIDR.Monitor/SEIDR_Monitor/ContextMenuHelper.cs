using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Configurations;
using SEIDR.WindowMonitor.MonitorConfigurationHelpers;
using static SEIDR.Dynamics.Configurations.ConfigListHelper;

namespace SEIDR.WindowMonitor
{
    public class ContextMenuHelper
    {
        public string TopLevelOwner; //Query and Dashboard - Self
        
        /// <summary>
        /// Returns the owner scope of the top level context item. If not set/saved in the file, will use the naming convention.
        /// </summary>
        public Scope topLevelScope
        {
            get
            {
                if (_topScope == Scope.UNK)
                {
                    //_topScope = GetScope(TopLevelOwner);
                    _topScope = GuessScope(TopLevelOwner);
                }
                return _topScope;
            }
            set
            {
                _topScope = value;
            }
        }
        public string Name;
        public string DisplayName;
        Scope _myScope = Scope.UNK;
        public Scope myScope
        {
            get
            {
                if (_myScope == Scope.UNK)
                    _myScope = GuessScope(Name); //GetScope(Name);
                return _myScope;
            }
            set
            {
                _myScope = value;
            }
        }
        Scope _topScope = Scope.UNK;

        public ContextMenuHelper( string displayName, string Name, string TopLevel = null, Scope topScope = Scope.UNK)
        {            
            TopLevelOwner = TopLevel?? "_SELF_";
            string x = (TopLevel ?? Name);
            this.Name = Name;            
            topLevelScope = topScope;
            DisplayName = displayName ?? Name;            
        }
        public Scope GetScope(string Name)
        {
            Scope rs; //ToDo: add a constant scope to the classes....Main purpose of using name is to be able to get from the top owner without actually loading the owner...
                        //although owner name needs to be able to identify source, unless an owner scope is added to ContextMenu
            string x = Name.ToUpper();
            if (x.StartsWith("D_"))            
                rs = Scope.D;            
            else if (x.StartsWith("CM_"))           
                rs = Scope.CM;            
            else if (x.StartsWith("Q_"))            
                rs = Scope.Q;            
            else if (x.StartsWith("A_"))
                rs = Scope.A;
            else if (x.StartsWith("SW_"))
                rs = Scope.SW;
            else
                rs = Scope.UNK;
            return rs;
        }
        public override string ToString()
        {
            return DisplayName ?? Name;
        }
        public string Description
        {
            get
            {
                return "Scope:" + myScope.ToString() + Environment.NewLine 
                    + "Top Owner: " + TopLevelOwner + Environment.NewLine 
                    + "Scope: " + topLevelScope.ToString();
             }
        }


        public static List<ContextMenuHelper> GetList(ContextMenuItems cmi, Queries ql, SEIDR_MenuAddOnConfigs al)
        {            
            List<ContextMenuHelper> rl = new List<ContextMenuHelper>();
            foreach (var d in cmi.GetDashboardList())
            {
                string tempDisplay = d;
                if (d.ToUpper().StartsWith("D_"))
                    tempDisplay = d.Substring(2);
                rl.Add(new ContextMenuHelper(tempDisplay, d) { topLevelScope = Scope.D, myScope = Scope.D });
            }
            foreach (var item in cmi)
            {
                if (item.IsSwitch)
                    continue; //Switches are not included in the list... Would be scope = 'SW', though
                string n = item.Name;
                string tempDisplay = item.DisplayName;
                var to = cmi.GetTopOwner(item);
                rl.Add(new ContextMenuHelper(tempDisplay, n, to.owner, to.OwnerScope) {  myScope = Scope.CM } );
            }
            foreach (var q in ql)
            {
                string tempDisplay = $"{q.MyName} (Connection: '{q.DBConnectionName}')";
                rl.Add(new ContextMenuHelper(tempDisplay, q.Name) { topLevelScope = Scope.Q, myScope = Scope.Q });
            }
            foreach(var a in al)
            {
                var md = SettingManager.myMenuLibrary?.GetMetaData(SettingManager._ME, a.AddonName);
                if (md == null || !md.UsesCaller)
                    continue; //Meta data needs to exist, and needs to indicate that it can update the DataGrid

                string tempDisplay = a.DisplayName ?? a.Name;
                if (tempDisplay.ToUpper().StartsWith("A_"))
                    tempDisplay = tempDisplay.Substring(2) + " (Connection: '" + a.DBConnection + "')";
                rl.Add(new ContextMenuHelper(tempDisplay, a.Name) { topLevelScope = Scope.A, myScope = Scope.A });
            }

            return rl;
        }
        public static List<ContextMenuHelper> GetSubList(List<ContextMenuHelper> cmList, Scope toGrab)
        {
            var search = from cmh in cmList
                         where cmh.myScope == toGrab
                         select cmh;
            return search.ToList();
        }

        public static int GetOwnerIndex(List<ContextMenuHelper> cmList, string ownerName)
        {            
            for(int x = 0; x < cmList.Count; x++)
            {
                var cmh = cmList[x];
                if (cmh.Name == ownerName)
                    return x;
            }
            return -1;
        }
    }
}
