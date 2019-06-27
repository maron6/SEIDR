using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// Enumerate readonly versions of DataTable as though the DataRows are of type <typeparamref name="DT"/>
    /// </summary>
    /// <typeparam name="DT"></typeparam>
    public class DataTableDoc<DT> : IEnumerable<DT> where DT:IDataRecord, new()
    {
        #region casting
        /// <summary>
        /// Allow casting a DataTable as a DataTableDoc
        /// </summary>
        /// <param name="source"></param>
        public static explicit operator DataTableDoc<DT>(System.Data.DataTable source)
        {
            return new DataTableDoc<DT>(source);
        }
        /// <summary>
        /// Allow using a DataTableDoc as a <see cref="System.Data.DataTable"/>, by returning the underlying source object.
        /// </summary>
        /// <param name="source"></param>
        public static implicit operator System.Data.DataTable(DataTableDoc<DT> source)
        {
            return source.source;
        }
        /// <summary>
        /// Allow using a DataTableDoc as a DocRecordColumn collection by returning the underlying column set
        /// </summary>
        /// <param name="source"></param>
        public static implicit operator DocRecordColumnCollection(DataTableDoc<DT> source)
        {
            return source.ColumnSet;
        }
        #endregion
        
        /// <summary>
        /// Underlying column set built by parsing DataColumn information from the source dataTable.
        /// </summary>
        public DocRecordColumnCollection ColumnSet { get; private set; }
        System.Data.DataTable source;
        /// <summary>
        /// Gets a meta data that can be used for writing the data in this instance to a file.
        /// </summary>
        /// <param name="DestinationFile"></param>
        /// <returns></returns>
        public DocMetaData GetMetaData(string DestinationFile)
        {
            DocMetaData n = new DocMetaData(DestinationFile);
            n.CanWrite = true;
            n.CopyDetailedColumnCollection(ColumnSet);
            return n;
        }
        /// <summary>
        /// Clear the rows in the underlying table.
        /// </summary>
        public void Clear()
        {
            source.Rows.Clear();
        }
        /// <summary>
        /// Number of rows in underlying table.
        /// </summary>
        public int RecordCount => source.Rows.Count;
        /// <summary>
        /// Number of columns in underlying table.
        /// </summary>
        public int ColumnCount => source.Columns.Count;
        public void AddRecord(IDataRecord record)
        {            
            var dr = source.NewRow();
            var v = new object[ColumnSet.Columns.Count];
            foreach(var col in ColumnSet.Columns)
            {
                object o;
                if (record.TryGet(col, out o))
                    v[col] = o ?? DBNull.Value;
                else
                    v[col] = DBNull.Value;
            }
            dr.ItemArray = v;
            source.Rows.Add(dr);
        }
        public void AddRecord(IDataRecord record, DocWriterMap columnMappings)
        {
            if (columnMappings is null || columnMappings.MapData.Count == 0)
            {
                AddRecord(record);
                return;
            }
            var map = columnMappings.MapData;
            if (map.Exists(m => m.Key >= ColumnSet.Count))
                throw new ArgumentException("Column Mappings goes out of range for DataTable columns.", nameof(columnMappings));
            
            var dr = source.NewRow();
            var v = new object[ColumnSet.Columns.Count];

            for (int idx = 0; idx <= ColumnSet.Count; idx++)
            {
                DocRecordColumnInfo col = null;                
                if (map.ContainsKey(idx))
                    col = map[idx] ?? ColumnSet[idx];
                else 
                    col = ColumnSet[idx];
                
                object o;
                if (record.TryGet(col, out o))
                    v[idx] = o ?? DBNull.Value;
                else
                    v[idx] = DBNull.Value;
            }
            dr.ItemArray = v;
            source.Rows.Add(dr);
        }
        /// <summary>
        /// Gets a basic record set, which is NOT attached to this object, but does have the same column collection.
        /// </summary>
        /// <returns></returns>
        public DT GetBasicRecord() => ColumnSet.GetRecord<DT>();                    
        /// <summary>
        /// Removes a column from both the underlying table's columnset, and the underlying DocRecord Column set.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public DataTableDoc<DT> RemoveColumn(DocRecordColumnInfo columnName)
        {
            source.Columns.Remove(columnName);
            ColumnSet.RemoveColumn(columnName);
            return this;
        }
        
        public static DocRecordColumnCollection GetColumnCollection(string Alias, System.Data.DataColumnCollection dataColumns)
        {
            var ColumnSet = new DocRecordColumnCollection(Alias);            
            foreach (System.Data.DataColumn col in dataColumns)
            {
                DocRecordColumnType colType = DocRecordColumnType.Unknown;
                switch (Type.GetTypeCode(col.DataType))
                {
                    case TypeCode.String:
                        colType = DocRecordColumnType.Varchar;
                        break;
                    case TypeCode.Int16:
                        colType = DocRecordColumnType.Smallint;
                        break;
                    case TypeCode.DateTime:
                        colType = DocRecordColumnType.DateTime;
                        break;
                    case TypeCode.Double:
                        colType = DocRecordColumnType.Double;
                        break;
                    case TypeCode.Byte:
                        colType = DocRecordColumnType.Tinyint;
                        break;
                    case TypeCode.Char:
                        colType = DocRecordColumnType.NVarchar;
                        break;
                    case TypeCode.Decimal:
                        colType = DocRecordColumnType.Decimal;
                        break;
                    case TypeCode.DBNull:
                        colType = DocRecordColumnType.NUL;
                        break;
                    case TypeCode.Int32:
                        colType = DocRecordColumnType.Int;
                        break;
                    case TypeCode.Int64:
                        colType = DocRecordColumnType.Bigint;
                        break;
                    default:
                        colType = DocRecordColumnType.Unknown;
                        break;
                }
                ColumnSet.AddColumn(col.ColumnName, col.MaxLength, true, false, colType);
            }
            return ColumnSet;
        }
        public DataTableDoc(System.Data.DataTable dtSource)
        {
            ColumnSet = GetColumnCollection(dtSource.TableName, dtSource.Columns);
            source = dtSource;
        }
        public IEnumerator<DT> GetEnumerator()
        {
            long idx = 0;
            foreach(System.Data.DataRow row in source.Rows)
            {
                DT v = new DT();
                v.Configure(ColumnSet, false, row.ItemArray);

                if (v is DocEditor.IDetailDataRecord)
                {
                    (v as DocEditor.IDetailDataRecord).SetID(idx ++);
                }
                yield return v;
            }
        }
        public DT this[int rowID]
        {
            get
            {
                DT v = new DT();
                var r = source.Rows[rowID];
                v.Configure(ColumnSet, false, r.ItemArray);
                if (v is DocEditor.IDetailDataRecord)
                {
                    (v as DocEditor.IDetailDataRecord).SetID(rowID);
                }
                return v;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
