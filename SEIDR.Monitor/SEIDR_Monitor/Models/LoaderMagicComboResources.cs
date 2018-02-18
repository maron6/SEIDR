using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Windows;

namespace SEIDR.WindowMonitor.Models
{
    public static class LoaderMagicComboResources
    {        
        public static Dictionary<Type, List<ComboDisplay>> ComboResources { get; private set; }
        public static void AddResource(Type LMType, ComboDisplay resource)
        {
            if (ComboResources.ContainsKey(LMType))
            {
                var l = ComboResources[LMType];
                if (l.Contains(resource))
                    l.Remove(resource);
                l.Add(resource);
            }
            else
            {
                ComboResources[LMType] = new List<ComboDisplay>();
                ComboResources[LMType].Add(resource);
            }
        }
        public static ComboDisplay[] GetDisplay(Type LMType)
        {                        
            if (ComboResources.ContainsKey(LMType))
            {
                return ComboResources[LMType].ToArray();
            }
            return null;            
        }
    }
}
