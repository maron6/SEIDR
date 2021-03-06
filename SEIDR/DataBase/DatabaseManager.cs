﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Reflection;
using static SEIDR.DataBase.DatabaseManagerExtensions;

namespace SEIDR.DataBase
{
    /// <summary>
    /// Sets up a database manager to handle converting c# objects to SQL parameters.
    /// <para>Can also be used for populating c# objects with results of procedures which could also have had its parameters populated by an object(generic)
    ///</para>
    /// </summary>
    public class DatabaseManager
    {
        #region Default Helper models
        /// <summary>
        /// Returns the default Helper Model for this connection.
        /// </summary>
        /// <param name="includeConnection">If true, sets the SQL connection using this manager's connection</param>
        /// <returns></returns>
        public DatabaseManagerHelperModel GetBasicHelper(bool includeConnection = false)
        {
            DatabaseManagerHelperModel h = new DatabaseManagerHelperModel(_Schema);
            if (includeConnection)
                h.SetConnection(this);
            return h;            
        }
        public DatabaseManagerHelperModel GetBasicHelper(object mapObj, bool includeConnection = false)
        {
            DatabaseManagerHelperModel h = new DatabaseManagerHelperModel
            {
                Schema = _Schema
            };
            h.ParameterMap = mapObj;
            if (includeConnection)
                h.SetConnection(this);
            return h;
        }
        public DatabaseManagerHelperModel GetBasicHelper(object mapObj, 
            string UnqualifiedProcedureName, bool includeConnection = false)
        {
            DatabaseManagerHelperModel h = 
                new DatabaseManagerHelperModel(mapObj, _Schema, UnqualifiedProcedureName);            
            if (includeConnection)
                h.SetConnection(this);
            return h;
        }
        public DatabaseManagerHelperModel GetBasicHelper(Dictionary<string, object> Keys,
            string UnqualifiedProcedureName, bool includeConnection = false)
        {
            DatabaseManagerHelperModel h = 
                new DatabaseManagerHelperModel(_Schema,UnqualifiedProcedureName,Keys);            
            if (includeConnection)
                h.SetConnection(this);
            return h;
        }
        public DatabaseManagerHelperModel GetBasicHelper(Dictionary<string, object> Keys, bool includeConnection = false)
        {
            var h = new DatabaseManagerHelperModel(_Schema, null, Keys);
            if (includeConnection)
                h.SetConnection(this);
            return h;
        }
        public DatabaseManagerHelperModel GetBasicHelper(Dictionary<string, object> Keys, string QualifiedProcedure)
        {
            return new DatabaseManagerHelperModel(null, Keys: Keys)
            {
                QualifiedProcedure = QualifiedProcedure
            };                        
        }
        public DatabaseManagerHelperModel GetBasicHelper(Dictionary<string, object> Keys, object mapObj, string procedureName)
        {
            return new DatabaseManagerHelperModel(UnqualifiedProcedureName: procedureName, Keys: Keys)
            {
                ParameterMap = mapObj
                , Schema = _Schema
            };
        }
        #endregion
        void CheckQualified(ref string ProcedureName)
        {
            string[] p = ProcedureName.Split('.');
            if (p.Length == 1)
                ProcedureName = "[" + _Schema + "]." + ProcedureName;            
        }
        /// <summary>
        /// When inferring a procedure call model, allows overriding the default setting for Retry on Deadlock
        /// </summary>
        public bool? DefaultRetryOnDeadlock { get; set; } = null;
        #region Constructor + clone
        /// <summary>
        /// Sets up a database manager to handle converting c# objects to SQL parameters.
        /// <para>Can also be used for populating c# objects with results of procedures which could also have had its parameters populated by an object(generic)
        ///</para>
        /// </summary>
        /// <param name="Connection">The database Connection for storing SQL connection info</param>
        /// <param name="DefaultSchema">Default Schema to be used when not overridden in any of the versions of the procedures/ when using a ConnectionParameterInfo object</param>
        /// <param name="SaveFormat">Format to be used in <see cref="Save{RT}(RT, string, string[])"/>. Object type name replaces {0}</param>
        /// <param name="UpdateFormat">Format to be used in <see cref="Update{RT}(RT, string, string[])"/>. Object type name replaces {0}</param>
        /// <param name="InsertFormat">Format to be used in <see cref="Insert{RT}(RT, string, string[])"/>. Object type name replaces {0}</param>
        /// <param name="DeleteFormat">Format to be used in <see cref="Delete{RT}(RT, string, string[])"/>. Object type name replaces {0}</param>
        /// <param name="SelectRowFormat">Format to be used in <see cref="SelectSingle{RT}(object, string, string[])"/>. Object type name replaces {0}</param>
        /// <param name="SelectListFormat">Format to be used in <see cref="SelectList{RT}(object, string, string[])"/>. Object type name replaces {0}</param>
        public DatabaseManager(DatabaseConnection Connection, string DefaultSchema = "dbo", 
            string SaveFormat = DEFAULT_SAVE, string UpdateFormat = DEFAULT_UPDATE, string InsertFormat = DEFAULT_INSERT,
            string DeleteFormat = DEFAULT_DELETE, string SelectRowFormat = DEFAULT_SELECT_ROW, string SelectListFormat = DEFAULT_SELECT_LIST)
        {
            //_conn = Connection ?? throw new ArgumentNullException("Connection"); //Requires higher version of C# language spec
            if (Connection == null)
                throw new ArgumentNullException(nameof(Connection));
            _conn = Connection;
            //_Schema = DefaultSchema;
            this.DefaultSchema = DefaultSchema;
            _Parameters = new ParamStore();
            this.SaveFormat = SaveFormat ?? DEFAULT_SAVE;
            this.UpdateFormat = UpdateFormat ?? DEFAULT_UPDATE;
            this.DeleteFormat = DeleteFormat ?? DEFAULT_DELETE;
            this.InsertFormat = InsertFormat ?? DEFAULT_INSERT;
            this.SelectRowFormat = SelectRowFormat ?? DEFAULT_SELECT_ROW;
            this.SelectListFormat = SelectListFormat ?? DEFAULT_SELECT_LIST;
        }
        /// <summary>
        /// Overrides the Application Name of the DatabaseConnection when starting new connections
        /// </summary>
        public string ProgramName
        {
            get { return _conn.ApplicationName; }
            set { _conn.ApplicationName = value; }
            /*
            get => _conn.ApplicationName;
            set => _conn.ApplicationName = value;
            */
        }

