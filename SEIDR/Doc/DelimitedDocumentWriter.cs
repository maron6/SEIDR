using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SEIDR.Doc
{
    /// <summary>
    /// For adding delimited records to a file. 
    /// <para>Can be used in conjunction with DelimitedDocumentReader to filter or combine files</para>
    /// </summary>
    public class DelimitedDocumentWriter : IDisposable
    {
        string filePath;
        List<DelimitedRecord> records;
        public string[] Header => _Header;
        /// <summary>
        /// Creates an instance of DelimiteddocumentWriter for adding records to the FilePath
        /// </summary>
        /// <param name="FilePath"></param>
        /// <param name="AppendIfExists">If true, will keep the existing file and just append files. Otherwise will reset the file if it exists already</param>
        /// <param name="delimiter">The delimiter for records that get added</param>
        /// <param name="HeaderList"></param>
        public DelimitedDocumentWriter(string FilePath, char delimiter, bool AppendIfExists, string[] HeaderList = null)
            :this(HeaderList, FilePath )
        {
            Delimiter = delimiter;
            if (!AppendIfExists)
                internalReset();
        }
        /// <summary>
        /// Creates an instance of DelimiteddocumentWriter for adding records to the FilePath.
        /// <para>Restarts the file if it already exists</para>
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="Delimiter"></param>
        /// <param name="Headers"></param>
        public DelimitedDocumentWriter(string filepath, char Delimiter, string[] Headers)
            :this(filepath, Delimiter, false, Headers) { }
        private DelimitedDocumentWriter(string[] HeaderList, string FilePath)
        {
            _Header = HeaderList;
            filePath = FilePath;
            records = new List<DelimitedRecord>();
        }
        /// <summary>
        /// Creates an instance of DelimiteddocumentWriter for adding records to the FilePath with the default delimiter
        /// </summary>
        /// <param name="Filepath"></param>
        /// <param name="AppendIfExists">If true, will keep the existing file and just append files. Otherwise will reset the file if it exists already</param>        
        /// <param name="HeaderList"></param>
        public DelimitedDocumentWriter(string Filepath, bool AppendIfExists, string[] HeaderList = null)
            : this(HeaderList, Filepath)
        {
            Delimiter = DefaultDelimiter;
            if (!AppendIfExists)
               internalReset();
        }
        /// <summary>
        /// Creates an instance of DelimiteddocumentWriter for adding records to the FilePath with the default delimiter
        /// <para>Resets the file if it exists already.</para>
        /// </summary>
        /// <param name="FilePath"></param>        
        /// <param name="HeaderList"></param>
        public DelimitedDocumentWriter(string FilePath, string[] HeaderList = null)
            :this(FilePath, false, HeaderList) { }
        string[] _Header;
        /// <summary>
        /// Batch size - when the number of records added exceeds this value, they will be added to the file.
        /// <para>Default value is 50000</para>
        /// </summary>
        public int MaxBatchSize = 50000;
        /// <summary>
        /// Delimiter for records when writing
        /// </summary>
        public char Delimiter;
        /// <summary>
        /// The default delimiter for new DocumentWriters.
        /// Default value is set to '|'
        /// </summary>
        public const char DefaultDelimiter = '|';
        /// <summary>
        /// Merges the two DelimitedRecords and adds them to the documentWriter
        /// </summary>
        /// <param name="record1"></param>
        /// <param name="record2"></param>
        public void AddRecordMerge(DelimitedRecord record1, DelimitedRecord record2)
        {
            //List<string> t = record1.GetContent();
            //t.AddRange(record2.GetContent());
            //DelimitedRecord temp = new DelimitedRecord(t.ToArray(), Delimiter);
            DelimitedRecord temp = DelimitedRecord.Merge(record1, record2);            
            AddRecord(temp);
        }
        /// <summary>
        /// Merges multiple delimited records together and adds them to the document output
        /// </summary>
        /// <param name="toMerge"></param>
        public void AddRecordMerge(params DelimitedRecord[] toMerge)
        {
            if (toMerge.Length == 0)
                throw new ArgumentException(nameof(toMerge), "No Records to add!");
                                    
            DelimitedRecord work = toMerge[0];
            for(int i = 1; i< toMerge.Length; i++)
            {
                work = DelimitedRecord.Merge(work, toMerge[i]);
            }
            AddRecord(work);
        }
        /// <summary>
        /// Adds the delimited record to the DocumentWriter
        /// </summary>
        /// <param name="record"></param>
        public void AddRecord(DelimitedRecord record)
        {
            record.ChangeDelimiter(Delimiter);
            records.Add(record);
            if (records.Count >= MaxBatchSize)
                Flush();
        }
        /// <summary>
        /// Add multiple delimited records at one time
        /// </summary>
        /// <param name="recordBatch"></param>
        public void AddRecordBatch(params DelimitedRecord[] recordBatch)
        {
            foreach(var record in recordBatch)
            {
                record.ChangeDelimiter(Delimiter);
            }
            records.AddRange(recordBatch);
            if (records.Count >= MaxBatchSize)
                Flush();
        }
        /// <summary>
        /// Ensures that all records that have been added to the documentWriter get added to the actual file.
        /// </summary>
        public void Dispose()
        {
            if(records != null && records.Count > 0)
                Flush();
        }
        /// <summary>
        /// If set to true, the document will be created with the header created
        /// </summary>
        public bool HasHeader { get { return _Header != null && _Header.Length > 0; } }
        /// <summary>
        /// Adds all of the records that have been added to the file.
        /// <para>Also clears the records from the internal list.</para>
        /// </summary>
        public void Flush() { Flush(HasHeader); }
        private void Flush(bool includeHeader)
        {
            List<string> content = new List<string>();
            if (!System.IO.File.Exists(filePath))
            {
                if (includeHeader)
                {
                    content.Add(GetHeader());
                    System.IO.File.WriteAllLines(filePath, content);
                    content.Clear();
                }
                else
                {
                    System.IO.File.Create(filePath);
                }
            }
            foreach(var record in records)
            {
                content.Add(record.ToString());
            }
            System.IO.File.AppendAllLines(filePath, content);
            records.Clear();
        }
        private string GetHeader()
        {
            return string.Join(Delimiter.ToString(), _Header);
        }
        private void internalReset()
        {
            //file should not exist from constructor
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }
        /// <summary>
        /// Restarts the file
        /// </summary>
        /// <param name="IncludeHeader"></param>
        public void Reset(bool? IncludeHeader = null)
        {
            if(System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
            if (IncludeHeader ?? false && HasHeader)
            {
                List<string> temp = new List<string>();
                temp.Add(GetHeader());
                System.IO.File.WriteAllLines(filePath, temp);
            }
            else
                System.IO.File.Create(filePath);
        }
    }
}
