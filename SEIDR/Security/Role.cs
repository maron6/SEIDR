using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Security
{
    public class Role
    {
        /// <summary>
        /// Default area, used as an overriding value.
        /// </summary>
        public const string SYSTEM_AREA = "____SYSTEM___";
        public Role(string areaName, Permission permission, bool denyPermission = false)
        {
            Area = areaName;
            AreaPermission = permission;
            Deny = denyPermission;
        }        
        public Role(string areaName, Permission permission, int subArea, bool denyPermission= false)
        {
            Area = areaName;
            SubArea = subArea;
            AreaPermission = permission;
            Deny = denyPermission;
        }
        public string Area { get; private set; }
        /// <summary>
        /// Key for identifying a subsection of area. Likely to be an ID.
        /// </summary>
        public int? SubArea { get; private set; } = null;
        public Permission AreaPermission { get; private set; }        
        /// <summary>
        /// Indicates that this Role is a denial of permission.
        /// </summary>
        public bool Deny { get; private set; }
        /// <summary>
        /// Parses a role from a data record
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public static Role ParseRole(Doc.IDataRecord record)
        {
            string Area = SYSTEM_AREA;
            if (record.HasColumn(nameof(Area)))
                Area = record[nameof(Area)];
            int? SubArea = null;
            if (record.HasColumn(nameof(SubArea)))
            {
                string sub = record[nameof(SubArea)];
                int temp;
                if (int.TryParse(sub, out temp))
                    SubArea = temp;
            }
            Permission AreaPermission;
            if (!record.HasColumn(nameof(AreaPermission)) 
            || !Enum.TryParse(record[nameof(AreaPermission)], out AreaPermission))
            {
                AreaPermission = Permission.NONE;
            }
            bool Deny = false;
            if (record.HasColumn(nameof(Deny)))
            {
                if (!bool.TryParse(record[nameof(Deny)], out Deny))
                    Deny = false;
            }
            if(SubArea.HasValue)
                return new Role(Area, AreaPermission, SubArea.Value, Deny);
            return new Role(Area, AreaPermission, Deny);

        }
    }
}
