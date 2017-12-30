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
        public DatabaseConnection Connection { get; protected set; } = null;
        public DatabaseObject() { Connection = null; }
        public DatabaseObject(DatabaseConnection conn) { Connection = conn; }


        public DatabaseManager GetDefaultManager(string Schema = "dbo")
        {
            return new DatabaseManager(Connection, Schema);
        }
    }
}
