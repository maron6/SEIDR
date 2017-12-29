using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace SEIDR.DataBase
{
    public class DatabaseManagerHelperModel : IDisposable
    {
        public static bool DefaultRetryOnDeadlock { get; set; } = false;
        public bool RetryOnDeadlock { get; set; } = DefaultRetryOnDeadlock;
        /// <summary>
        /// Provide a value to override the value from DatabaseManager
        /// </summary>
        public bool? RethrowException { get; set; } = null;
        /// <summary>
        /// Allow maintaining a SQL Connection across commands until the HelperModel is disposed
        /// </summary>
        public SqlConnection connection { get; private set; } = null;
        public SqlTransaction Transaction { get; private set; } = null;
        public int ReturnValue { get; set; } = 0;
        /// <summary>
        /// If a value is set - output parameters will not be updated if ReturnValue doesn't match.
        /// <para>If a transaction is open and the return value doesn't match, it will also rollback automatically</para>
        /// <para>Otherwise, does nothing, and will not cause any exception to be thrown.</para>
        /// </summary>
        public int? ExpectedReturnValue { get; set; } = null;
        #region Constructors
        public DatabaseManagerHelperModel()
        {
            RetryOnDeadlock = DefaultRetryOnDeadlock;
            ParameterMap = null;
            Procedure = null;
            _Schema = null;
            Parameters = new Dictionary<string, object>();
            _PropertyIgnore = new List<string>();
        }
        public DatabaseManagerHelperModel(string UnqualifiedProcedure) : this()
        {
            Procedure = UnqualifiedProcedure;
        }
        public DatabaseManagerHelperModel(string schema, string Procedure) : this()
        {
            this.Schema = schema;
            this.Procedure = Procedure;
        }
        public DatabaseManagerHelperModel(string schema, string Procedure, Dictionary<string, object> Keys)
            :this(schema, Procedure)
        {
            Parameters = Keys;
        }
        public DatabaseManagerHelperModel(string UnqualifiedProcedureName, Dictionary<string, object> Keys):this()
        {
            Procedure = UnqualifiedProcedureName;
            Parameters = Keys;
        }
        public DatabaseManagerHelperModel(string UnqualifiedProcedure, object mapObj):this()
        {
            Procedure = UnqualifiedProcedure;
            ParameterMap = mapObj;
        }        
        public DatabaseManagerHelperModel(object mapObj, string Procedure, string Schema, 
            Dictionary<string, object> Keys, params string[] Ignore)
        {
            RetryOnDeadlock = DefaultRetryOnDeadlock;
            ParameterMap = mapObj;
            this.Procedure = Procedure;
            this.Schema = Schema;
            this.Parameters = Keys ?? new Dictionary<string, object>();
            _PropertyIgnore = new List<string>(Ignore);
        }
        public DatabaseManagerHelperModel(string Procedure, string Schema, object mapObj, string[] ignore)
            :this(mapObj, Procedure, Schema, null, Ignore: ignore) { }
        public DatabaseManagerHelperModel(object mapObj, string Schema, string[] Ignore)
            :this(mapObj, null, Schema, null, Ignore) { }
        public DatabaseManagerHelperModel(object mapObj, string QualifiedProcedure)            
        {
            ParameterMap = mapObj;
            this.QualifiedProcedure = QualifiedProcedure;            
            this.Parameters = new Dictionary<string, object>();
            _PropertyIgnore = new List<string>();
        }
        public DatabaseManagerHelperModel(object mapObj, string Schema, string Procedure)
            :this(mapObj, Schema + "." + Procedure){}
        public DatabaseManagerHelperModel(object mapObj, string Procedure, string Schema,
            Dictionary<string, object> Keys)
            :this(mapObj, Procedure, Schema, Keys, new string[] { }) { }
        #endregion

        #region Ignored Properties/Keys
        /// <summary>
        /// Resets the list of Properties to ignore to be the provided string array
        /// </summary>
        /// <param name="PropertyList"></param>
        public void SetPropertyIgnore(params string[] PropertyList)
        {
            _PropertyIgnore = new List<string>(PropertyList);
        }
        /// <summary>
        /// Removes existing parameter key/value pairs and sets it the provided Dictionary
        /// </summary>
        /// <param name="ParameterKeys">New value for extra parameter list... Null is okay</param>
        public void ResetKeys(Dictionary<string, object> ParameterKeys = null)
        {
            Dictionary<string, object> pk2 = new Dictionary<string, object>();
            if (ParameterKeys != null)
            {
                ParameterKeys.ForEach( kv =>
                {
                    if (kv.Key[0] == '@')
                        pk2.Add(kv.Key.Substring(1), kv.Value);
                    else
                        pk2.Add(kv.Key, kv.Value);
                });
            }
            Parameters = pk2;
        }
        /// <summary>
        /// Converts a DataRow from a table to Parameter Key/values
        /// </summary>
        /// <param name="r"></param>
        public void AddDataRowAsKeys(DataRow r)
        {
            var cols = r.Table.Columns;
            foreach (DataColumn col in cols)
            {
                this[col.ColumnName] = r[col.ColumnName];
            }
        }

        public void SetKey(string Name, object value)
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new ArgumentException("Key is null or white space", "Name");
            if (Name[0] == '@')
                Name = Name.Substring(1);
            Parameters[Name] = value;
        }
        /// <summary>
        /// Calls Dictionary Add on the underlying key dictionary
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="value"></param>
        public void AddKey(string Name, object value)
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new ArgumentException("Key is null or white space", "Name");
            if (Name[0] == '@')
                Name = Name.Substring(1);
            Parameters.Add(Name, value);
        }
        /// <summary>
        /// Removes the key if it exists.
        /// </summary>
        /// <param name="Key"></param>
        public void RemoveKey(string Key)
        {
            if (string.IsNullOrWhiteSpace(Key))
                throw new ArgumentException("Key is null or white space", "Key");
            if (Key[0] == '@')
                Key = Key.Substring(1);
            if (Parameters.ContainsKey(Key))
                Parameters.Remove(Key);
        }

        #endregion
        /// <summary>
        /// Qualified procedure name, containing the schema + Procedure.
        /// <para>If Schema has not been set, dbo will be used.</para>
        /// </summary>
        public string QualifiedProcedure
        {
            get
            {
                return "[" + (_Schema ?? "dbo") + "]." + _Procedure;
            }
            set
            {
                string[] s = value.Split('.');
                if (s.Length != 2)
                    throw new ArgumentException("Invalid procedure Qualification");
                Schema = s[0];
                Procedure = s[1];
            }
        }
        /// <summary>
        /// Contains readable properties that should be mapped to SQL parameters
        /// </summary>
        public object ParameterMap;
        string _Schema = null;
        /// <summary>
        /// Allow overridding the Default Schema from the Database Connection manager. 
        /// <para>If null or empty, the Schema from DatabaseConnectionManager will be used, but dbo will be returned in 'QualifiedProcedure' property, if used separately
        /// </para>
        /// <para>Removes brackets and quotes, due to the way the manager uses it for schema</para>
        /// </summary>
        public string Schema
        {
            get { return _Schema; }
            set
            {                
                if (string.IsNullOrWhiteSpace(value))
                    _Schema = null;
                else
                    _Schema = value.Replace("[", "").Replace("]", "").Replace("\"", "").Trim();
            }
        }

        string _Procedure;
        /// <summary>
        /// Procedure to be called. Quotes will be removed and the string surrounded with brackets.
        /// </summary>
        public string Procedure
        {
            get { return _Procedure; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    _Procedure = null;
                else
                {
                    _Procedure = "[" + value.Replace("[", "").Replace("]", "").Replace("\"", "").Trim() + "]";
                }

            }
        }
        List<string> _PropertyIgnore = new List<string>();
        /// <summary>
        /// List of properties to ignore from the parameterMap (Ignored parameters will use the default value)
        /// </summary>
        public List<string> PropertyIgnore
        {
            get
            {
                return _PropertyIgnore;
            }
        }
        /// <summary>
        /// Add/Get Key-Value pairs for SQL parameters - will override properties from parameter map. 
        /// <para>Any new keys will also be added to PropertyIgnore</para>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object this[string key]
        {
            get
            {
                if (key[0] == '@')
                    key = key.Substring(1);
                object o;
                if (Parameters.TryGetValue(key, out o))
                    return o;
                return null;
            }
            set
            {
                if (key[0] == '@')
                    key = key.Substring(1);
                if (!Parameters.ContainsKey(key))
                {
                    _PropertyIgnore.Add(key);
                    _PropertyIgnore = _PropertyIgnore.Distinct().ToList();
                }
                Parameters[key] = value;
            }
        }
        public Dictionary<string, object> Parameters { get; private set; } = new Dictionary<string, object>();

        public void SetConnection(DatabaseConnection db)
        {
            if (db == null)
                throw new ArgumentNullException("db");            
            Dispose();
            connection = new SqlConnection(db.ConnectionString);
            IsRolledBack = false;
        }
        public void SetConnection(DatabaseManager dm)
        {
            if (dm == null)
                throw new ArgumentNullException("dm");
            Dispose();
            connection = dm.GetConnection();
            IsRolledBack = false;
        }
        /// <summary>
        /// Begins a transaction on the set connection
        /// </summary>
        public void BeginTran()
        {
            if (connection == null)
                throw new InvalidOperationException("Connection not set");
            if (Transaction != null)
                throw new InvalidOperationException("Existing Transaction already exists");
            if (connection.State == ConnectionState.Closed)
                connection.Open();
            Transaction = connection.BeginTransaction();
            IsRolledBack = false;
        }
        /// <summary>
        /// Rolls back the transaction and then disposes it. Require open transaction/connection
        /// </summary>
        public void RollbackTran()
        {
            if (connection == null)
                throw new InvalidOperationException("Connection not set");
            if (connection.State == ConnectionState.Closed)
                throw new InvalidOperationException("Connection is closed.");
            if (Transaction == null)
                throw new InvalidOperationException("No transaction to roll back");            
            Transaction.Rollback();
            Transaction.Dispose();
            Transaction = null;
            IsRolledBack = true;
        }
        /// <summary>
        /// Commits the transaction and then disposes it. Requires open transaction/connection.
        /// </summary>
        public void CommitTran()
        {
            if (connection == null)
                throw new InvalidOperationException("Connection not set");
            if (connection.State == ConnectionState.Closed)
                throw new InvalidOperationException("Connection is closed.");
            if (Transaction == null)
                throw new InvalidOperationException("No transaction to Commit");
            Transaction.Commit();
            Transaction.Dispose();
            Transaction = null;
        }
        
        public bool HasOpenTran { get { return Transaction != null; } }
        public bool IsRolledBack { get; set; } = false;
        public void ClearConnection()
        {
            ClearTran();
            if(connection != null)
            {
                connection.Dispose();
                connection = null;
            }
        }
        /// <summary>
        /// Removes the transaction by calling dispose, if not null. Resets the IsRolledBack variable
        /// </summary>        
        public void ClearTran()
        {
            if (Transaction != null)
            {                
                Transaction.Dispose();
                Transaction = null;
            }
            IsRolledBack = false;
        }
        public void Dispose()
        {
            if (Transaction != null)
                Transaction.Dispose();
            if (connection != null)
                connection.Dispose();
        }
    }
}