        /// <summary>
        /// Clones the DatabaseManager, but takes the setting of Rethrow from the parameter.
        /// </summary>
        /// <param name="reThrowException"></param>
        /// <param name="programName">If non-null, overrids the value of the cloned manager's program name</param>
        /// <returns></returns>
        public DatabaseManager Clone(bool reThrowException = true, string programName = null)
        {
            DatabaseManager toClone = this;
            var db = toClone.CloneConnection(programName ?? toClone.ProgramName); //So that we have separate DB Connection objects and do not share program name settings.
            DatabaseManager clone = new DatabaseManager(db, toClone._Schema,
                toClone.SaveFormat, toClone.UpdateFormat, toClone.InsertFormat,
                toClone.DeleteFormat, toClone.SelectRowFormat, toClone.SelectListFormat);
            clone._Parameters = toClone._Parameters;
            clone.DefaultRetryOnDeadlock = toClone.DefaultRetryOnDeadlock;
            clone.RethrowException = reThrowException;
            return clone;
        }
        /// <summary>
        /// Returns a cloned copy of the connection associated with this manager.
        /// </summary>
        /// <param name="programName">If non null, changes the application name. If null, maintains</param>
        /// <returns></returns>
        public DatabaseConnection CloneConnection(string programName = null)
        {
            var conn = DatabaseConnection.FromString(_conn.ConnectionString);
            if (programName != null)
            {
                if (string.IsNullOrWhiteSpace(programName))
                    conn.ApplicationName = null;
                else
                    conn.ApplicationName = programName;
            }
            return conn;
        }
        #endregion
        #region Connection properties
        /// <summary>
        /// Returns a new sql connection using the underlying <see cref="DatabaseConnection"/>
        /// </summary>
        /// <returns></returns>
        public SqlConnection GetConnection()
        {
            return new SqlConnection(_conn.ConnectionString);
        }
        /// <summary>
        /// Returns a copy of the connection string from the underly <see cref="DatabaseConnection"/>
        /// </summary>
        /// <returns></returns>
        public string GetConnectionString()
        {
            return _conn.ConnectionString;
        }

        DatabaseConnection _conn;
        /// <summary>
        /// Connection timeout
        /// </summary>
        public int TimeOut
        {
            get { return _conn.Timeout; }
            set { _conn.Timeout = value; }
        }
        #endregion
        
        #region Default setup properties
        /// <summary>
        /// Changes the underlying connection
        /// </summary>
        /// <param name="newConnection"></param>
        public void ChangeConnection(DatabaseConnection newConnection)
        {
            _conn = newConnection;
        }
        string _Schema = "dbo";
        /// <summary>
        /// Sets the default schema - used when parameters are not passed to methods to override the schema. 
        /// <para>[ and ] are removed, but will be added around the schema when actually performing the call</para>
        /// </summary>
        public string DefaultSchema
        {
            get { return _Schema; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Default Schema must be a non empty string");
                else if (string.IsNullOrWhiteSpace(value.Replace("[", "").Replace
                    ("]", "").Trim()))
                    throw new ArgumentException("Default Schema must be non empty ignoring brackets");
                else
                    _Schema = value.Trim().Replace("[", "").Replace("]", "");
            }
        }
        string CheckSuffix(string Procedure, string suffix)
        {
            if (string.IsNullOrWhiteSpace(suffix))
                return Procedure;
            if (Procedure[Procedure.Length - 1] == ']')
                Procedure.Insert(Procedure.Length - 1, suffix);
            else
                Procedure += suffix;
            return Procedure;
        }
        /// <summary>
        /// Default format for combined "Insert/Update" procedure calls
        /// </summary>
        public const string DEFAULT_SAVE = "usp_{0}_iu";
        /// <summary>
        /// Default format for "Update" procedure call
        /// </summary>
        public const string DEFAULT_UPDATE = "usp_{0}_u";
        /// <summary>
        /// Default format for "Insert" procedure call
        /// </summary>
        public const string DEFAULT_INSERT = "usp_{0}_i";
        /// <summary>
        /// Default format for "Delete" procedure call
        /// </summary>
        public const string DEFAULT_DELETE = "usp_{0}_d";
        /// <summary>
        /// Default format for "Select single" procedure call
        /// </summary>
        public const string DEFAULT_SELECT_ROW = "usp_{0}_ss";
        /// <summary>
        /// Default format for "Select list" procedure call - multiple row result from the first DataTable
        /// </summary>
        public const string DEFAULT_SELECT_LIST = "usp_{0}_sl";
        /// <summary>
        /// Format for a procedure that handles both inserting and updating. Should not include Schema
        /// </summary>
        public string SaveFormat = DEFAULT_SAVE;
        /// <summary>
        /// Format for a procedure that handles inserting a new record. Should not include Schema
        /// </summary>
        public string InsertFormat = DEFAULT_INSERT;
        /// <summary>
        /// Format for a procedure that handles updating existing a single record. Should not include Schema
        /// </summary>
        public string UpdateFormat = DEFAULT_UPDATE;
        /// <summary>
        /// Format for a procedure that handles deleting a single record. Should not include Schema
        /// </summary>
        public string DeleteFormat = DEFAULT_DELETE;
        /// <summary>
        /// Format for a procedure that handles selecting a single row. Should not include Schema
        /// </summary>
        public string SelectRowFormat = DEFAULT_SELECT_ROW;
        /// <summary>
        /// Format for a procedure that handles selecting a list of rows. Should not include Schema
        /// </summary>
        public string SelectListFormat = DEFAULT_SELECT_LIST;
        #endregion
        #region basic methods, minimal parameters
        public void Save<RT>(RT toSave, params string[] ignoreProperties)
        {
            Save<RT>(toSave, Schema:null, ignore:ignoreProperties);
        }
        public void Insert<RT>(RT toInsert, params string[] ignoreProperties) where RT : class, new()
        {
            Insert(toInsert, Schema: null, ignore: ignoreProperties);
        }
        public void Update<RT>(RT obj, params string[] ignoreProperties) where RT : class, new()
        {
            Update(obj, Schema: null, ignore: ignoreProperties);
        }
        public void Delete<RT>(RT obj, params string[] ignoreProperties) where RT : class, new()
        {
            Delete(obj, Schema: null, ignore: ignoreProperties);
        }
        public IEnumerable<RT> SelectList<RT>(object obj, params string[] ignoreProperties) where RT : class, new()
        {
            return SelectList<RT>(obj, Schema: null, ignore: ignoreProperties);
        }
        public RT SelectSingle<RT>(object paramObj, bool RequireSingle, params string[] ignoreProperties) 
            where RT : class, new() => SelectSingle<RT>(paramObj, null, null, ignoreProperties, RequireSingle);
        public RT SelectSingle<RT> (object obj, params string[] ignoreProperties) 
            where RT : class, new() => SelectSingle<RT>(obj, Schema: null, ignore: ignoreProperties);
        #endregion
        const int DEADLOCK_SLEEP = 3500;
        static PropertyInfo DatabaseObjectManagerInfo = typeof(DatabaseObject).GetProperty(nameof(DatabaseObject.Manager));

