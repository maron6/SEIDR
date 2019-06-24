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
        #endregion
        
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
            n.CopyDetailedColumnCollection(ColumnSet);
            return n;
        }
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
            foreach(System.Data.DataRow row in source.Rows)
            {
                DT v = new DT();
                v.Configure(ColumnSet, false, row.ItemArray);
                yield return v;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
