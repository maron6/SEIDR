using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Reflection;
using System.Data.SqlClient;
using System.Collections;

namespace SEIDR.DataBase
{
    /// <summary>
    /// Extensions to System.Data objects
    /// </summary>
    public static class DatabaseExtensions
    {
        /// <summary>
        /// Converts the DataTable from the specified index of the dataset into a List of <typeparamref name="RT"/>
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="ds"></param>
        /// <param name="TableIndex"></param>
        /// <returns></returns>
        public static List<RT> ToContentList<RT>(this DataSet ds, int TableIndex = 0) where RT: new()
        {
            if (ds == null || ds.Tables.Count < TableIndex)
                return null;
            return ds.Tables[TableIndex].ToContentList<RT>();
        }
        /// <summary>
        /// Converts datatable to a list of objects of type RT. Returns null if the datatable passed is null.
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static List<RT> ToContentList<RT>(this DataTable dt) where RT : new()
        {
            if (dt == null)
                return null;
            List<RT> rl = new List<RT>();
            if (dt.Rows.Count == 0)
                return rl;
            
            Type tInfo = typeof(RT);// work.GetType();
            Dictionary<string, PropertyInfo> md;

            lock (((ICollection)mapCache).SyncRoot)
            {
                if (!mapCache.TryGetValue(tInfo, out md))
                {
                    md = tInfo.GetProperties().Where(p => p.CanWrite).ToDictionary(p => p.Name, p => p);
                    mapCache[tInfo] = md;
                }
            }
            foreach (DataRow r in dt.Rows)
            {
                RT work = new RT();
                Map(work, r, md, dt.Columns);
                rl.Add(work);                
            }
            return rl;
        }
        /// <summary>
        /// If the specified table is in the DataSet and contains a row at the specified index, returns a new record of type <typeparamref name="RT"/>.
        /// <para>Else, returns the default for the type.</para>
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="ds"></param>
        /// <param name="TableIndex"></param>
        /// <param name="RowIndex"></param>
        /// <returns></returns>
        public static RT ToContentRecord<RT>(this DataSet ds, int TableIndex = 0, int RowIndex = 0) where RT : new()
        {
            if (ds == null || ds.Tables.Count < TableIndex)
                return default(RT);
            return ds.Tables[TableIndex].ToContentRecord<RT>(RowIndex);
        }
        /// <summary>
        /// If the specified row is in the Datatable, returns a new record of type <typeparamref name="RT"/>.
        /// <para>Else, returns the default for the type.</para>
        /// </summary>
        public static RT ToContentRecord<RT>(this DataTable table, int RowIndex) where RT : new()
        {
            if (table == null || RowIndex >= table.Rows.Count)
                return default(RT);
            return table.Rows[RowIndex].ToContentRecord<RT>();
        }
        /// <summary>
        /// Returns a new copy of type <typeparamref name="RT"/>. 
        /// If the passed DataRow is null, returns default for the Type.
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="row"></param>
        /// <returns></returns>
        public static RT ToContentRecord<RT>(this DataRow row) where RT: new()
        {
            if (row == null)
                return default(RT);
            RT work = new RT();
            if (row.Table.Columns.Count == 0)
                return work;                        
            Type tInfo = typeof(RT);
            Dictionary<string, PropertyInfo> md;
            lock (((ICollection)mapCache).SyncRoot)
            {
                if (!mapCache.TryGetValue(tInfo, out md))
                {
                    md = tInfo.GetProperties().Where(p => p.CanWrite).ToDictionary(p => p.Name, p => p); //Cache this in a limited dictionary of <Type, Dictionary<string, PropertyInfo>> ?
                    mapCache[tInfo] = md; //don't worry about cached values becoming innacurate, since the TypeInfo isn't going to change after compilation.
                                          //Even dynamic class matching is based on definition matching
                }
            }
            Map(work, row, md, row.Table.Columns);
            return work;
        }
        static Dictionary<Type, Dictionary<string, PropertyInfo>> mapCache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
        public static DataRowView CreateDataRowView<RT>(RT Record) where RT: new()
        {
            DataTable dt = Record.ToTable();
            if (dt == null || dt.Rows.Count == 0)
                return null;
            return dt.AsDataView()[0];
        }
        public static DataRow CreateDataRow<RT>(RT Record) where RT: new()
        {
            DataTable dt = Record.ToTable();
            if (dt == null || dt.Rows.Count == 0)
                return null;
            return dt.Rows[0];
        }
        private static void Map(object map, DataRow r, Dictionary<string, PropertyInfo> properties, DataColumnCollection dcc)
        {
            foreach(DataColumn col in dcc)
            {
                PropertyInfo p;
                if(properties.TryGetValue(col.ColumnName, out p) )
                {
                    object nValue = r[col.ColumnName] ?? DBNull.Value;
                    if (nValue == DBNull.Value && p.PropertyType.IsClass)
                        nValue = null;
                    else if (nValue == DBNull.Value)
                        continue; //Null value for struct - skip
                    else
                    {
                        Type underType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                        if (underType.IsEnum)
                        {
                            nValue = Enum.Parse(underType, nValue.ToString(), true);
                        }
                        else if (underType.IsArray)
                            continue; //Skip arrays...doesn't really make sense for coming from a dataRow, although it might be possible to do dynamically..
                        else if(underType == typeof(char))
                        {
                            var svalue = nValue.ToString();
                            if (svalue.Length > 1)
                                throw new InvalidCastException($"{p.Name} tried to set a value of {svalue}, but needs to be a single char.");
                            nValue = svalue[0];
                        }
                        /*else
                        {
                            nValue = p.GetValue(map);
                        }*/
                    }
                    p.SetValue(map, nValue);
                }                
            }
        }
        
