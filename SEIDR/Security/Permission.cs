using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Security
{
    /// <summary>
    /// Permission paradigm
    /// </summary>
    [Flags]
    public enum Permission
    {
        /// <summary>
        /// Nothing
        /// </summary>
        NONE = 0,
        /// <summary>
        /// Allow viewing data
        /// </summary>
        READ = 1,
        /// <summary>
        /// Allow inserting new data
        /// </summary>
        INSERT = 2 * READ,
        /// <summary>
        /// Allow updating existing data
        /// </summary>
        UPDATE = 2 * INSERT,
        /// <summary>
        /// Insert/update permissions
        /// </summary>
        WRITE = INSERT|UPDATE,
        /// <summary>
        /// Read/Insert/Update permissions
        /// </summary>
        READ_WRITE = READ|WRITE,
        /// <summary>
        /// Should be allowed to remove data
        /// </summary>
        DELETE = 2 * UPDATE,
        /// <summary>
        /// All basic permissions
        /// </summary>
        ADMIN = READ | INSERT| UPDATE| DELETE,
        /// <summary>
        /// Advanced read permission
        /// </summary>
        ELEVATED_READ = READ | (DELETE * 2),
        /// <summary>
        /// Advanced Insert permission
        /// </summary>
        ELEVATED_INSERT = INSERT | (DELETE * 4),        
        /// <summary>
        /// Advanced update permission
        /// </summary>
        ELEVATED_UPDATE = UPDATE | (DELETE * 8),
        /// <summary>
        /// Advanced insert/update permission
        /// </summary>
        ELEVATED_WRITE = ELEVATED_INSERT | ELEVATED_UPDATE,
        /// <summary>
        /// Advanced read/insert/update permission
        /// </summary>
        ELEVATED_READ_WRITE = ELEVATED_WRITE | ELEVATED_READ,
        /// <summary>
        /// Advanced delete permissions
        /// </summary>
        ELEVATED_DELETE = DELETE | (DELETE * 16),
        /// <summary>
        /// All advanced permissions.
        /// </summary>
        ELEVATED_ADMIN = ELEVATED_READ | ELEVATED_INSERT | ELEVATED_UPDATE | ELEVATED_DELETE,
        /// <summary>
        /// Context dependent
        /// </summary>
        SPECIAL = DELETE * 32,
        /// <summary>
        /// Elevated Admin + Context dependent
        /// </summary>
        SPECIAL_ADMIN = ELEVATED_ADMIN | SPECIAL
           

    }
}
