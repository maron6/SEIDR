using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Security
{
    /// <summary>
    /// Represent a set of roles for a user or security
    /// </summary>
    public class RoleCollection: IList<Role>
    {
        /// <summary>
        /// Check for a Role entry which provides sufficiently matching permission.
        /// <para>Order of checking: Area = <see cref="Role.SYSTEM_AREA"/>, Area (null subArea) match against <paramref name="toCheck"/>, Area + subArea match match.</para>
        /// </summary>
        /// <param name="toCheck">If <paramref name="toCheck"/> is flagged as permission denial, will return true if the permision is denied.
        /// <para>If <paramref name="toCheck"/> is not flagged as permission denial will return true if the permission is granted. Else false.</para></param>
        /// <returns></returns>
        public bool CheckRole(Role toCheck)
        {            
            var checks = roles.Where(r => r.Area == Role.SYSTEM_AREA && r.Deny);            
            #region check permission denials
            foreach (var check in checks)
            {
                if((check.AreaPermission & toCheck.AreaPermission) == toCheck.AreaPermission)
                {
                    return toCheck.Deny; //Checking for denials first. If found a denial, then return whether or not we want a denial.                                        
                }
            }
            checks = roles.Where(r => r.Area == toCheck.Area && r.SubArea == null && r.Deny);
            foreach(var check in checks)
            {
                if ((check.AreaPermission & toCheck.AreaPermission) == toCheck.AreaPermission)
                {
                    return toCheck.Deny;
                    //if checking for a non denial, then look for true != false. (!toCheck.Deny != check.Deny)
                    //if checking for a denial of permission, then look for false != true
                }
            }
            if (toCheck.SubArea.HasValue)
            {
                checks = roles.Where(r => r.Area == toCheck.Area && r.SubArea == toCheck.SubArea && r.Deny);
                foreach (var check in checks)
                {
                    if ((check.AreaPermission & toCheck.AreaPermission) == toCheck.AreaPermission)
                    {
                        return toCheck.Deny;
                    }
                }
            }
            #endregion

            #region check permission grants
            checks = roles.Where(r => r.Area == Role.SYSTEM_AREA && !r.Deny);
            foreach (var check in checks)
            {
                if ((check.AreaPermission & toCheck.AreaPermission) == toCheck.AreaPermission)
                {
                    return !toCheck.Deny; //Checking for denials first. If found a denial, then return whether or not we want a denial.                                        
                }
            }
            checks = roles.Where(r => r.Area == toCheck.Area && r.SubArea == null && !r.Deny);
            foreach (var check in checks)
            {
                if ((check.AreaPermission & toCheck.AreaPermission) == toCheck.AreaPermission)
                {
                    return !toCheck.Deny;
                    //if checking for a non denial, then look for true != false. (!toCheck.Deny != check.Deny)
                    //if checking for a denial of permission, then look for false != true
                }
            }
            if (toCheck.SubArea.HasValue)
            {
                checks = roles.Where(r => r.Area == toCheck.Area && r.SubArea == toCheck.SubArea && !r.Deny);
                foreach (var check in checks)
                {
                    if ((check.AreaPermission & toCheck.AreaPermission) == toCheck.AreaPermission)
                    {
                        return !toCheck.Deny;
                    }
                }
            }
            #endregion


            return toCheck.Deny; //if looking for a denial, then true if we get here (nothing passed)
        }
        /// <summary>
        /// Returns true if this collection matches ALL roles from the provided list
        /// </summary>
        /// <param name="toCheck"></param>
        /// <returns></returns>
        public bool MatchAll(IEnumerable<Role> toCheck)
        {
            if (toCheck.Exists(c => !CheckRole(c)))
                return false;
            return true;
        }
        /// <summary>
        /// Checks that at least one of the roles is a match against this collection
        /// </summary>
        /// <param name="toCheck"></param>
        /// <returns></returns>
        public bool MatchAny(IEnumerable<Role> toCheck)
        {
            return toCheck.Exists(c => CheckRole(c));
        }
        List<Role> roles;
        /// <summary>
        /// Checks for granted permissions in the area (and subArea if applicable)
        /// </summary>
        /// <param name="area"></param>
        /// <param name="subArea"></param>
        /// <returns></returns>
        public Permission CheckPermission(string area, int? subArea = null)
        {
            Permission denied = Permission.NONE;
            var denialCheck = roles.Where(r => r.Deny
                                        && r.Area.In(Role.SYSTEM_AREA, area)
                                        && (r.Area == Role.SYSTEM_AREA || r.SubArea == null || r.SubArea == subArea));
            foreach(var check in denialCheck)
            {
                denied |= check.AreaPermission;
            }
            Permission granted = Permission.NONE;
            var grantCheck = roles.Where(r => !r.Deny && r.Area.In(Role.SYSTEM_AREA, area)
                                        && (r.Area == Role.SYSTEM_AREA || r.SubArea == null || r.SubArea == subArea));
            foreach(var check in grantCheck)
            {
                granted |= check.AreaPermission;
            }
            return granted & (granted ^ denied); //Compare granted against granted XOR denied. 
            /*
             * From XOR, Permissions from granted that match denied will be 0, while mismatches will be 1.
             * Then, check that the mismatches (1) are also 1 in the original granted
             * 
             */
        }
        /// <summary>
        /// Checks for a list of sub areas which do not have the exact specified permission granted.
        /// <para>If the area does not have permission, will throw a <see cref="NoPermissionException"/>.</para>
        /// </summary>
        /// <param name="area"></param>
        /// <param name="checkPermission"></param>
        /// <returns></returns>
        public List<int> CheckDeniedSubareas(string area, Permission checkPermission)
        {
            Permission actual = CheckPermission(area, null);
            if ((actual & checkPermission) != checkPermission)
                throw new NoPermissionException(area, checkPermission, actual);
            List<int> subAreas = new List<int>();
            var denialCheck = roles.Where(r => r.Deny && r.Area == area && r.SubArea != null);
            foreach(var denial in denialCheck)
            {
                if (subAreas.Exists(sa => sa == denial.SubArea))
                    continue;
                //If subarea denies any part of the permission being checked, include it.
                if ((denial.AreaPermission & checkPermission) != Permission.NONE) 
                    subAreas.Add(denial.SubArea.Value);
            }
            return subAreas;
        }
        /// <summary>
        /// Parses the role information from the list of records.
        /// </summary>
        /// <param name="recordSet"></param>
        /// <returns></returns>
        public static RoleCollection ParseRoleCollection(IEnumerable<Doc.IDataRecord> recordSet)
        {
            RoleCollection ret = new RoleCollection();
            foreach(var record in recordSet)
            {
                ret.Add(Role.ParseRole(record));
            }
            return ret;
        }

        public Role this[int index] { get => ((IList<Role>)roles)[index]; set => ((IList<Role>)roles)[index] = value; }

        public int Count => ((IList<Role>)roles).Count;

        public bool IsReadOnly => ((IList<Role>)roles).IsReadOnly;

        public void Add(Role item)
        {
            ((IList<Role>)roles).Add(item);
        }

        public void Clear()
        {
            ((IList<Role>)roles).Clear();
        }

        public bool Contains(Role item)
        {
            return ((IList<Role>)roles).Contains(item);
        }

        public void CopyTo(Role[] array, int arrayIndex)
        {
            ((IList<Role>)roles).CopyTo(array, arrayIndex);
        }

        public IEnumerator<Role> GetEnumerator()
        {
            return ((IList<Role>)roles).GetEnumerator();
        }

        public int IndexOf(Role item)
        {
            return ((IList<Role>)roles).IndexOf(item);
        }

        public void Insert(int index, Role item)
        {
            ((IList<Role>)roles).Insert(index, item);
        }

        public bool Remove(Role item)
        {
            return ((IList<Role>)roles).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<Role>)roles).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<Role>)roles).GetEnumerator();
        }
    }
}
