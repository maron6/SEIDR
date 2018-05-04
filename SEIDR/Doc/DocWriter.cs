using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SEIDR.Doc
{
    /// <summary>
    /// Helper using DocMetaData to wrap a StreamWriter and write metaData to a file
    /// </summary>
    public class DocWriter: IDisposable
    {        
        StreamWriter sw;
        DocMetaData md;        
        public bool FixedWidthMode => md.FixedWidthMode;
        public DocWriter(DocMetaData metaData, bool AppendIfExists = false, int bufferSize = 100000)
        {
            if (!metaData.Valid)
                throw new InvalidOperationException("MetaData is not in a valid state");
            if (!metaData.Columns.Valid)
                throw new InvalidOperationException("Column state Invalid");
            md = metaData;
            bool AddHeader = md.HasHeader && (!File.Exists(metaData.FilePath) || !AppendIfExists);
            sw = new StreamWriter(metaData.FilePath, AppendIfExists, metaData.FileEncoding, bufferSize);
            if (AddHeader)
            {
                StringBuilder sb = new StringBuilder();
                for(int i = 0; i < md.SkipLines; i ++)
                {
                    sb.Append(md.LineEndDelimiter);
                }
                foreach(var col in Columns)
                {
                    if(FixedWidthMode)
                        sb.Append(col.ColumnName.PadRight(col.MaxLength.Value));
                    else
                    {
                        sb.Append(col.ColumnName);
                        if (col.Position < Columns.Count -1)
                            sb.Append(md.Delimiter);
                    }                    
                }
                if (!string.IsNullOrEmpty(md.LineEndDelimiter))
                    sb.Append(md.LineEndDelimiter);
                sw.Write(sb);
            }
            
        }
        /// <summary>
        /// Sets the textQualifier. Default is "
        /// </summary>
        public char TextQualifier
        {
            get { return Columns.TextQualifier; }
            set { Columns.TextQualifier = value; }
        }
        /// <summary>
        /// Sets whether or not qualify listed columns as text when writing. Default is false.<para>Note: Ignored in FixedWidth</para>
        /// </summary>
        /// <param name="qualifying"></param>
        /// <param name="columnsToQualify"></param>
        public void SetTextQualify(bool qualifying, params string[] columnsToQualify)
        {
            foreach(var col in columnsToQualify)
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
            foreach(var col in columnsToQualify)
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
            foreach(var col in columnsToJustify)
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
        /// Adds the record to the file via streamWriter
        /// </summary>
        /// <param name="record"></param>
        /// <param name="columnMapping">Optional mapping override. Positions can be set to null or ignored to use the default mapping. 
        /// <para>Key should be the target position in the output file, value should be the column information from the source.
        /// </para>
        /// </param>
        public void AddRecord<RecordType>(RecordType record, IDictionary<int, DocRecordColumnInfo> columnMapping = null) where RecordType: DocRecord
        {
            if (!md.Columns.Valid)
                throw new InvalidOperationException("Column state Invalid");
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if(record.Columns == md.Columns 
                && (columnMapping == null || columnMapping.Count == 0))
            {
                sw.Write(record.ToString()); //same column collection, no mapping override, just write the toString
                return;
            }
            StringBuilder sb = new StringBuilder();
            Columns.ForEachIndex((col, idx) =>
            {
                if (!md.FixedWidthMode && col.TextQualify)
                    sb.Append(Columns.TextQualifier);
                DocRecordColumnInfo map = col;
                if (columnMapping != null && columnMapping.ContainsKey(idx))
                    map = columnMapping[idx];
                string s = record.GetBestMatch(map.ColumnName, map.OwnerAlias) ?? string.Empty;
                if (FixedWidthMode)
                {
                    if (col.LeftJustify)
                        sb.Append(s.PadRight(col.MaxLength.Value));
                    else
                        sb.Append(s.PadLeft(col.MaxLength.Value));
                }
                else
                {
                    if (s.Contains(Columns.Delimiter.Value) && !col.TextQualify)
                    {
                        sb.Append(Columns.TextQualifier);
                        col.TextQualify = true; //force text qualify in the column going forward.
                    }

                    sb.Append(s);
                    if (col.TextQualify)
                        sb.Append(Columns.TextQualifier);
                    if (idx < Columns.Count - 1)
                        sb.Append(md.Delimiter.Value);
                }

            });
            if (!string.IsNullOrEmpty(md.LineEndDelimiter))
                sb.Append(md.LineEndDelimiter);
            sw.Write(sb);
        }

        /// <summary>
        /// Writes the records out using ToString without validating that they match the column meta data of the writer.
        /// <para>Null records will be ignored.</para>
        /// <para>NOTE: THIS IGNORES METADATA.</para>
        /// </summary>
        /// <param name="toWrite"></param>
        public void BulkWrite(IEnumerable<DocRecord> toWrite)
        {
            foreach (var rec in toWrite)
            {
                if (rec == null)
                    continue;
                sw.Write(rec.ToString());
            }
        }
        /// <summary>
        /// Writes the records out using ToString without validating that they match the column meta data of the writer.
        /// <para>NOTE: THIS IGNORES METADATA.</para>
        /// </summary>
        /// <param name="toWrite"></param>
        public void BulkWrite(params DocRecord[] toWrite) => BulkWrite((IEnumerable<DocRecord>)toWrite);

        /// <summary>
        /// Writes the strings out without validating that they match the column meta data of the writer. Will add the LineEndDelimiter of this metaData if specified, though.
        /// <para>NOTE: THIS IGNORES METADATA EXCEPT FOR LINE END DELIMITER.</para>
        /// </summary>
        /// <param name="Lines"></param>
        public void BulkWrite(IEnumerable<string> Lines)
        {
            foreach (var line in Lines)
            {
                if (line == null)
                    continue;
                sw.Write(line + Columns.LineEndDelimiter ?? string.Empty);
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
                sw.Write(line + Columns.LineEndDelimiter ?? string.Empty);
            }
        }
        /// <summary>
        /// Adds record to the file via underlying streamWriter
        /// </summary>
        /// <param name="record"></param>
        public void AddRecord(string record)
        {
            AddRecord(Columns.ParseRecord(false, record), null);
        }
        /// <summary>
        /// Parses the strings and maps them using this collection's MetaData. Will add the LineEndDelimiter of this metaData if specified, though.
        /// </summary>
        /// <param name="Lines"></param>
        public void BulkAdd(IEnumerable<string> Lines)
        {
            foreach (var line in Lines)
                sw.Write(Columns.ParseRecord(false, line));
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
                    sw.Flush();
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