        /// <summary>
        /// Executes the command associated with the model. Any open transaction will attempt to commit on success.
        /// <para>Builds a sql command using the helper model, and its connection.</para>
        /// <para>If the helper model does not have an open connection, it will be opened first.</para>
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public SqlCommand GetSqlCommand(DatabaseManagerHelperModel i)
        {
            if (i.Connection == null)
                i.SetConnection(_conn);
            SqlConnection c = i.Connection;
            //ToDo: Parameter store for procedure with the database connection?
            string proc = $"[{ (i.Schema ?? _Schema)}].{i.Procedure}";
            
            if (i.IsRolledBack && i.Transaction != null)
                throw new InvalidOperationException("Connection's Transaction has been rolled back - Model has not had Tran cleared yet.");
            if (c.State == ConnectionState.Closed)
                c.Open();
            SqlCommand cmd = new SqlCommand(proc) { CommandType = CommandType.StoredProcedure };                
            cmd.Connection = c;
            cmd.CommandTimeout = i.CommandTimeout ?? _conn.CommandTimeout;
            if (i.Transaction != null)
                cmd.Transaction = i.Transaction;
            FillCommandParameters(cmd, i.ParameterMap, i.Parameters, i.PropertyIgnore.ToArray());
            return cmd;                
        }
        
