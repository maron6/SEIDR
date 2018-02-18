using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Dynamics.Configurations.ContextMenuConfiguration
{
    public class ParameterInfo
    {
        public string Key { get; set; }
        public object Value { get; set; }
        public static Dictionary<string, object> ToDictionary(IEnumerable<ParameterInfo> list)
        {
            return list.ToDictionary(l => l.Key, l => l.Value);
        }
    }
}
