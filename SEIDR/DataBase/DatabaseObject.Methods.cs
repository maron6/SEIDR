using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace SEIDR.DataBase
{
    public partial class DatabaseObject
    {
        public void ExecuteProcedure(string QualifiedProcedure)
        {
            string Schema = "dbo";
            string[] split = QualifiedProcedure.Split('.');
            if (split.Length > 1)
                Schema = split[0];
            //obj.GetDefaultManager(Schema).ExecuteNonQuery(QualifiedProcedure, obj);
            DatabaseManager dm = new DatabaseManager(Connection, Schema);
            dm.ExecuteNonQuery(QualifiedProcedure, this);
        }
        public void ExecuteProcedure(string Schema, string Procedure) => ExecuteProcedure($"[{Schema}].{Procedure}");

        public DataSet PassToProcedure( string QualifiedProcedure)
        {
            string Schema = "dbo";
            string[] split = QualifiedProcedure.Split('.');
            if (split.Length > 1)
                Schema = split[0];
            DatabaseManager dm = new DatabaseManager(Connection, Schema);
            return dm.Execute(QualifiedProcedure, this);
        }
        public DataSet PassToProcedure( string Schema, string Procedure) => PassToProcedure($"[{Schema}].{Procedure}");

        public DatabaseManager GetDefaultManager(string Schema = "dbo")
        {
            return new DatabaseManager(Connection, Schema);
        }

        public DataTable GetTable( string QualifiedProcedure)
        {
            DataSet ds = PassToProcedure(QualifiedProcedure);
            if (ds == null || ds.Tables.Count == 0)
                return null;
            return ds.Tables[0];
        }
        public DataTable GetTable(string Schema, string Procedure) => GetTable($"[{Schema}].{Procedure}");

        public RT GetRecord<RT>(string QualifiedProcedure) where RT : new()
        {
            DataRow r = GetRow(QualifiedProcedure);
            return r.ToContentRecord<RT>();
        }
        public RT GetRecord<RT>(string Schema, string Procedure)
            where RT : new() => GetRecord<RT>($"[{Schema}].{Procedure}");        
        public List<RT> GetList<RT>(string QualifiedProcedure) where RT : new()
        {
            DataTable dt = GetTable(QualifiedProcedure);
            return dt.ToContentList<RT>();
        }
        public List<RT> GetList<RT>(string Schema, string Procedure)
            where RT : new() => GetList<RT>($"[{Schema}].{Procedure}");
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
