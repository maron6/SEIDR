using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// For large set of data from DB, read FORWARD only. (As is the underlying <see cref="SqlDataReader"/>)
    /// </summary>
    public class DataReaderDoc:IDisposable, IEnumerable<TypedDataRecord>
    {
        public static implicit operator DataReaderDoc(SqlDataReader reader)
        {
            return new DataReaderDoc(reader);
        }
        public static implicit operator SqlDataReader(DataReaderDoc doc)
        {
            return doc._reader;
        }
        public static implicit operator DataReaderDoc(SqlCommand cmd)
        {
            return new DataReaderDoc(cmd.ExecuteReader()) { cmd = cmd };
        }
        SqlCommand cmd = null;
        SqlDataReader _reader;
        /// <summary>
        /// Underlying column collection from the schema of the reader.
        /// </summary>
        public DocRecordColumnCollection ColumnSet { get; private set; }
        /// <summary>
        /// Creates a new metadata for specified file path by copying the column set of this object.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public DocMetaData GetMetaDataForFile(string filePath)
        {
            var md = new DocMetaData(filePath);
            return md.AddDetailedColumnCollection(ColumnSet);
        }
        /// <summary>
        /// Creates a new metadata for specified file path by copying the column set of this object.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="FileName"></param>
        /// <param name="alias"></param>        
        /// <returns></returns>
        public DocMetaData GetMetaDataForFile(string folder, string FileName, string alias = null)
        {
            var md = new DocMetaData(folder, FileName, alias);
            return md.AddDetailedColumnCollection(ColumnSet);
        }
        public DataReaderDoc(SqlDataReader reader)
        {
            _reader = reader;
            ColumnSet = new DocRecordColumnCollection(null as string);
            foreach(System.Data.DataRow row in _reader.GetSchemaTable().Rows)
            {
                DocRecordColumnType t;
                if (!Enum.TryParse((string)row["DataTypeName"], true, out t))
                    t = DocRecordColumnType.Unknown;
                var col = new DocRecordColumnInfo(row["ColumnName"].ToString(), t);
                if (t.In(DocRecordColumnType.Varchar, DocRecordColumnType.NVarchar, DocRecordColumnType.Unknown))
                    col.MaxLength = row["ColumnSize"] as int?;
                ColumnSet.AddColumn(col);
            }
            
        }
        DocRecordColumnCollection columns;
        /// <summary>
        /// Dispose underlying reader.
        /// </summary>
        public void Dispose()
        {
            if (cmd != null)
                cmd.Dispose();
            _reader.Dispose();
        }
        /// <summary>
        /// Number of records that have been read so far.
        /// </summary>
        public int CurrentRowID { get; private set; } = 0;
        public IEnumerator<TypedDataRecord> GetEnumerator()
        {
            if (_reader.IsClosed)
                throw new InvalidOperationException("Underlying reader is closed.");
            while (_reader.Read())
            {
                var record = (System.Data.IDataRecord)_reader;
                TypedDataRecord ret = new TypedDataRecord(ColumnSet);
                foreach(var col in ColumnSet)
                {
                    object o;                    
                    ret[col] = new DataItem(record[col.Position], col.DataType);
                    (ret as DocEditor.IDetailDataRecord).SetID(CurrentRowID++);
                }
                yield return ret;
            }
            _reader.Close();
            yield break;
        }
        /// <summary>
        /// Returns true if the underlying reader has finished reading and is now closed.
        /// </summary>
        public bool Closed => _reader.IsClosed;
        public bool HasRows
        {
            get { return _reader.HasRows; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