        /// <summary>
        /// Executes a SQL Command based on the ConnectionProcedureCallModel. 
        /// <para>If there's a transaction and <paramref name="CommitSuccess"/> is true, will commit the transaction on the model.</para>
        /// </summary>
        /// <param name="i"></param>
        /// <param name="CommitSuccess">If command is successful, commit the transaction on the model. If the model doesn't have a transaction, does nothing</param>
        /// <returns></returns>
        public DataSet Execute(DatabaseManagerHelperModel i, bool CommitSuccess = false)
        {
            SqlConnection c = i.Connection;            
            //ToDo: Parameter store for procedure with the database connection?
            string proc = $"[{ (i.Schema ?? _Schema)}].{i.Procedure}";
            DataSet ds = new DataSet();
            if (c != null)
            {
                if (i.IsRolledBack && i.Transaction != null)
                    throw new InvalidOperationException("Connection's Transaction has been rolled back - Model has not had Tran cleared yet.");
                if (c.State == ConnectionState.Closed)
                    c.Open();
                using (SqlCommand cmd = new SqlCommand(proc) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Connection = c;
                    cmd.CommandTimeout = i.CommandTimeout ?? _conn.CommandTimeout;
                    if (i.Transaction != null)
                        cmd.Transaction = i.Transaction;
                    FillCommandParameters(cmd, i.ParameterMap, i.Parameters, i.PropertyIgnore.ToArray());
                    try
                    {
                        SqlDataAdapter sda = new SqlDataAdapter(cmd);
                        sda.Fill(ds);
                        int rv = cmd.GetReturnValue();
                        i.ReturnValue = rv;
                        if (!i.ExpectedReturnValue.HasValue || i.ExpectedReturnValue.Value == rv)
                        {
                            if (i.DoMapUpdate)
                            {
	                            CheckOutputParameters(cmd, i.ParameterMap);
	                            CheckOutputParameters(cmd, i.Parameters);
	                        }
                        }
                        else if (i.Transaction != null)
                        {
                            i.RollbackTran(i.Savepoint); //if Savepoint is empty or null, will be the same as calling without a Savepoint string
                            return null;
                        }
                        if (CommitSuccess && i.Transaction != null)
                            i.CommitTran();                        
                        i.ResetDeadlockLimit();
                    }
                    catch(SqlException ex)
                    {

                        if (ex.Number.In(201, 214, 225, 349, 591, 1023, 2003)
                            || ex.Message.ToUpper().Contains("PARAMETER"))
                            _Parameters.Remove(cmd);
                        if (i.Transaction != null)
                        {
                            try
                            {
                                i.RollbackTran(i.Savepoint);
                            }
                            catch { }
                        }                        
                        else if (ex.Number == 1205 && i.RetryOnDeadlock)
                        {
                            if (i.DeadlockRetryLimit != 0)
                            {
                                if (i.DeadlockRetryLimit > 0)
                                    i.DeadlockRetryLimit--;

                                System.Threading.Thread.Sleep(DEADLOCK_SLEEP);
                                return Execute(i, CommitSuccess);
                            }
                        }
                        if (i.RethrowException ?? RethrowException)
                            throw;
                    }
                }
            }
            else
            {
                using (SqlCommand cmd = new SqlCommand(proc) { CommandType = CommandType.StoredProcedure })
                using (c = new SqlConnection(_conn.ConnectionString))
                {
                    try
                    {
                        c.Open();
                        cmd.Connection = c;
                        cmd.CommandTimeout = i.CommandTimeout ?? _conn.CommandTimeout;
                        FillCommandParameters(cmd, i.ParameterMap, i.Parameters, i.PropertyIgnore.ToArray());
                        SqlDataAdapter sda = new SqlDataAdapter(cmd);
                   
                        sda.Fill(ds);
                        int rv = cmd.GetReturnValue();
                        i.ReturnValue = rv;
                        if (!i.ExpectedReturnValue.HasValue || i.ExpectedReturnValue.Value == rv)
                        {
                            if (i.DoMapUpdate)
                            {
	                            CheckOutputParameters(cmd, i.ParameterMap);
	                            CheckOutputParameters(cmd, i.Parameters);
	                        }
                        }
                        i.ResetDeadlockLimit();
                    }
                    catch(SqlException ex)
                    {
                        if (ex.Number.In(201, 214, 225, 349, 591, 1023, 2003)
                            || ex.Message.ToUpper().Contains("PARAMETER"))
                            _Parameters.Remove(cmd);
                        if (ex.Number == 1205 && i.RetryOnDeadlock)
                        {
                            if (i.DeadlockRetryLimit != 0)
                            {
                                if (i.DeadlockRetryLimit > 0)
                                    i.DeadlockRetryLimit--;

                                System.Threading.Thread.Sleep(DEADLOCK_SLEEP);
                                return Execute(i, CommitSuccess);
                            }
                        }

                        if (i.RethrowException ?? RethrowException)
                            throw;
                    }
                    finally
                    {
                        if(c.State == ConnectionState.Open)
                            c.Close();
                    }                    
                }
            }
            return ds;            
        }
        /// <summary>
        /// Executes the stored procedure. 
        /// </summary>
        /// <param name="QualifiedProcedureName"></param>
        /// <param name="mapObj">Object to use for populating parameters from properties</param>
        /// <param name="updateMap"></param>
        /// <returns></returns>
        public DataSet Execute (string QualifiedProcedureName, object mapObj = null, bool updateMap = true)
        {
            CheckQualified( ref QualifiedProcedureName);
            DataSet ds = new DataSet();
            using (SqlCommand cmd = new SqlCommand(QualifiedProcedureName) { CommandType = CommandType.StoredProcedure })            
            using (SqlConnection c = new SqlConnection(_conn.ConnectionString))
            {
                try
                {
                    c.Open();
                    cmd.Connection = c;
                    cmd.CommandTimeout = _conn.CommandTimeout;
                    if (mapObj != null)
                        FillCommandParameters(cmd, mapObj, null, null);
                    SqlDataAdapter sda = new SqlDataAdapter(cmd);
                
                    sda.Fill(ds);
                    int rv = cmd.GetReturnValue();
                    if(updateMap)
	                    CheckOutputParameters(cmd, mapObj);
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 1205 && (DefaultRetryOnDeadlock ?? DatabaseManagerHelperModel.DefaultRetryOnDeadlock))
                    {
                        System.Threading.Thread.Sleep(DEADLOCK_SLEEP);
                        return Execute(QualifiedProcedureName, mapObj);
                    }
                    if (ex.Number.In(201, 214, 225, 349, 591, 1023, 2003)
                        || ex.Message.ToUpper().Contains("PARAMETER"))
                        _Parameters.Remove(cmd);
                    if (RethrowException)
                        throw;
                }
            }                            
            return ds;            
        }
        /// <summary>
        /// Executes a SQL Command based on the ConnectionProcedureCallModel, with no result set.
        /// <para>If there's a transaction and <paramref name="CommitSuccess"/> is true, will commit the transaction on the model.</para>
        /// </summary>
        /// <param name="i"></param>
        /// <param name="CommitSuccess">If command is successful, commit the transaction from the model. If the model doesn't have a transaction, does nothing</param>
        /// <returns>Affected RowCount</returns>
        public int ExecuteNonQuery(DatabaseManagerHelperModel i, bool CommitSuccess = false)
        {
            int rc = 0;
            SqlConnection c = i.Connection;
            //ToDo: Parameter store for procedure with the database connection?
            string proc = $"[{ (i.Schema ?? _Schema)}].{i.Procedure}";            
            if(c != null)
            {
                if (i.IsRolledBack && i.Transaction != null)
                    throw new InvalidOperationException("Connection's Transaction has been rolled back - Model has not had Tran cleared yet.");
                if (c.State == ConnectionState.Closed)
                    c.Open();
                using(SqlCommand cmd = new SqlCommand(proc) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Connection = c;
                    cmd.CommandTimeout = i.CommandTimeout ?? _conn.CommandTimeout;
                    if (i.Transaction != null)
                        cmd.Transaction = i.Transaction;
                    FillCommandParameters(cmd, i.ParameterMap, i.Parameters, i.PropertyIgnore.ToArray());
                    try
                    {
                        rc = cmd.ExecuteNonQuery();
                        int rv = cmd.GetReturnValue();
                        i.ReturnValue = rv;
                        if (!i.ExpectedReturnValue.HasValue || i.ExpectedReturnValue.Value == rv)
                        {
                            if (i.DoMapUpdate)
                            {
	                            CheckOutputParameters(cmd, i.ParameterMap);
	                            CheckOutputParameters(cmd, i.Parameters);
	                        }
                        }
                        else if (i.Transaction != null)
                        {
                            i.RollbackTran(i.Savepoint); //if Savepoint is null or empty, will be the same as calling without a Savepoint parameter
                            i.ResetDeadlockLimit();
                            return rc;
                        }
                        if (CommitSuccess && i.Transaction != null)
                            i.CommitTran();
                        i.ResetDeadlockLimit();
                    }
                    catch(SqlException ex)
                    {
                        if (ex.Number.In(201, 214, 225, 349, 591, 1023, 2003)
                            || ex.Message.ToUpper().Contains("PARAMETER"))
                            _Parameters.Remove(cmd);
                        if (i.Transaction != null)
                        {
                            try
                            {
                                i.RollbackTran(i.Savepoint);
                            }
                            catch { }
                        }
                        else if (i.RetryOnDeadlock && ex.Number == 1205)
                        {
                            if (i.DeadlockRetryLimit != 0)
                            {
                                if(i.HasDeadlockLimit)
                                    i.DeadlockRetryLimit--;

                                System.Threading.Thread.Sleep(DEADLOCK_SLEEP);
                                return ExecuteNonQuery(i, CommitSuccess);
                            }
                        }                                                
                        if(i.RethrowException ?? RethrowException)
                            throw;                        
                    }
                }
                return rc;
            }
            using (SqlCommand cmd = new SqlCommand(proc) { CommandType = CommandType.StoredProcedure })                            
            using (c = new SqlConnection(_conn.ConnectionString))
            {
                try
                {
                    c.Open();
                    cmd.Connection = c;
                    cmd.CommandTimeout = i.CommandTimeout ?? _conn.CommandTimeout;
                    FillCommandParameters(cmd, i.ParameterMap, i.Parameters, i.PropertyIgnore.ToArray());
                
                    rc = cmd.ExecuteNonQuery();
                    int rv = cmd.GetReturnValue();
                    i.ReturnValue = rv;
                    if (!i.ExpectedReturnValue.HasValue || i.ExpectedReturnValue.Value == rv)
                    {
                        if (i.DoMapUpdate)
                        {
	                        CheckOutputParameters(cmd, i.ParameterMap);
	                        CheckOutputParameters(cmd, i.Parameters);
	                    }
                    }
                    i.ResetDeadlockLimit();
                }
                catch(SqlException ex)
                {
                    if(ex.Number == 1205 && i.RetryOnDeadlock)
                    {
                        if (i.DeadlockRetryLimit != 0)
                        {
                            if (i.HasDeadlockLimit)
                                i.DeadlockRetryLimit--;

                            System.Threading.Thread.Sleep(DEADLOCK_SLEEP);
                            return ExecuteNonQuery(i, CommitSuccess);
                        }
                    }
                    if (ex.Number.In(201, 214, 225, 349, 591, 1023, 2003)
                        || ex.Message.ToUpper().Contains("PARAMETER"))
                        _Parameters.Remove(cmd);
                    if (i.RethrowException ?? RethrowException)
                        throw;
                }
                //finally
                //{
                //    c.Close(); //optional, connection is in a "USING" block
                //}                
            }
            return rc;
        }
        /// <summary>
        /// Executes the non query specified by the Qualified procedure name (Schema + Procedure Name)
        /// </summary>
        /// <param name="QualifiedProcedureName"></param>
        /// <param name="mapObj"></param>
        /// <param name="RetryDeadlock"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(string QualifiedProcedureName, object mapObj = null, bool? RetryDeadlock = null)
        {
            //=> ExecuteNonQuery(QualifiedProcedureName, out int rc, mapObj, RetryDeadlock); //higher c# version...
            int rc;
            return ExecuteNonQuery(QualifiedProcedureName, out rc, mapObj, RetryDeadlock);
        }
                
