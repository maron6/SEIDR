using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace Ryan_UtilityCode.Dynamics.Configurations
{
    public interface iConfigList
    {
        List<string> GetNameList();        
        List<string> ToStringList(bool AddNew = true);
        DataTable MyData { get; }
        int GetIndex(string idx, bool IncludeNew = true);
        void Remove(string NameKey);
        Guid Version { get; set; }
        bool Cloneable { get; }
    }
}
