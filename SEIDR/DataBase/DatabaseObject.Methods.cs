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
    public abstract class DatabaseObject<RT> : DatabaseObject where RT : DatabaseObject<RT>, new()
    {
        /// <summary>
        /// Constructor for the Generic base class inheriting from <see cref="DatabaseObject"/>
        /// </summary>
        /// <param name="dbm"></param>
        public DatabaseObject(DatabaseManager dbm):base(dbm){}
        /// <summary>
        /// Basic, parameterless constructor to allow constructing from reflection.
        /// </summary>
        public DatabaseObject() { }
        /// <summary>
        /// Executes the non query procedure by passing to the DatabaseManager
        /// </summary>
        /// <param name="QualifiedProcedure"></param>
        public void ExecuteProcedure(string QualifiedProcedure)
        {
            //string Schema = "dbo";
            //string[] split = QualifiedProcedure.Split('.');
            //if (split.Length > 1)
            //    Schema = split[0];
            //obj.GetDefaultManager(Schema).ExecuteNonQuery(QualifiedProcedure, obj);
            if (!QualifiedProcedure.Contains('.'))
                QualifiedProcedure = "dbo." + QualifiedProcedure;
            Manager.ExecuteNonQuery(QualifiedProcedure, this);            
        }
        /// <summary>
        /// Executes the non query procedure by passing to the DatabaseManager
        /// </summary>
        /// <param name="Schema"></param>
        /// <param name="Procedure"></param>
        public void ExecuteProcedure(string Schema, string Procedure) => ExecuteProcedure($"[{Schema}].{Procedure}");
        /// <summary>
        /// Executes the procedure by passing to the DatabaseManager and returning any result set.
        /// </summary>
        /// <param name="QualifiedProcedure"></param>
        /// <returns></returns>
        public DataSet PassToProcedure( string QualifiedProcedure)
        {
            //string Schema = "dbo";
            //string[] split = QualifiedProcedure.Split('.');
            //if (split.Length > 1)
            //    Schema = split[0];
            if (!QualifiedProcedure.Contains('.'))
                QualifiedProcedure = "dbo." + QualifiedProcedure;
            //if(_DBManager == null)
            //    _DBManager = new DatabaseManager(Connection, Schema);
            return Manager.Execute(QualifiedProcedure, this);
        }
        /// <summary>
        /// Executes the procedure by passing to the DatabaseManager and returning any result set.
        /// </summary>
        /// <param name="Schema"></param>
        /// <param name="Procedure"></param>
        /// <returns></returns>
        public DataSet PassToProcedure( string Schema, string Procedure) => PassToProcedure($"[{Schema}].{Procedure}");

        /// <summary>
        /// Executes the procedure by passing to the DatabaseManager and returning the first table of any result set.
        /// </summary>
        /// <param name="QualifiedProcedure"></param>
        /// <returns></returns>
        public DataTable PassToProcedureForTable(string QualifiedProcedure) => PassToProcedure(QualifiedProcedure).GetFirstTableOrNull();        
        /// <summary>
        /// Executes the procedure by passing to the DatabaseManager and returning the first table of any result set.
        /// </summary>
        /// <param name="Schema"></param>
        /// <param name="Procedure"></param>
        /// <returns></returns>
        public DataTable PassToProcedureForTable(string Schema, string Procedure) => PassToProcedureForTable($"[{Schema}].{Procedure}");
        /// <summary>
        /// Executes the procedure by passing to the DatabaseManager and returning the first record of the first table of any result set.
        /// <para>Result record is converted to a <see cref="RT"/>. </para>
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="model">Model for the query to be executed</param>
        /// <returns></returns>
        public static RT GetRecord(DatabaseManager manager, DatabaseManagerHelperModel model)
        {
            DataRow r = manager.Execute(model).GetFirstRowOrNull();
            var ret = r?.ToContentRecord<RT>();
            if (ret != null)
                ret.Manager = manager;
            return ret;
        }     
        /// <summary>
        /// Executes the procedure by passing to the DatabaseManager and returning the first table of any result set, converted to a list of <see cref="RT"/>
        /// </summary>        
        /// <returns></returns>
        public static List<RT> GetList(DatabaseManager manager, DatabaseManagerHelperModel model)
        {
            DataTable dt = manager.Execute(model).GetFirstTableOrNull();
            if (dt == null)
                return new List<RT>();
            var l = dt.ToContentList<RT>();
            foreach(var i in l)
            {
                i.Manager = manager;
            }
            return l;
        }
        /// <summary>
        /// Executes the procedure by passing to the DatabaseManager and returning the first row of the first table of any result set.
        /// </summary>
        /// <param name="QualifiedProcedure"></param>
        /// <returns></returns>
        public DataRow GetRow(string QualifiedProcedure) => Manager.Execute(QualifiedProcedure, mapObj: this).GetFirstRowOrNull();
        
        /// <summary>
        /// Executes the procedure by passing to the DatabaseManager and returning the first row of the first table of any result set.
        /// </summary>
        /// <param name="Schema"></param>
        /// <param name="Procedure"></param>
        /// <returns></returns>
        public DataRow GetRow( string Schema, string Procedure) => GetRow($"[{Schema}].{Procedure}");

    }
}
