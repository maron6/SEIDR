using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.DataBase.Schema
{
    /// <summary>
    /// Helper class for DB Conversions
    /// </summary>
    public class Schema
    {          
              
        public static bool TryConvertDBType(Type codeType, out SqlDbType Converted)
        {
            Converted = SqlDbType.Variant;
            if (codeType == typeof(byte))
            {
                Converted = SqlDbType.TinyInt;
                return true;
            }
            if(codeType == typeof(char))
            {
                Converted = SqlDbType.Char;
                return true;
            }
            if (codeType == typeof(Guid))
            {
                Converted = SqlDbType.UniqueIdentifier;
                return true;
            }
            if (codeType.In(typeof(uint), typeof(int)))
            {
                Converted = SqlDbType.Int;
                return true;
            }
            if (codeType.In(typeof(short), typeof(ushort)))
            {
                Converted = SqlDbType.SmallInt;
                return true;
            }
            if (codeType == typeof(DateTime))
            {
                Converted = SqlDbType.DateTime;
                return true;
            }
            if (codeType.In(typeof(ulong), typeof(long)))
            {
                Converted = SqlDbType.BigInt;
                return true;
            }
            if (codeType == typeof(bool))
            {
                Converted = SqlDbType.Bit;
                return true;
            }
            if (codeType == typeof(decimal))
            {
                Converted = SqlDbType.Decimal;
                return true;
            }
            if (codeType == typeof(double))
            {
                Converted = SqlDbType.Float;
                return true;
            }
            if (codeType == typeof(float))
            {
                Converted = SqlDbType.Real;
                return true;
            }
            if (codeType == typeof(TimeSpan))
            {
                Converted = SqlDbType.Time;
                return true;
            }
            if (codeType == typeof(byte[])) { Converted = SqlDbType.VarBinary; return true; }
            if (codeType == typeof(DateTimeOffset))
            {
                Converted = SqlDbType.DateTimeOffset;
                return true;
            }
            if (codeType == typeof(string))
            {
                Converted = SqlDbType.VarChar;
                return true;
            }
            if (codeType == typeof(object))
                return true;
            return false;            
        }
        public static Type ConvertDBType(SqlDbType dbType)
        {
            switch (dbType)
            {
                case SqlDbType.Variant: return typeof(object);
                case SqlDbType.Char:
                case SqlDbType.NChar:
                case SqlDbType.NVarChar:
                case SqlDbType.NText:
                case SqlDbType.Text:
                case SqlDbType.VarChar: return typeof(string);
                case SqlDbType.Xml: return typeof(string); //Noted to be typeof(Xml) in the microsoft mapping, so separate...
                case SqlDbType.Real: return typeof(Single);
                case SqlDbType.UniqueIdentifier: return typeof(Guid);
                case SqlDbType.BigInt: return typeof(Int64);
                case SqlDbType.Int: return typeof(Int32);
                case SqlDbType.SmallInt: return typeof(Int16);
                case SqlDbType.TinyInt: return typeof(byte);
                case SqlDbType.Time: return typeof(TimeSpan);
                case SqlDbType.Date:
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.SmallDateTime: return typeof(DateTime);
                case SqlDbType.DateTimeOffset: return typeof(DateTimeOffset);
                case SqlDbType.Bit: return typeof(bool);
                case SqlDbType.Float: return typeof(double);
                case SqlDbType.Decimal:
                case SqlDbType.SmallMoney:
                case SqlDbType.Money: return typeof(decimal);
                case SqlDbType.Binary:
                case SqlDbType.Timestamp:
                case SqlDbType.VarBinary: return typeof(byte[]);
                default:
                    return null;
            }
        }
    }

    //Not sure I want to keep this stuff..

    class TABLE
    {
        public bool IS_HEAP { get
            {
                return pk == null; //Actually clustered index... but eh.
            }
        }
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    _Name = null;
                else
                    _Name = value.Trim().Replace("[", "").Replace("]", "");
            }
        }
        string _Name;
        string _Schema;
        public string Schema
        {
            get { return _Schema; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    _Schema = null;                
                else
                    _Schema = value.Trim().Replace("[", "").Replace("]", "");
            }
        }
        public string QualifiedName
        { get
            {
                if (_Name == null)
                    return null;
                return $"[{Schema ?? "dbo"}].[{ Name }]";
            }
        }
        public PRIMARY_KEY pk { get; set; } = null;
        public IEnumerable<FOREIGN_KEY> fks { get; set; } = null;
        public List<COLUMN> cols { get; private set; } = new List<COLUMN>();
        public static TABLE FromObject(object owner, string Name = null, string Schema = null)
        {
            if (owner == null)
                return null;
            TABLE t = new TABLE();
            Type ot = owner.GetType();
            if (string.IsNullOrWhiteSpace(Name))
                t.Name = ot.Name;
            else
                t.Name = Name.Trim();
            if (string.IsNullOrWhiteSpace(Schema))
                t.Schema = null;
            else
                t.Schema = Schema.Trim();

            t.cols = new List<COLUMN>();
            ot.GetProperties()
                .Where(p => p.CanRead && p.CanWrite)
                .ForEach(p =>
                {
                    SqlDbType type;
                    if (SEIDR.DataBase.Schema.Schema.TryConvertDBType(p.PropertyType, out type))
                    {
                        t.cols.Add(new COLUMN(p.Name, type));
                        if (p.Name == t.Name + "ID")
                            t.pk = new PRIMARY_KEY(t.Name, p.Name);
                    }
                });            
            return t;
        }
        public string GetCreateTableScript()
        {

            if (string.IsNullOrWhiteSpace(Name))
                return null;
            string x = $@"CREATE TABLE {QualifiedName}(" + Environment.NewLine;
            if (IS_HEAP.Or(pk.SingleCol) && fks.NotExists(fk => fk.KeyCount > 1))
            {
                cols.ForEach(c =>
                {
                    string temp = "[" + c.Name + "] [" + c.DataType.ToString() + "] " + (c.Length > 0 ? "(" + c.Length + ")" : "");
                    temp += c.Nullable ? " NULL " : " NOT NULL ";
                    if (pk.Columns == c.Name)
                        temp += " PRIMARY KEY ";
                    var fk = fks.Where(k => k.Columns == c.Name).FirstOrDefault();
                    if (fk != null)
                        temp += " FOREIGN KEY REFERENCES " + fk.ForeignTable + "(" + fk.Columns + ")";
                    x += temp + ", " + Environment.NewLine;
                });
                x += Environment.NewLine + ")";
                return x;
            }            

            cols.ForEach(c => x = x + $"[{c.Name}] [{c.DataType}] " 
                + (c.Length > 0 ? "(" + c.Length + ")" : "")  
                + (c.Nullable? " NULL" : " NOT NULL")
                + "," + Environment.NewLine);
            x += @")
GO
" + (pk?.ADD_SCRIPT ?? "");
            fks?.ForEach(fk => x += "GO" + Environment.NewLine + fk.GetCreate(QualifiedName));            
            return x;
        }
    }
    class FOREIGN_KEY
    {
        public bool SingleCol { get { return KeyCount == 1; } }
        public int KeyCount { get; private set; }
        public string Columns { get; private set; }
        public string ForeignTable { get; set; }
        public string ForeignColumns { get; set; }
        public FOREIGN_KEY(string UnqualifiedTableOwner, string QualifiedForeignTable, params string[] Columns)
        {
            this.Columns = string.Join(",", Columns);
            KeyCount = Columns.Length;
            this.ForeignTable = ForeignTable;
            ForeignColumns = this.Columns;
        }
        public FOREIGN_KEY(string UnqualifiedTableOwner, string QualifiedForeignTable, string[] myColumns, string[] ForeignColumns)
        {
            this.Columns = string.Join(",", myColumns);
            this.ForeignTable = ForeignTable;
            this.ForeignColumns = string.Join("," , ForeignColumns);         
        }        
        public string GetCreate(string QualifiedTable)
        {
            return "ALTER TABLE " + QualifiedTable + " ADD FOREIGN KEY (" + Columns + ") REFERENCES " + ForeignTable
                + "(" + ForeignColumns + ")";
        }
    }
    class PRIMARY_KEY
    {        
        public bool SingleCol { get { return KeyCount == 1; } }
        public string Columns { get; private set; }
        public int KeyCount { get; private set; }
        public string ADD_SCRIPT { get; private set; }        
        public PRIMARY_KEY(string TableOwner, params string[] Columns)
        {
            this.Columns = string.Join(",", Columns);
            KeyCount = Columns.Length;
            ADD_SCRIPT = "ALTER TABLE " + TableOwner 
                + " ADD CONSTRAINT PK_" + TableOwner.Replace(' ', '_').Replace("[", "").Replace("]", "")
                + " PRIMARY KEY (" + Columns + ")";            
        }        
    }

    class COLUMN
    {
        public string Name { get; set; }
        SqlDbType _DataType;
        public SqlDbType DataType { get { return _DataType; } }
        public byte Length { get; set; }        
        public bool Nullable { get; private set; }
        public COLUMN(string name, Type t, byte length = 0)
        {
            Name = name;
            Schema.TryConvertDBType(t, out _DataType);
            Length = length;
            if (t.IsClass)
                Nullable = true;
            else
                Nullable = false;

            if (length == 0 && DataType.In(SqlDbType.VarChar, SqlDbType.Char))
                Length = 30;
        }
        public COLUMN(string name, SqlDbType dbt, byte length = 0, bool nullable = false)
        {
            Name = name;
            _DataType = dbt;
            Length = length;
            Nullable = nullable;
            if (length == 0 && DataType.In(SqlDbType.VarChar, SqlDbType.Char))
                Length = 30;
        }
    }
}
