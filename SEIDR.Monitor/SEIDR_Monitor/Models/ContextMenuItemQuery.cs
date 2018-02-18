using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Configurations;
using System.Data;
using System.Data.SqlClient;
//using SEIDR.Processing.Data.DBObjects;
using SEIDR.Dynamics.Windows;
using SEIDR.Dynamics.Configurations.DatabaseConfiguration;
using SEIDR.Dynamics.Configurations.ContextMenuConfiguration;
using static SEIDR.WindowMonitor.MonitorConfigurationHelpers.LibraryManagement;
using SEIDR.Dynamics;
using SEIDR.DataBase;

namespace SEIDR.WindowMonitor.Models
{
    public static class ConetextMenuItemHelper
    {
        static Dictionary<string, object> MapDataRowView(DataRowView rv)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            var q = (from DataColumn col in rv.Row.Table.Columns
                     select new { col.ColumnName, Value = rv[col.ColumnName] ?? DBNull.Value }
                     ).OrderBy(a => a.ColumnName);
            foreach(var item in q)
            {
                string h = item.ColumnName;
                if (h.ToLower().StartsWith("hdn"))
                    h = h.Substring(3);
                else if (h.ToLower().StartsWith("dtl_"))
                    h = h.Substring(4);                
                result[h] = item.Value;
            }
            return result;
        }
        public static DataSet RunContext(this ContextMenuConfiguration cmi, DataRowView drv , Database db)
        {
            int x;
            return RunContext(cmi, drv, db, out x);
        }
        public static DataSet RunContext(this ContextMenuConfiguration cmi, DataRowView drv, Database dbc, out int RC)
        {
            DataSet ds = null;
            RC = -1;
            try
            {
                var map = MapDataRowView(drv);
                if(!string.IsNullOrWhiteSpace(cmi.ProcIDParameterName))
                    map[cmi.ProcIDParameterName] = cmi.ProcID;
                var dm = dbc.Manager;
                using (var m = dm.GetBasicHelper(Keys: map, includeConnection: true))
                {
                    m.QualifiedProcedure = cmi.ProcedureCall;
                    m.BeginTran();
                    ds = dm.Execute(m, true);
                    RC = m.ReturnValue;
                    if (!string.IsNullOrWhiteSpace(cmi.Dashboard))
                    {
                        GridDashboardWindow gd = new GridDashboardWindow(m, dm, dbc, cmi);
                        gd.ShowDialog();
                    }
                }                             
            }
            catch(Exception ex)
            {
                Handle(ex, $"Error executing context action {cmi.Key}({cmi.MyScope.GetDescription()})");
            }
            return ds;
        }
        public static DataTable RunContextDataTable(this ContextMenuConfiguration cmi, DataRowView drv, Database db)
        {
            int x;
            return RunContext(cmi, drv, db, out x).GetFirstTableOrNull();
        }        
        public static int RunContextAffectedCount(this ContextMenuConfiguration cmi, DataRowView drv, Database db)
        {
            int rc = -1;
            try
            {
                var map = MapDataRowView(drv);
                if (!string.IsNullOrWhiteSpace(cmi.ProcIDParameterName))
                    map[cmi.ProcIDParameterName] = cmi.ProcID;
                var dm = db.Manager;
                using (var m = dm.GetBasicHelper(Keys: map, includeConnection: true))
                {
                    m.QualifiedProcedure = cmi.ProcedureCall;
                    m.BeginTran();
                    rc = dm.ExecuteNonQuery(m, true);
                    if (!string.IsNullOrWhiteSpace(cmi.Dashboard))
                    {
                        GridDashboardWindow gd = new GridDashboardWindow(m, dm, db, cmi);
                        gd.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                Handle(ex, $"Error executing context action {cmi.Key}({cmi.MyScope.GetDescription()})");
            }
            return rc;
        }
        public static int RunContextRC(this ContextMenuConfiguration cmi, DataRowView drv, Database db)
        {
            int x;
            RunContext(cmi, drv, db, out x);
            return x;
        }
    }
    public class ContextMenuItemQuery :IDisposable
    {
        SqlConnection conn;
        SqlCommand comm;
        ContextMenuConfiguration cmi;
        Database db;
        public ContextMenuItemQuery(ContextMenuConfiguration Cmi, DataRowView drv, Database dbc)
        {
            this.cmi = Cmi;
            db = dbc;
            conn = new SqlConnection(dbc.Connection.ConnectionString);
            conn.Open();
            comm = new SqlCommand(Cmi.ProcedureCall) { CommandType = CommandType.StoredProcedure, Connection = conn };            
            DataColumnCollection dcc = drv.Row.Table.Columns;
            SqlCommandBuilder.DeriveParameters(comm);
            foreach(SqlParameter p in comm.Parameters)
            {
                object value = DBNull.Value;
                string name = p.ParameterName;
                string s = name[0] == '@' ? name.Substring(1) : name;
                if (dcc.Contains(s))
                {
                    value = drv[s] ?? DBNull.Value;
                }                
                else if (s == Cmi.ProcIDParameterName)
                {
                    value = Cmi.ProcID.HasValue ? (object)cmi.ProcID : DBNull.Value;
                }
                else if(dcc.Contains("hdn"+ s))
                {
                    value = drv["hdn" + s] ?? DBNull.Value;
                }
                else if(dcc.Contains("dtl_" + s))
                {
                    value = drv["dtl_" + s] ?? DBNull.Value;
                }
                comm.Parameters[name].Value = value;
            }
            conn.Close();
            /*
            foreach (string s in cmi.DataRowMappings)
            {
                object value = DBNull.Value;
                if(drv[s] != null)
                    value = drv[s];
                comm.Parameters.AddWithValue("@" + s, value);
            }*/
            /*
            if (!string.IsNullOrWhiteSpace(Cmi.ProcIDParameterName))
            {
                comm.Parameters.AddWithValue("@" + Cmi.ProcIDParameterName, Cmi.ProcID);
            }*/
            //Deprecated mappings, only use columns from the data row
            //foreach (var kv in cmi.Mappings)
            //{
            //    comm.Parameters.AddWithValue("@" + kv.Key, kv.Value);
            //}
        }
        public SqlCommand GetCommand()
        {
            return comm;
        }
        public int Execute()
        {
            int x = -1;
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                x = comm.ExecuteNonQuery();
                int temp = (int)comm.Parameters["@RETURN_VALUE"].Value;
                if (temp != 0)
                    x = temp;
            }
            catch(SqlException ex)
            {
                Handle(ex, null);
                return x;
            }
            finally
            {
                conn.Close();
            }
            return x;
        }
        #region selects
        public DataSet SelectDataSet()
        {
            DataSet ds = new DataSet();
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                SqlDataAdapter sda = new SqlDataAdapter(comm);
                sda.Fill(ds);
            }
            catch (SqlException ex)
            {
                Handle(ex, null);
                return ds;
            }
            finally
            {
                conn.Close();
            }
            return ds;
        }
        public DataTable SelectDataTable()
        {
            DataTable dt = new DataTable();
            try
            {
                if(conn.State != ConnectionState.Open)
                    conn.Open();
                SqlDataAdapter sda = new SqlDataAdapter(comm);
                sda.Fill(dt);
            }
            catch (SqlException ex)
            {
                Handle(ex, null);
                return dt;
            }
            finally
            {
                //conn.Close();
            }
            return dt;
        }
        public DataRow SelectDataRow()
        {
            DataTable dt = SelectDataTable();
            if (dt.Rows.Count == 0)
                return null;
            return SelectDataTable().Rows[0];
        }
        #endregion
        ~ContextMenuItemQuery()
        {
            Dispose();
        }
        /// <summary>
        /// Based on the set up of this item, runs the context action for the provided DataRowView.
        /// </summary>
        /// <param name="drv">Not used for setup, but instead will be passed to any windows that are opened by the context action.</param>
        /// <returns>Count of records affected if the method runs SQL *directly*</returns>
        public int RunContextAction(DataRowView drv)
        {
            int rc = 0;
            if (string.IsNullOrWhiteSpace(cmi.ProcedureCall))
            {
                if (cmi.Dashboard != null)
                {
                    if(conn.State == ConnectionState.Open)
                        conn.Close();
                    EditableDashboardDisplay edd = new EditableDashboardDisplay(drv, cmi.Dashboard, readOnlyMode: true);
                    edd.ShowDialog(true); //Non editing mode.
                }
                else
                    Handle("Context menu does not have a procedure, Dashboard, or Plugin specified. No action."
                        , SEIDR.Dynamics.ExceptionLevel.UI_Basic);
                return 0;
            }
            if (cmi.Dashboard != null)
            {
                //DashboardDisplay dd = new DashboardDisplay(cmiq.GetCommand(), db, cmi.Dashboard);
                var ds = SelectDataSet();
                rc = ds.Tables[0].Rows.Count;
                if (rc != 0)
                {
                    if (conn.State == ConnectionState.Open)
                        conn.Close();
                    if (!cmi.SingleDetail)
                    {
                        //DashboardDisplay dd = new DashboardDisplay(cmiq.GetCommand(), db, cmi.Dashboard);
                        //dd.ShowDialog();                        
                        GridDashboardWindow gd = new GridDashboardWindow(GetCommand(), db, cmi);
                        gd.ShowDialog(true);
                    }
                    else
                    {
                        string[] Excluded = null;
                        ComboDisplay[] combos = null;
                        if (ds.Tables.Count > 1)
                            combos = ComboDisplay.Build(ds.Tables[1]).ToArray();
                        if (ds.Tables.Count > 2)
                            Excluded = EditableDashboardDisplay.SetExceptionList(ds.Tables[2]);

                        EditableDashboardDisplay edd = new EditableDashboardDisplay(GetCommand(), db.Connection,
                            cmi.Dashboard, cmi.DetailChangeProc, Excluded, combos);
                        edd.ShowDialog(true);
                    }//SingleDetail
                }//if(rc!= 0)
            }
            else
            {
                rc = Execute();                
            }
            if (rc == 0)
            {
                
                Handle($"Context Menu {cmi.Key}({cmi.ID}): Result count was zero.", SEIDR.Dynamics.ExceptionLevel.UI_Advanced);
            }
            return rc;
        }
        public void Dispose()
        {            
            if (comm != null)
            {
                comm.Dispose();
                comm = null;
            }
            if (conn != null)
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
                conn.Dispose();
                conn = null;
            }
        }
    }
}
