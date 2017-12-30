using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace SEIDR.DataBase
{
    /// <summary>
    /// Base class inheriting from <see cref="DatabaseObject"/>, which adds basic method functionality.
    /// Use with a recursive generic.
    /// </summary>
    /// <typeparam name="RT">Recursive generic. Ex: class myObject: DatabaseObject&lt;myObject> </typeparam>
    public abstract class DatabaseObject<RT>: DatabaseObject where RT:DatabaseObject<RT>, new()
    {
        /// <summary>
        /// A Connection manager utility object. Auto populated when calling <see cref="ExecuteProcedure(string)"/>  or <see cref="PassToProcedure(string)"/>
        /// </summary>
        protected DatabaseManager _DBManager;
        public void ExecuteProcedure(string QualifiedProcedure)
        {
            string Schema = "dbo";
            string[] split = QualifiedProcedure.Split('.');
            if (split.Length > 1)
                Schema = split[0];
            //obj.GetDefaultManager(Schema).ExecuteNonQuery(QualifiedProcedure, obj);
            if(_DBManager == null)
                _DBManager = new DatabaseManager(Connection, Schema);
            _DBManager.ExecuteNonQuery(QualifiedProcedure, this);            
        }
        public void ExecuteProcedure(string Schema, string Procedure) => ExecuteProcedure($"[{Schema}].{Procedure}");

        public DataSet PassToProcedure( string QualifiedProcedure)
        {
            string Schema = "dbo";
            string[] split = QualifiedProcedure.Split('.');
            if (split.Length > 1)
                Schema = split[0];
            if(_DBManager == null)
                _DBManager = new DatabaseManager(Connection, Schema);
            return _DBManager.Execute(QualifiedProcedure, this);
        }
        public DataSet PassToProcedure( string Schema, string Procedure) => PassToProcedure($"[{Schema}].{Procedure}");


        public DataTable GetTable( string QualifiedProcedure)
        {
            DataSet ds = PassToProcedure(QualifiedProcedure);
            if (ds == null || ds.Tables.Count == 0)
                return null;
            return ds.Tables[0];
        }
        public DataTable GetTable(string Schema, string Procedure) => GetTable($"[{Schema}].{Procedure}");

        public RT GetRecord(string QualifiedProcedure)
        {
            DataRow r = GetRow(QualifiedProcedure);
            return r.ToContentRecord<RT>();
        }
        public RT GetRecord(string Schema, string Procedure)
             => GetRecord($"[{Schema}].{Procedure}");        
        public List<RT> GetList(string QualifiedProcedure)
        {
            DataTable dt = GetTable(QualifiedProcedure);
            return dt.ToContentList<RT>();
        }
        public List<RT> GetList(string Schema, string Procedure)
            => GetList($"[{Schema}].{Procedure}");
        public DataRow GetRow(string QualifiedProcedure)
        {
            DataTable dt = GetTable(QualifiedProcedure);
            if (dt == null || dt.Rows.Count == 0)
                return null;
            return dt.Rows[0];
        }
        public DataRow GetRow( string Schema, string Procedure) => GetRow($"[{Schema}].{Procedure}");

    }
}