        /// <summary>
        /// Executes the fully Qualified procedure.
        /// </summary>
        /// <param name="QualifiedProcedureName">Procedure name. If not actually fully qualified, use the default schema.</param>
        /// <param name="ReturnCode">The SQL Return code from calling the stored procedure</param>
        /// <param name="mapObj"></param>
        /// <param name="RetryDeadlock">Priority for determining whether to retry on deadlock</param>
        /// <param name="updateMap">Indicates whether the map object should have its properties updated</param>
        /// <returns>RowCount from ExecuteNonQuery</returns>
        public int ExecuteNonQuery(string QualifiedProcedureName, out int ReturnCode, object mapObj = null, bool? RetryDeadlock = null, bool updateMap = true)
        {
            CheckQualified(ref QualifiedProcedureName);
            int rv = 0;
            ReturnCode = -1;
            using (SqlCommand cmd = new SqlCommand(QualifiedProcedureName) { CommandType = CommandType.StoredProcedure })           
            using (SqlConnection c = new SqlConnection(_conn.ConnectionString))
            {
                try
                {
                    c.Open();
                    cmd.Connection = c;
                    cmd.CommandTimeout = _conn.CommandTimeout;
                    if (mapObj != null)
                    FillCommandParameters(cmd, mapObj, null, null);
                
                    rv = cmd.ExecuteNonQuery();
                    ReturnCode = cmd.GetReturnValue();                    
                }
                catch(SqlException ex)
                {
                    if (ex.Number == 1205 && (RetryDeadlock ?? DefaultRetryOnDeadlock ?? DatabaseManagerHelperModel.DefaultRetryOnDeadlock))
                    {
                        System.Threading.Thread.Sleep(DEADLOCK_SLEEP);
                        return ExecuteNonQuery(QualifiedProcedureName, out ReturnCode, mapObj, RetryDeadlock);                        
                    }
                    if (ex.Message.ToUpper().Contains("PARAMETER"))
                        _Parameters.Remove(cmd);
                    if ( RethrowException)
                        throw;
                }
                if(updateMap)
	                CheckOutputParameters(cmd, mapObj);

            }
            return rv;
        }

