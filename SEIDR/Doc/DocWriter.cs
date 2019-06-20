using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SEIDR.Doc
{
    public class DocWriter : DocWriter<DocMetaData>
    {
        #region operators   
        /// <summary>
        /// Allow treating the doc writer as a DocMetaData to help keep code more succinct. Returns null if the MetaData type is not a DocMetaData.
        /// </summary>
        /// <param name="writer"></param>
        public static implicit operator DocMetaData(DocWriter writer)
        {
            return writer.md as DocMetaData;
        }
        /// <summary>
        /// Allow treating the writer as a column colleciton to help keep code more succinct
        /// </summary>
        /// <param name="writer"></param>
        public static implicit operator DocRecordColumnCollection(DocWriter writer)
        {
            return writer.md?.Columns;
        }
        #endregion
        public DocWriter(DocMetaData metaData, bool AppendIfExists = false, int bufferSize = 5000)
            :base(metaData, AppendIfExists, bufferSize)
        {
            if (!metaData.Columns.Valid)
                throw new InvalidOperationException("Column state Invalid");            
        }
        
        /// <summary>
        /// Sets whether or not qualify listed columns as text when writing. Default is false.<para>Note: Ignored in FixedWidth</para>
        /// </summary>
        /// <param name="qualifying"></param>
        /// <param name="columnsToQualify"></param>
        public void SetTextQualify(bool qualifying, params string[] columnsToQualify)
        {
            foreach (var col in columnsToQualify)
            {
                Columns[col].TextQualify = qualifying;
            }
        }
        /// <summary>
        /// Sets whether or not qualify listed columns as text when writing. Default is false.<para>Note: Ignored in FixedWidth</para>
        /// </summary>
        /// <param name="qualifying"></param>
        /// <param name="columnsToQualify"></param>
        public void SetTextQualify(bool qualifying, params int[] columnsToQualify)
        {
            foreach (var col in columnsToQualify)
            {
                Columns[col].TextQualify = qualifying;
            }
        }
        /// <summary>
        /// Changes the justification for listed columns. (Default is to leftJustify)
        /// </summary>
        /// <param name="leftJustify"></param>
        /// <param name="columnsToJustify"></param>
        public void SetJustification(bool leftJustify, params string[] columnsToJustify)
        {
            foreach (var col in columnsToJustify)
            {
                Columns[col].LeftJustify = leftJustify;
            }
        }
        /// <summary>
        /// Changes the justification for listed columns. (Default is to leftJustify)
        /// </summary>
        /// <param name="leftJustify"></param>
        /// <param name="columnsToJustify"></param>
        public void SetJustification(bool leftJustify, params int[] columnsToJustify)
        {
            foreach (var col in columnsToJustify)
            {
                Columns[col].LeftJustify = leftJustify;
            }
        }
        /// <summary>
        /// Column meta Data
        /// </summary>
        public DocRecordColumnCollection Columns => md.Columns;

        
        /// <summary>
        /// Calls <see cref="AddDocRecord(DocRecord, IDictionary{int, DocRecordColumnInfo})> using the DocWriter's underlying dictionary.
        /// </summary>
        /// <param name="record"></param>
        /// <param name="columnMapping"></param>
        public void AddDocRecord(DocRecord record, DocWriterMap columnMapping)           
        {
            AddDocRecord(record, columnMapping.MapData);
        }
        //Note: for some reason, naming this method as 'AddRecord' messes up with name resolution for function, and calls to base class AddRecord doesn't work in DocSorter
        /// <summary>
        /// Adds the record to the file via streamWriter
        /// </summary>
        /// <param name="record"></param>
        /// <param name="columnMapping">Optional mapping override. Positions can be set to null or ignored to use the default mapping. 
        /// <para>Key should be the target position in the output file, value should be the column information from the source.
        /// </para>
        /// </param>
        public void AddDocRecord(DocRecord record, IDictionary<int, DocRecordColumnInfo> columnMapping = null)
        {
            sw.Write(md.FormatRecord(record, true, columnMapping));
            
        }
    }
    /// <summary>
    /// Helper using DocMetaData to wrap a StreamWriter and write metaData to a file
    /// </summary>
    public class DocWriter<MD>: IDisposable where MD: MetaDataBase
    {
        /// <summary>
        /// Adds record to output.
        /// </summary>
        /// <param name="record"></param>
        public void AddRecord(IDataRecord record)
        {
            sw.Write(md.FormatRecord(record, true));
        }
        /// <summary>
        /// True if the file being written to is being written with columns having fixed widths and positions.
        /// </summary>
        public bool FixedWidthMode => md.FixWidthMode;
        /// <summary>
        /// Sets the textQualifier. Default is null
        /// </summary>
        public string TextQualifier
        {
            get { return md.TextQualifier; }
            set { md.SetTextQualifier(value); }
        }
        /// <summary>
        /// Underlying stream.
        /// </summary>
        protected StreamWriter sw { get; private set; }
        /// <summary>
        /// Underlying MetaData
        /// </summary>
        protected MD md;        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="metaData"></param>
        /// <param name="AppendIfExists"></param>
        /// <param name="bufferSize">Initial buffer size for underlying stream in KB. 
        /// <para>Note: can be forced to grow, which can be expensive according to https://stackoverflow.com/questions/32346051/what-does-buffer-size-mean-when-streaming-text-to-a-file. </para>
        /// <para>Adding one line at a time, though, so should probably choose based on max size of a line </para>
        /// <para>May also need to consider whether you're writing locally or on a network.</para></param>
        public DocWriter(MD metaData, bool AppendIfExists = false, int bufferSize = 5000)
        {
            if (metaData.AccessMode == FileAccess.Read)
                throw new ArgumentException("MetaData Access Mode is set to READ when passing to DocWriter.", nameof(metaData));
            if (!metaData.Valid)
                throw new InvalidOperationException("MetaData is not in a valid state");
            md = metaData;
            bool AddHeader = md.HasHeader && (!File.Exists(metaData.FilePath) || !AppendIfExists);
            if (md.FileEncoding == null)
                md.FileEncoding = Encoding.Default;
            sw = new StreamWriter(metaData.FilePath, AppendIfExists, metaData.FileEncoding, bufferSize);
            if (AddHeader)
            {
                StringBuilder sb = new StringBuilder();                
                for(int i = 0; i < md.SkipLines; i ++)
                {
                    sb.Append(md.LineEndDelimiter);
                }
                sb.Append(md.GetHeader());
                md.CheckAddLineDelimiter(sb);                
                sw.Write(sb);
            }
            
        }
        
     

        /// <summary>
        /// Writes the records out using ToString without validating that they match the column meta data of the writer.
        /// <para>Null records will be ignored.</para>
        /// </summary>
        /// <param name="toWrite"></param>
        public void BulkWrite(IEnumerable<IDataRecord> toWrite)
        {
            foreach (var rec in toWrite)
            {
                if (rec == null)
                    continue;
                sw.Write(md.FormatRecord(rec, true));
            }
        }
        /// <summary>
        /// Writes the records out using ToString without validating that they match the column meta data of the writer.
        /// </summary>
        /// <param name="toWrite"></param>
        public void BulkWrite(params IDataRecord[] toWrite) => BulkWrite((IEnumerable<IDataRecord>)toWrite);

        /// <summary>
        /// Writes the strings out without validating that they match the column meta data of the writer. Will add the LineEndDelimiter of this metaData if specified, though.
        /// <para>NOTE: THIS IGNORES METADATA EXCEPT FOR LINE END DELIMITER.</para>
        /// <para>If meta Data does not have a line end delimiter, then it will *NOT* add one.</para>
        /// </summary>
        /// <param name="Lines"></param>
        public void BulkWrite(IEnumerable<string> Lines)
        {
            foreach (var line in Lines)
            {
                if (line == null)
                    continue;
                sw.Write(line + md.LineEndDelimiter ?? string.Empty);
            }
        }

        /// <summary>
        /// Writes the strings out without validating that they match the column meta data of the writer. Will add the LineEndDelimiter of this metaData if specified, though.
        /// <para>NOTE: THIS IGNORES METADATA EXCEPT FOR LINE END DELIMITER.</para>
        /// </summary>
        /// <param name="Lines"></param>
        public void BulkWrite(params string[] Lines)
        {
            foreach (var line in Lines)
            {
                if (line == null)
                    continue;
                sw.Write(line + md.LineEndDelimiter ?? string.Empty);
            }
        }
        /// <summary>
        /// Adds record to the file via underlying streamWriter
        /// </summary>
        /// <param name="record"></param>
        public void AddRecord(string record)
        {
            if (string.IsNullOrEmpty(record))
                return;
            var rc = md.ParseRecord(false, record);
            sw.Write(md.FormatRecord(rc, true));
        }
        /// <summary>
        /// Adds a StringBuilder to the output Document
        /// </summary>
        /// <param name="sbRecord"></param>
        public void AddRecord(StringBuilder sbRecord)
        {
            AddRecord(sbRecord.ToString());
        }
        /// <summary>
        /// Parses the strings and maps them using this collection's MetaData. Will add the LineEndDelimiter of this metaData if specified, though.
        /// </summary>
        /// <param name="Lines"></param>
        public void BulkAdd(IEnumerable<string> Lines)
        {
            foreach (var line in Lines)
                sw.Write(md.FormatRecord(md.ParseRecord(false, line), true));
        }

        /// <summary>
        /// Parses the strings and maps them using this collection's MetaData. Will add the LineEndDelimiter of this metaData if specified, though.
        /// </summary>
        /// <param name="Lines"></param>
        public void BulkAdd(params string[] Lines) => BulkAdd(Lines);
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }
                if(sw != null)
                {
                    if (sw.BaseStream != null && sw.BaseStream.CanWrite)
                    {
                        sw.Flush();
                    }
                    sw.Dispose();
                    sw = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }
        
        ~DocWriter()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
