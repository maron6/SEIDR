using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace SEIDR.Dynamics.Configurations
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
        iConfigList cloneSetup();        
    }
    public class ConfigListHelper
    {
        public static string GetIDName(Scope s)
        {
            Guid g = Guid.NewGuid();
            return "_" + s.ToString() + "_" + g.ToString().Replace('-', '_').Replace("{", "").Replace("}", "");
        }
        public static Scope GuessScope(string IDName)
        {
            
            if (IDName[0] == '_')
                IDName = IDName.Substring(1);
            Scope s = Scope.UNK;
            if (Enum.TryParse(IDName.Substring(0, IDName.IndexOf('_')), out s))
                return s;
            return Scope.UNK;
        }
        public enum Scope
        {
            /// <summary>
            /// Query
            /// </summary>
            Q, //Query
            /// <summary>
            /// Context Menu
            /// </summary>
            CM, //Context Menu - will never be top level scope
            /// <summary>
            /// Dashboard
            /// </summary>
            D, //Dashboard
            /// <summary>
            /// Unknown
            /// </summary>
            UNK,
            /// <summary>
            /// Window Addon
            /// </summary>
            A, //Window addon   
            /// <summary>
            /// Switch
            /// </summary>
            SW //Switch (Switches menu - that menu could be considered under query, then the actual switches are scoped as 'SW'. But they're also ignored by this class
        }
    }

}