        public static DataTable ToTable<RT>(this RT o, params string[] ignoredProperties) where RT : new()
        {
            return o.ToTable(null, ignoredProperties);
        }
        public static DataTable ToTable<RT>(this RT o, string TableName, params string[] ignoredProperties) where RT:new()
        {
            var l = new[] { o };
            return (l.AsEnumerable()).ToTable(TableName, ignoredProperties);
        }
        public static DataTable ToTable<RT> (this IEnumerable<RT> o, params string[] ignoredProperties) where RT: new()
        {
            return o.ToTable(null, ignoredProperties);
        }        
        public static DataTable ToTable<RT>(this IEnumerable<RT> o, string TableName, params string[] ignoredProperties) where RT : new()
        {
            DataTable dt = new DataTable(TableName);
            dt.AddColumns<RT>(ignoredProperties);
            foreach(var item in o)
            {
                dt.AddRow(item);
            }
            return dt;
        }
        /// <summary>
        /// Add Missing columns to the datatable by using the names and information of properties from type <typeparamref name="RT"/>
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="dt"></param>
        /// <param name="IgnoredProperties"></param>
        public static void AddColumns<RT>(this DataTable dt, params string[] IgnoredProperties)
        {            
            Type t = typeof(RT);
            if (dt == null)
                dt = new DataTable(t.Name);
            var props = t.GetProperties().Where(p => !IgnoredProperties.Contains(p.Name) && !dt.Columns.Contains(p.Name));
            if (props.Count() == 0)
                return;
            foreach(var prop in props)
            {
                bool nullable = prop.PropertyType.IsClass || dt.Rows.Count > 0;

                Type underlying = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (!nullable && underlying != prop.PropertyType)
                    nullable = true;

                dt.Columns.Add(new DataColumn
                {
                    ColumnName = prop.Name,
                    DataType = underlying,
                    AllowDBNull = nullable                                        
                });
            }
        }
        /// <summary>
        /// Maps the properties of <paramref name="obj"/> to a new row in <paramref name="dt"/>.
        /// <para>If <paramref name="obj"/> is null, each column in the new row will be mapped to DBNull.</para>
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="dt"></param>
        /// <param name="obj"></param>
        public static void AddRow<RT>(this DataTable dt, RT obj)
        {
            if (dt == null || dt.Columns.Count == 0)
                dt.AddColumns<RT>();
            Type t = typeof(RT);
            DataRow r = dt.NewRow();
            var props = t.GetProperties().Where(p => dt.Columns.Contains(p.Name));
            if( obj == null)
            {                
                r.MapEachColumn( c => DBNull.Value);
                dt.Rows.Add(r);
                return;
            }
            foreach(var prop in props)
            {
                r[prop.Name] = prop.GetValue(obj) ?? DBNull.Value;
            }
            //r.MapObjectsToColumns(props, p => p.Name, p => p.GetValue(obj));
            dt.Rows.Add(r);
        }
        public static void AddRowRange<RT>(this DataTable dt, IEnumerable<RT> range)
        {
            range.ForEach(r => dt.AddRow(r));
        }
        public static void ForEachColumn(this DataRow r, Action<object> apply)
        {
            foreach(DataColumn col in r.Table.Columns)
            {
                apply(r[col]);
            }
        }
        /// <summary>
        /// Applies mapping function to each column of the row. Ignores any existing values
        /// </summary>
        /// <param name="r"></param>
        /// <param name="map"></param>
        public static void MapEachColumn(this DataRow r, Func<DataColumn, object> map)
        {
            foreach(DataColumn col in r.Table.Columns)
            {
                r[col.ColumnName] = map(col);
            }
        }
        /// <summary>
        /// Applies mapping function to each column of the row
        /// </summary>
        /// <param name="r"></param>
        /// <param name="mapUpdate"> Mapping function that can consider any existing value in the column</param>
        public static void MapEachColumn(this DataRow r, Func<DataColumn, object, object> mapUpdate)
        {
            foreach(DataColumn col in r.Table.Columns)
            {
                object o = r[col.ColumnName];
                r[col.ColumnName] = mapUpdate(col, o);
            }
        }
        public static void MapObjectsToColumns<RT>(this DataRow r, IEnumerable<RT> values, Func<RT, string> mapToColumnName)
        {
            values.ForEach(v => r[mapToColumnName(v)] = ((object)v ?? DBNull.Value));
        }
        public static void MapObjectsToColumns<RT>(this DataRow r, IEnumerable<RT> source, 
            Func<RT, string> mapColumnName, Func<RT, object> mapValue)
        {
            source.ForEach(s => r[mapColumnName(s)] = mapValue(s) ?? DBNull.Value);
        }
        /// <summary>
        /// Returns the first table from the dataset, or null if there are no tables.
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static DataTable GetFirstTableOrNull(this DataSet ds)
        {
            if (ds == null || ds.Tables.Count == 0)
                return null;
            return ds.Tables[0];
        }
        /// <summary>
        /// Returns first row of the dataTable, if it has any rows. Else returns null.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DataRow GetFirstRowOrNull(this DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0)
                return null;
            return dt.Rows[0];
        }
        /// <summary>
        /// Returns first row of the first table, or null if there there is none.
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static DataRow GetFirstRowOrNull(this DataSet ds)
        {
            return GetFirstTableOrNull(ds).GetFirstRowOrNull();
        }
        /// <summary>
        /// Returns the first row of the specified Table, if the dataset contains that many tables
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="Table">Zero based index of the table to be grabbed</param>
        /// <returns></returns>
        public static DataRow GetFirstRowOrNull(this DataSet ds, int Table)
        {
            DataTable dt = ds.GetTableOrNull(Table);
            if (dt == null || dt.Rows.Count == 0)
                return null;
            return dt.Rows[0];
        }
        /// <summary>
        /// Return table at zero based index or null
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static DataTable GetTableOrNull(this DataSet ds, int index)
        {
            if (ds == null || ds.Tables.Count < index)
                return null;
            return ds.Tables[index];
        }
        /// <summary>
        /// Gets the Return value of the command. If no return value is set, return 0.
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public static int GetReturnValue(this SqlCommand cmd)
        {
            if(cmd.Parameters.Contains("@RETURN_VALUE"))
                return (int)cmd.Parameters["@RETURN_VALUE"].Value;
            return 0;
        }
        public static T GetValue<T>(this DataRow row, string Column, T def = default(T))
        {
            if (row[Column].In(null, DBNull.Value))
                return def;
            return (T)row[Column];
        }
        public static string GetStringValue(this DataRow row, string Column)
        {
            if (row[Column].In(null, DBNull.Value))
                return string.Empty;
            return row[Column].ToString();
        }
    }
}
