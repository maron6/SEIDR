using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.DataBase
{
    /// <summary>
    /// Basic base class for classes that will use an instance of <see cref="DatabaseConnection"/>.
    /// <para>Inherit generically with your type as the generic to get basic method functionality as well.</para>
    /// </summary>
    public abstract class DatabaseObject
    {        
        /// <summary>
        /// Used for 
        /// </summary>
        public DatabaseManager Manager { get; protected set; }        
        /// <summary>
        /// Base constructor for DatabaseObject.
        /// </summary>
        /// <param name="manager"></param>
        public DatabaseObject(DatabaseManager manager) { Manager = manager; }
        /// <summary>
        /// Basic constructor, to allow constructing from reflection
        /// </summary>
        public DatabaseObject() { }
        /// <summary>
        /// Clones the connection
        /// </summary>
        /// <param name="programName"></param>
        /// <returns></returns>
        public DatabaseConnection CloneConnection(string programName = null)
            => Manager.CloneConnection(programName);        
    }
}
