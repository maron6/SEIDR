using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.DataBase
{
    public abstract partial class DatabaseObject
    {
        public DatabaseConnection Connection { get; protected set; } = null;
        public DatabaseObject() { Connection = null; }
        public DatabaseObject(DatabaseConnection conn) { Connection = conn; }
    }

}
