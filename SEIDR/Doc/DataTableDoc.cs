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
                    v[col] = o;
                else
                    v[col] = DBNull.Value;
            }
            dr.ItemArray = v;
        }
        public DataTableDoc(System.Data.DataTable dtSource)
        {
            ColumnSet = new DocRecordColumnCollection(dtSource.TableName);
            foreach(System.Data.DataColumn col in dtSource.Columns)
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
