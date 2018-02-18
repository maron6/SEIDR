using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace SEIDR.Dynamics.Configurations
{
    public class SQLDbTypeHelper
    {
        public static Type ConvertDBType(SqlDbType dbType)
        {
            switch (dbType)
            {
                case SqlDbType.Variant:return typeof(object);
                case SqlDbType.Char:
                case SqlDbType.NChar:
                case SqlDbType.NVarChar:
                case SqlDbType.NText:
                case SqlDbType.Text:
                case SqlDbType.VarChar: return typeof(string);
                case SqlDbType.Xml:return typeof(string); //Noted to be typeof(Xml) in the microsoft mapping, so separate...
                case SqlDbType.Real: return typeof(Single);
                case SqlDbType.UniqueIdentifier: return typeof(Guid);
                case SqlDbType.BigInt: return typeof(Int64);
                case SqlDbType.Int:return typeof(Int32);
                case SqlDbType.SmallInt:return typeof(Int16);
                case SqlDbType.TinyInt:return typeof(byte);
                case SqlDbType.Time:return typeof(TimeSpan);
                case SqlDbType.Date:
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.SmallDateTime: return typeof(DateTime);
                case SqlDbType.DateTimeOffset:return typeof(DateTimeOffset);
                case SqlDbType.Bit: return typeof(bool);                      
                case SqlDbType.Float: return typeof(double);
                case SqlDbType.Decimal:
                case SqlDbType.SmallMoney:
                case SqlDbType.Money: return typeof(decimal);
                case SqlDbType.Binary:
                case SqlDbType.Timestamp:
                case SqlDbType.VarBinary:return typeof(byte[]);
                default:
                    return null;
            }
        }
        
    }
}