        /// <summary>
        /// Rethrows exceptions from execution. Set to false if dealing with procedures where you don't care about catching the exception yourself.
        /// </summary>
        public bool RethrowException { get; set; } = true;
        /// <summary>
        /// Executes the command text as a non query.
        /// </summary>
        /// <param name="CommandText"></param>
        /// <param name="RetryDeadlock">If a value is provided, determines whether to retry on a deadlock.
        /// <para>If no value is provided, will use the DatabaseManager's value, or the default for HelperModels</para></param>
        /// <returns>Affected RowCount from Executing non query</returns>
        public int ExecuteTextNonQuery(string CommandText, bool? RetryDeadlock = null)
        {
            int rv = 0;
            using (SqlConnection c = new SqlConnection(_conn.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(CommandText) { CommandType = CommandType.Text })
            {
                c.Open();
                cmd.Connection = c;
                cmd.CommandTimeout = _conn.CommandTimeout;
                try
                {
                    rv = cmd.ExecuteNonQuery();
                }
                catch(SqlException ex)
                {
                    if(ex.Number == 1205 && (RetryDeadlock ?? DefaultRetryOnDeadlock ?? DatabaseManagerHelperModel.DefaultRetryOnDeadlock))
                    {
                        System.Threading.Thread.Sleep(DEADLOCK_SLEEP);
                        return ExecuteTextNonQuery(CommandText, true);                        
                    }
                    if (RethrowException)
                        throw;                    
                }                
            }
            return rv;
        }
        /// <summary>
        /// Executes the command text as a query.
        /// </summary>
        /// <param name="CommandText"></param>
        /// <param name="RetryDeadlock">If a value is provided, determines whether to retry on a deadlock.
        /// <para>If no value is provided, will use the DatabaseManager's value, or the default for HelperModels</para></param>
        public DataSet ExecuteText(string CommandText, bool? RetryDeadlock = null)
        {
            DataSet ds = new DataSet();
            using (SqlConnection c = new SqlConnection(_conn.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(CommandText) { CommandType = CommandType.Text})
            {
                c.Open();
                cmd.Connection = c;
                cmd.CommandTimeout = _conn.CommandTimeout;
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                try
                {
                    sda.Fill(ds);
                }
                catch(SqlException ex)
                {
                    if (ex.Number == 1205 && (RetryDeadlock ?? DefaultRetryOnDeadlock ?? DatabaseManagerHelperModel.DefaultRetryOnDeadlock))
                    {
                        System.Threading.Thread.Sleep(DEADLOCK_SLEEP);
                        return ExecuteText(CommandText, true);
                    }

                    if (RethrowException)
                        throw;
                    return null;
                }
                return ds;
            }
        }
        /// <summary>
        /// Executes the command text using settings/connection information from the Helper model
        /// </summary>
        /// <param name="i"></param>
        /// <param name="CommandText"></param>
        /// <param name="Commit"></param>
        /// <returns></returns>
        public DataSet ExecuteText(DatabaseManagerHelperModel i, string CommandText, bool Commit = false)
        {
            DataSet ds = new DataSet();
            SqlConnection c = i.Connection;
            if(c == null)
            {
                using (c = new SqlConnection(this._conn.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(CommandText))
                {
                    c.Open();
                    cmd.Connection = c;
                    cmd.CommandTimeout = i.CommandTimeout ?? _conn.CommandTimeout;
                    cmd.CommandType = CommandType.Text;
                    if (_conn.CommandTimeout >= 0)
                        cmd.CommandTimeout = _conn.CommandTimeout;

                    SqlDataAdapter sda = new SqlDataAdapter(cmd);
                    try
                    {
                        sda.Fill(ds);
                    }
                    catch(SqlException ex)
                    {
                        if (ex.Number == 1205 && i.RetryOnDeadlock)
                        {
                            System.Threading.Thread.Sleep(DEADLOCK_SLEEP);
                            return ExecuteText(i, CommandText, Commit);
                        }

                        if (i.RethrowException ?? RethrowException)
                            throw;
                        return null;
                    }
                    i.ReturnValue = cmd.GetReturnValue();
                    c.Close(); //Note: optional (functionality is performed during dispose)
                    return ds;
                }
            }
            if (c.State == ConnectionState.Closed)
                c.Open();
            using(SqlCommand cmd = new SqlCommand(CommandText, c))
            {
                cmd.CommandType = CommandType.Text;
                if (_conn.CommandTimeout >= 0)
                    cmd.CommandTimeout = i.CommandTimeout ?? _conn.CommandTimeout;

                if (i.Transaction != null)
                    cmd.Transaction = i.Transaction;
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                try
                {
                    sda.Fill(ds);
                    if (Commit && i.Transaction != null)
                        i.CommitTran();
                    i.ResetDeadlockLimit();
                }
                catch(SqlException ex)
                {
                    if (i.Transaction != null)
                        try
                        {
                            i.RollbackTran(i.Savepoint);
                        }
                        catch { }
                    else if (ex.Number == 1205 && i.RetryOnDeadlock)
                    {
                        if (i.DeadlockRetryLimit != 0)
                        {
                            if (i.HasDeadlockLimit)
                                i.DeadlockRetryLimit--;

                            System.Threading.Thread.Sleep(DEADLOCK_SLEEP);
                            return ExecuteText(i, CommandText, Commit);
                        }
                    }
                    if (i.RethrowException ?? RethrowException)
                        throw;
                    return null;
                }
                i.ReturnValue = cmd.GetReturnValue();
                return ds;
            }
        }
        #region Basic method set
        /// <summary>
        /// Selects a single record
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="i">Contain information for calling a procedure</param>
        /// <param name="RequireSingleResult">If true, will return null when the the procedure returns more than one result</param>
        /// <param name="CommitSuccess">If true, commitsa if the transaction is open</param>
        /// <returns></returns>        
        public RT SelectSingle<RT>(DatabaseManagerHelperModel i, bool RequireSingleResult = true, bool CommitSuccess = false) where RT:class, new()
        {
            if (i.Procedure == null)
                i.Procedure = string.Format(SelectRowFormat, typeof(RT).Name);
            DataSet ds = Execute(i, CommitSuccess: CommitSuccess);
            if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                return null;
            if (RequireSingleResult && ds.Tables[0].Rows.Count > 1)
                return null;
            RT ex = ds.ToContentRecord<RT>(0, 0);
            if (typeof(RT).IsSubclassOf(typeof(DatabaseObject)))
            {
                DatabaseObjectManagerInfo.SetValue(ex, this); // _conn);
            }
            return ex;
        }

        /// <summary>
        /// Call a procedure to select a single data record which matches to the return type {_}.
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="paramObj"></param>
        /// <param name="suffix">String value to append at the end of the <see cref="SelectRowFormat"/> for the particular procedure we want to run, to allow a more context specific version call.</param>
        /// <param name="Schema">Schema of procedure. If null, uses the <see cref="DefaultSchema"/> </param>
        /// <param name="ignore"></param>
        /// <param name="RequireSingle"></param>
        /// <returns></returns>
        public RT SelectSingle<RT>(object paramObj = null, string suffix = null, string Schema = null, string[] ignore = null, bool RequireSingle = true)where RT: class, new()
        {
            string proc = (Schema ?? _Schema) + "." + CheckSuffix(string.Format(SelectRowFormat, typeof(RT).Name), suffix);
            using (var h = GetBasicHelper())
            {
                h.QualifiedProcedure = proc;
                h.ParameterMap = paramObj;
                if(ignore != null)
                    h.SetPropertyIgnore(ignore);
                return SelectSingle<RT>(h, RequireSingle, true);
            }
        }
        public List<RT> SelectList<RT>(object paramObj = null, string suffix = null, string Schema = null, string[] ignore = null)
        {
            var m = new DatabaseManagerHelperModel
            {
                ParameterMap = paramObj,
                Schema = Schema,
                Procedure = CheckSuffix(string.Format(SelectListFormat, typeof(RT).Name), suffix)
            };
            m.SetPropertyIgnore(ignore);
            return SelectList<RT>(m);
        }
        /// <summary>
        /// Select a list of <typeparamref name="RT"/> records, based on the information in the helper model.
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="i"></param>
        /// <returns></returns>
        public List<RT> SelectList<RT>(DatabaseManagerHelperModel i) where RT : class, new()
        {
            if (i.Procedure == null)
            {
                i.Procedure = string.Format(SelectListFormat, typeof(RT).Name);                
            }
            DataSet ds = Execute(i);
            if (ds.Tables.Count == 0)
                return null;
            Type t = typeof(RT);
            List<RT> temp = ds.Tables[0].ToContentList<RT>();
            if (t.IsSubclassOf(typeof(DatabaseObject)))
            {
                //If it's a database object, have it's manager default to the one that created it. (this)
                temp.ForEach(r =>
                {
                    DatabaseObjectManagerInfo.SetValue(r, this);// _conn);
                });
            }
            return temp;
        }
        /// <summary>
        /// Calls the update procedure (name found using Schema attached to the UpdateFormat)
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="paramObj">Object with properties corresponding to the parameters used for updating a Database Object</param>
        /// <param name="suffix"></param>
        /// <param name="Schema">Allow overriding the Manager's main schema</param>
        /// <param name="ignore">Ignore properties from object - if exists as a parameter, will try to use default parameter value</param>
        public void Update<RT>(RT paramObj, string suffix = null, string Schema = null, string[] ignore = null)
        {
            var m = new DatabaseManagerHelperModel
            {
                ParameterMap = paramObj,
                Schema = Schema,
                Procedure = CheckSuffix( string.Format(UpdateFormat, typeof(RT).Name), suffix)
            };
            m.SetPropertyIgnore(ignore);
            Execute(m);            
        }
        public void Save<RT>(RT paramObj, string suffix = null, string Schema = null, string[] ignore = null) 
        {

            var m = new DatabaseManagerHelperModel
            {
                ParameterMap = paramObj,
                Schema = Schema,
                Procedure = CheckSuffix(string.Format(SaveFormat, typeof(RT).Name), suffix),
            };
            m.SetPropertyIgnore(ignore);
            Execute(m);
        }
        /// <summary>
        /// Insert procedure - found by using the type name, formatted using the InsertFormat variable
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="paramObj"></param>
        /// <param name="suffix">Suffix to tack onto the end of the procedure name (E.g. '_Register' for RT=Batch to get usp_Batch_i_Register)</param>
        /// <param name="Schema"></param>
        /// <param name="ignore"></param>
        public void Insert<RT>(RT paramObj, string suffix = null, string Schema = null, string[] ignore = null)
        {
            var m = new DatabaseManagerHelperModel
            {
                ParameterMap = paramObj,
                Schema = Schema,
                Procedure = CheckSuffix(string.Format(InsertFormat,typeof(RT).Name), suffix)
            };
            m.SetPropertyIgnore(ignore);
            Execute(m);
        }
        /// <summary>
        /// Calls a procedure whose name is determined by combining <see cref="DeleteFormat"/> with the name of type <typeparamref name="RT"/>.
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="paramObj"></param>
        /// <param name="suffix"></param>
        /// <param name="Schema"></param>
        /// <param name="ignore"></param>
        public void Delete<RT>(RT paramObj, string suffix = null, string Schema = null, string[] ignore = null ) where RT: class, new()
        {

            var m = new DatabaseManagerHelperModel
            {
                ParameterMap = paramObj,
                Schema = Schema,
                Procedure = CheckSuffix(string.Format(DeleteFormat, typeof(RT).Name), suffix)
            };
            m.SetPropertyIgnore(ignore);
            Execute(m);
        }
        #endregion
        /// <summary>
        /// Select everything from the first row of the table or view, filtered by Key + value
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="value">Value to filter on. Null is not allowed as a filter.</param>
        /// <param name="TableOrView">Name of a table or view to select from on key = value.
        /// <para>If there is no '.', the default schema for the database manager will be used.</para>
        /// </param>
        /// <param name="Page">Optional paging for view/Table. Only used if PageSize > 0. Option for SQL 2012 and later</param>
        /// <param name="PageSize">Optional size of pages for view/table. Only used if Page >= 0</param>
        /// 
        /// <returns></returns>
        public DataTable SelectWithKey(string Key, object value, string TableOrView, int Page = -1, int PageSize = -1)
        {
            if (value == null || value == DBNull.Value)
                throw new ArgumentNullException(nameof(value));
            if (string.IsNullOrWhiteSpace(Key))
                throw new ArgumentException( "Key must be non empty string", nameof(Key));
            if (string.IsNullOrWhiteSpace(TableOrView))
                throw new ArgumentException("Table or view must be non empty string", nameof(TableOrView));

            DataTable dt = new DataTable();
            Key = Key.Replace("[", "").Replace("]", "");
            if (Key.StartsWith("@"))
                Key = Key.Substring(1);
            const string SELECTOR = "SELECT * FROM {0} WHERE [{1}] = @KEY";
            const string PAGER = "  ORDER BY [{1}] OFFSET ({2} * {3}) ROWS FETCH NEXT {3} ROWS";
            if (TableOrView.IndexOf('.') < 0)
                TableOrView = $"[{_Schema}].[{TableOrView.Replace("[", "").Replace("]", "")}]";

            using (var conn = new SqlConnection(_conn.ConnectionString))
            {
                conn.Open();
                var cmd = new SqlCommand();
                StringBuilder sb = new StringBuilder(SELECTOR);
                if (Page >= 0 && PageSize > 0)
                    sb.Append(PAGER);
                cmd.CommandText = string.Format(sb.ToString(), TableOrView, Key, Page, PageSize);
                cmd.Connection = conn;
                if (_conn.CommandTimeout >= 0)
                    cmd.CommandTimeout = _conn.CommandTimeout;

                cmd.Parameters.AddWithValue("@KEY", value);
                var sda = new SqlDataAdapter(cmd);
                sda.Fill(dt);
                conn.Close();
            }
            return dt;
            //if (dt.Rows.Count == 0)
            //{
            //    return null;
            //}
            //return dt.Rows[0];
        }
        /// <summary>
        /// Gets the first row from selecting by the Key, or null if there are no rows
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="value"></param>
        /// <param name="TableOrView"></param>
        /// <returns></returns>
        public DataRow SelectRowWithKey(string Key, object value, string TableOrView)
            => SelectWithKey(Key, value, TableOrView).GetFirstRowOrNull();
        #region Parameter/mapping management
        private void FillCommandParameters(SqlCommand cmd, object paramObj, Dictionary<string, object> Keys, string[] ignore)
        {
            lock (((System.Collections.ICollection)_PopulateParameter).SyncRoot)
            {
                FillCommandParameters(cmd, paramObj, Keys, ignore, _Parameters, ref _PopulateParameter);
            }
        }

        ParamStore _Parameters;
        Dictionary<string, Action<SqlParameterCollection, object, Dictionary<string, object>, string[]>> _PopulateParameter = new Dictionary<string, Action<SqlParameterCollection, object, Dictionary<string, object>, string[]>>();
        private static void FillCommandParameters(SqlCommand cmd, object paramObj, Dictionary<string, object> extraKeys, string[] ignore, ParamStore ParameterStore, ref Dictionary<string, Action<SqlParameterCollection, object, Dictionary<string, object>, string[]>> Methods)
        {
            
            if (extraKeys == null)
                extraKeys = new Dictionary<string, object>();
            if (paramObj == null && extraKeys.Count > 0)
            {
                paramObj = new { };
                ignore = new string[0];
            }
            else if (paramObj == null)
                return;            
            if(paramObj is Doc.IDataRecord)
            {
                Doc.IDataRecord r = paramObj as Doc.IDataRecord;
                foreach(var col in r.Columns)
                {
                    extraKeys[col.ColumnName] = r[col];
                }
                paramObj = new { };
            }
            ParameterStore.FillParameterCollection(cmd); 
            if (cmd.Parameters.Count == 0) //No parameters to fill.
                return;
            if (ignore == null)
                ignore = new string[0];
            Type paramObjType = paramObj.GetType();
            Action<SqlParameterCollection, object, Dictionary<string, object>, string[]> m;
            string MKey = cmd.CommandText + "_" + paramObjType.FullName;
            if (!Methods.TryGetValue(MKey, out m))
            {

                Dictionary<string, MethodInfo> propDictLambda = paramObjType.GetGetters();
                m = ((SqlParameterCollection col, object o, Dictionary<string, object> e, string[] i) =>
                {
                    var lambda2 = propDictLambda
                        .Where(l => !i.Contains(l.Key)); //remove cases where it's in the ignore list
                    lambda2.ForEach(l =>
                    {
                        if (o == null)
                            e[l.Key] = DBNull.Value;
                        else
                        {
                            object temp = l.Value.Invoke(o, null) ?? DBNull.Value;
                            if (temp is Doc.DataItem)
                                temp = ((Doc.DataItem)temp).Value;
                            e[l.Key] = temp;
                        }
                    });
                    foreach (SqlParameter p in col)
                    {
                        if (p.Direction.In(ParameterDirection.ReturnValue, ParameterDirection.Output))
                            continue; //Skip parameters that are output only
                        string k = p.ParameterName.Substring(1);
                        if (!e.ContainsKey(k))
                            p.Value = null; //Not in dictionary - default parameter value
                        else
                        {
                            object temp = e[k] ?? DBNull.Value;
                            if (temp is Doc.DataItem)
                                temp = ((Doc.DataItem)temp).Value;
                            p.Value = temp;
                        }
                    }
                });
                Methods[MKey] = m;
            }
            else
            {
                if (Methods.Keys.Count > 500)
                {
                    Methods.Clear();
                    Methods[MKey] = m;
                }
            }
            try
            {
                m.Invoke(cmd.Parameters, paramObj, extraKeys, ignore);
            }
            catch
            {
                if(Methods.ContainsKey(MKey))
                    Methods.Remove(MKey); //Shouldn't happen unless the Command parameters are off somehow or something..
                Dictionary<string, MethodInfo> propDict = paramObjType.GetGetters();

                foreach (SqlParameter param in cmd.Parameters)
                {
                    if (param.Direction.In(ParameterDirection.Output, ParameterDirection.ReturnValue))
                        continue;
                    string Key = param.ParameterName.Substring(1); //Remove '@'
                    MethodInfo mi;
                    if (!ignore.Contains(Key) && propDict.TryGetValue(Key, out mi))
                    {
                        object temp = mi.Invoke(paramObj, null) ?? DBNull.Value;
                        if (temp is Doc.DataItem)
                            temp = ((Doc.DataItem)temp).Value;
                        param.Value = temp;
                    }
                    else if (extraKeys.ContainsKey(Key))
                    {
                        object temp = extraKeys[Key] ?? DBNull.Value;
                        if (temp is Doc.DataItem)
                            temp = ((Doc.DataItem)temp).Value;
                        param.Value = temp;
                    }
                    else
                        param.Value = null; //Default
                }
            }
        }
        private static void CheckOutputParameters(SqlCommand cmd, object paramObj)
        {
            if (paramObj == null)
                return;
            List<SqlParameter> pList = new List<SqlParameter>();
            foreach(SqlParameter param in cmd.Parameters)
            {
                if((param.Direction & CheckOutput) != 0)
                {
                    pList.Add(param);
                }
            }
            if (pList.Count == 0)
                return;
            paramObj.GetType()
                .GetProperties()
                .Where(p => p.CanWrite && !p.MappingIgnored(true) )
                .Join(pList,
                    p => "@" + p.GetMappedName(), // p=> "@" + p.Name,
                    p => p.ParameterName,
                    (p, sqlP) => new { Property = p, Value = sqlP.Value })
                .ForEach(a => a.Property.SetValue(paramObj, a.Value));                    
        }
        private static void CheckOutputParameters(SqlCommand cmd, Dictionary<string, object> keyDict)
        {
            if (keyDict == null || keyDict.Keys.Count == 0)
                return;
            List<SqlParameter> pList = new List<SqlParameter>();
            foreach (SqlParameter param in cmd.Parameters)
            {
                if (!keyDict.ContainsKey(param.ParameterName.Substring(1)))
                    continue;
                if ((param.Direction & CheckOutput) != 0)
                {
                    pList.Add(param);
                }
            }
            if (pList.Count == 0)
                return;
            pList.ForEach(p => keyDict[p.ParameterName.Substring(1)] = p.Value);
        }
        #endregion
    }
}
