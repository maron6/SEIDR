using System.Xml.Serialization;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace SEIDR.DataBase
{
    /// <summary>
    /// Wrapper class for managing a SQL server connection and running some common queries..
    /// </summary>
    public class DatabaseConnection
    {
        /// <summary>
        /// Construct an object using a connection string.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static DatabaseConnection FromString(string connectionString)
        {
            DatabaseConnection conn = new DatabaseConnection();
            //string[] cs = connectionString.Split(';');
            var cs = connectionString.SplitByQualifier(";", "'", true);
            var dict = (from c in cs
                        let split = c.SplitByQualifier("=", "'")// "{", "}")
                        where split.Count == 2
                        select new { Key = split[0].Trim().ToUpper(), Value = split[1] }).ToDictionary(a => a.Key, a => a.Value);
                       /*
            var dict = (from c in cs
                        let split = c.Split('=')
                        where split.Length == 2
                        select new { Key = split[0].Trim().ToUpper(), Value = split[1] }).ToDictionary(a => a.Key, a => a.Value);
                        */
            if(!dict.TryGetValue("SERVER", out conn._Server) && !dict.TryGetValue("DATA SOURCE", out conn._Server))
                conn._Server = "(localdb)";//local              
            
            if (!dict.TryGetValue("DATABASE", out conn._DefaultCatalog) && !dict.TryGetValue("DEFAULT CATALOG", out conn._DefaultCatalog))
                conn._DefaultCatalog = null;
            dict.TryGetValue("FAILOVER PARTNER", out conn._Failover);
            
            if (!dict.ContainsKey("TIMEOUT") || !int.TryParse(dict["TIMEOUT"], out conn._Timeout))
                conn.Timeout = -1;
            
            if(
                (!dict.TryGetValue("USERNAME", out conn._un) 
                    && !dict.TryGetValue("UID", out conn._un) //short circuiting, will stop once one is successful (causes && to fail)
                    && !dict.TryGetValue("USER ID", out conn._un)
                    )
                || (!dict.TryGetValue("PASSWORD", out conn._pw)
                    && !dict.TryGetValue("PWD", out conn._pw)
                    )
                )
            {
                conn._un = conn._pw = null;
            }
            if (dict.ContainsKey("APPLICATIONINTENT") && dict["APPLICATIONINTENT"] == "readonly")
                conn.ReadOnlyIntent = true;

            dict.TryGetValue("APPLICATION NAME", out conn._AN);
            
            if ( !dict.ContainsKey("COMMAND TIMEOUT") || !int.TryParse(dict["COMMAND TIMEOUT"], out conn.commandTimeout))
                conn.commandTimeout = -1;
            conn.ReformatConnection();
            return conn;
        }
        int _Timeout;
        /// <summary>
        /// If the value is >= 0, will specify the timeout in the connection string.
        /// </summary>
        public int Timeout
        {
            get
            {
                return _Timeout;
            }
            set
            {
                _Timeout = value;
                //ReformatConnection();
                _changed = true;
            }
        }

        
        [XmlIgnore]
        int commandTimeout = -1;
        /// <summary>
        /// If value is >= 0, will set the Command timeout in SQL commands when run by this class's RunCommand. Default is -1 (Does not change the command timeout).
        /// <para>Outisde of the class, returns 30 if the internal value is &lt; 0</para>
        /// </summary>
        public int CommandTimeout {
            get
            {
                if (commandTimeout < 0)
                    return 30;
                return commandTimeout;
            }
            set
            {
                commandTimeout = value;
            }
        }
        string _AN;
        /// <summary>
        /// Application Name for the connection. (E.g., seen when calling sp_who2)
        /// </summary>
        public string ApplicationName
        {
            get { return _AN; }
            set { _AN = value.Replace("'", ""); _changed = true; }
        }
        /// <summary>
        /// Default name for use in the connection manager
        /// </summary>
        [XmlIgnore]
        public const string DEFAULT_NAME = "Default";
        /// <summary>
        /// Constructor that does not set values.
        /// </summary>
        public DatabaseConnection() { _Server = null; DefaultCatalog = null; }
        /// <summary>
        /// The database connected to if one is not specified
        /// </summary>
        [XmlIgnore]
        public const string DEFAULT_DBNAME = "MASTER";
        public DatabaseConnection(string Server) { this.Server = Server; DefaultCatalog = DEFAULT_DBNAME; }
        public DatabaseConnection(string Server, string Catalog) { this.Server = Server; DefaultCatalog = Catalog; }

        bool _readonly = false;
        /// <summary>
        /// Application Intent. Default to false (readwrite). Only modifies the final connection string when true.
        /// <para>https://stackoverflow.com/questions/15347541/what-does-applicationintent-readonly-mean-in-the-connection-string
        /// </para><para>May be useful if there's a readonly replica configured (or planned)</para>
        /// </summary>
        public bool ReadOnlyIntent
        {
            get { return _readonly; }
            set
            {
                if (value != _readonly)
                {
                    _readonly = value;
                    _changed = true;
                }
            }
        }

        string _pw = null;
        string _un = null;
        /// <summary>
        /// Password for non trusted authentication. Must be set outside of constructor
        /// <para>Should probably only use for logins with limited access, e.g. a dev password with basically read only access or something</para>
        /// </summary>
        public string Password
        {
            get { return _pw; }
            set {
                if (value != _pw)
                { 
                    _pw = value;
                    //ReformatConnection();
                    _changed = true;
                }
            }
        }
        /// <summary>
        /// Username for non trusted authentication. Must be set outside of constructor
        /// </summary>
        public string UserName {
            get { return _un; }
            set
            {
                if (value != _un)
                {
                    _un = value;
                    //ReformatConnection();
                    _changed = true;
                }
            }
        }

        /// <summary>
        /// Gets whether or not the connection uses a UserName/Password to log in vs Windows authentication
        /// </summary>
        [XmlIgnore]
        public bool useTrustedConnection { get
            {
                return string.IsNullOrWhiteSpace(UserName + Password);
            } }
        string _Server;
        /// <summary>
        /// Gets or sets the SQL Server to connect to
        /// </summary>
        public string Server
        {
            get
            {
                return _Server ?? @".";
            }
            set
            {                
                if (value != _Server)
                {
                    _Server = value.Replace("'", "");                    
                    //ReformatConnection();
                    _changed = true;
                }
            }
        }
        string _DefaultCatalog;
        /// <summary>
        /// Sets the Default database to connect to. If missing, will return string.Empty
        /// </summary>
        public string DefaultCatalog
        {
            get
            {                                   
                return _DefaultCatalog ??string.Empty;
            }
            set
            {
                if (value != _DefaultCatalog)
                {
                    _DefaultCatalog = value.Replace("'", ""); ;
                    //ReformatConnection();
                    _changed = true;
                }
            }
        }        
        string ServerSegment
        {
            get
            {                
                string d = $"Server='{_Server}';Database='{_DefaultCatalog ?? DEFAULT_DBNAME}';";
                if (!string.IsNullOrEmpty(_Failover))
                    d += $"Failover Partner='{_Failover}';";
                if (!string.IsNullOrEmpty(_AN))
                    d += $"Application Name='{_AN}';";
                if (ReadOnlyIntent)
                    d += ";applicationintent=readonly";
                return d;
            }
        }
        const string ConnectionFormatTrusted = "Trusted_Connection=true;";        
        string TimeoutSegment
        {
            get
            {
                if (_Timeout < 0)
                    return string.Empty;
                return $"Timeout={_Timeout};";
            }
        }
        const string ConnectionFormatLogin = "Trusted_Connection=false;username='{0}';password='{1}';";
        string _ConnectionString;
        string _Failover;
        /// <summary>
        /// Specifies that there is a mirroring database that can be passed to the connection.
        /// </summary>
        public string FailoverPartner
        {
            get
            {
                return _Failover;
            }
            set
            {
                if (_Failover != value)
                {
                    if (value == string.Empty)
                        _Failover = null;
                    else
                        _Failover = value;
                    
                    //ReformatConnection();
                    _changed = true;
                }
            }
        }
        /// <summary>
        /// Returns the connection string based on properties of this  object
        /// </summary>
        [XmlIgnore]
        public string ConnectionString {
            get
            {
                if (!IsValid)
                    return null;
                if (_ConnectionString == null || _changed)
                    ReformatConnection();
                return _ConnectionString;
            }
        }
        bool _changed = true;
        void ReformatConnection()
        {
            if (useTrustedConnection)
                _ConnectionString = ServerSegment + ConnectionFormatTrusted + TimeoutSegment;
            //string.Format(ConnectionFormatTrusted, Server, DefaultCatalog ?? "dbo");
            else
                _ConnectionString = ServerSegment + string.Format(ConnectionFormatLogin, UserName, Password) + TimeoutSegment;
            _ConnectionString = _ConnectionString.Replace("''", "''''");
            _changed = false;
            /*
                _ConnectionString = string.Format(ConnectionFormatLogin,
                    Server,
                    DefaultCatalog,
                    UserName,
                    Password);
                    */
        }
        /// <summary>
        /// Returns whether the set up is valid - Connection string will not be returned if this is false
        /// </summary>
        [XmlIgnore]
        public bool IsValid
        {
            get { return _Server != null; }
        }
        /// <summary>
        /// Run the command with this Connection
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="Dispose">If true, dispose the sql command after getting results</param>
        /// <returns></returns>
        public DataSet RunCommand(SqlCommand cmd, bool Dispose = false)
        {
            DataSet s = new DataSet();
            using(SqlConnection c = new SqlConnection(ConnectionString))
            {
                c.Open();                
                cmd.Connection = c;
                if(commandTimeout >= 0)
                    cmd.CommandTimeout = CommandTimeout;
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                sda.Fill(s);
                c.Close();
            }
            if (Dispose)
                cmd.Dispose();
            return s;
        }        
        /// <summary>
        /// Runs the sql command on the passed sqlconnection. SQLConnection will be closed if it was unopened when passed. 
        /// <para>SQL Command will be disposed if <paramref name="Dispose"/> is true</para>
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="c"></param>
        /// <param name="Dispose"></param>
        /// <returns></returns>
        private DataSet RunCommand(SqlCommand cmd, SqlConnection c, bool Dispose = false)
        {
            bool AlreadyOpen = true;
            DataSet ds = new DataSet();
            if (c.State == ConnectionState.Closed || c.State == ConnectionState.Broken)
            {
                c.Open();
                AlreadyOpen = false;
            }
            cmd.Connection = c;
            if (commandTimeout >= 0)
                cmd.CommandTimeout = commandTimeout;
            SqlDataAdapter sda = new SqlDataAdapter(cmd);
            sda.Fill(ds);

            if (!AlreadyOpen)
                c.Close();
            if (Dispose)            
                cmd.Dispose();            

            sda.Dispose();
            return ds;
        }
    }
}
