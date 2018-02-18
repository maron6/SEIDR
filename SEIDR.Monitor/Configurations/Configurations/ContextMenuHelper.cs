using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryan_UtilityCode.Dynamics.Configurations
{
    public class ContextMenuHelper
    {
        public string TopLevelOwner; //Query and Dashboard - Self
        public enum Scope
        {
            Q, 
            C, //Context Menu - will never be top level scope
            D,
            UNK,
            A
        }
        public Scope topLevelScope;
        public string myName;
        public Scope myScope;

        public ContextMenuHelper( string Name, string TopLevel = null)
        {            
            TopLevelOwner = TopLevel?? "_SELF_";
            string x = (TopLevel ?? Name);
            myName = Name;
            topLevelScope = GetScope(x);
            myScope = GetScope(Name);
        }
        public Scope GetScope(string Name)
        {
            Scope rs;
            string x = Name.ToUpper();
            if (x.StartsWith("D_"))
            {
                rs = Scope.D;
            }
            else if (x.StartsWith("CM_"))
            {
                rs = Scope.C;
            }
            else if (x.StartsWith("Q_"))
            {
                rs = Scope.Q;
            }
            else if (x.StartsWith("A_"))
                rs = Scope.A;
            else
                rs = Scope.UNK;
            return rs;
        }
        public override string ToString()
        {
            return myName;
        }
        public string Description
        {
            get
            {
                return "Top Owner: " + TopLevelOwner + Environment.NewLine + "Scope: " + topLevelScope.ToString();
             }
        }


        public static List<ContextMenuHelper> GetList(ContextMenuItems cmi, Queries ql)
        {
            List<ContextMenuHelper> rl = new List<ContextMenuHelper>();
            foreach (var d in cmi.GetDashboardList())
            {
                rl.Add(new ContextMenuHelper(d) { topLevelScope = Scope.D });
            }
            foreach (var item in cmi)
            {
                string n = item.Name;
                var to = cmi.GetTopOwner(item);
                rl.Add(new ContextMenuHelper(n, to.owner));
            }
            foreach (var q in ql)
            {
                rl.Add(new ContextMenuHelper(q.Name) { topLevelScope = Scope.Q });
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
                if (cmh.myName == ownerName)
                    return x;
            }
            return -1;
        }
    }
}
