using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SEIDR.Doc
{
    /// <summary>
    /// Inforamtion about a Seidr Index
    /// </summary>
    public class DelimitedFileIndexInfo
    {
        /// <summary>
        /// Extension for index files used by DelimitedIndex class
        /// </summary>
        public const string FILE_EXTENSION = ".sidx";
        /// <summary>
        /// Path storing the location of the index file
        /// </summary>
        public string IndexFile { get; private set; }
        /// <summary>
        /// Path to the file the index is built for
        /// </summary>
        public string FilePath { get; private set; }
        /// <summary>
        /// Column delimiter for the file being indexed
        /// </summary>
        public char? FileDelimiter { get; private set; }
        /// <summary>
        /// Columns stored by the index in order
        /// </summary>
        public string[] Columns { get; private set; }
        private DelimitedFileIndexInfo(string IndexPath)
        {
            string temp = IndexPath;
            if (temp.EndsWith("\\"))
            {
                temp += Path.GetFileName(FilePath);
            }
            if(!temp.EndsWith(FILE_EXTENSION))
                temp += FILE_EXTENSION; 
            IndexFile = temp;
        }
        /// <summary>
        /// Constructor with delimiter specified
        /// </summary>
        /// <param name="RawFilePath"></param>
        /// <param name="IndexFilePath"></param>
        /// <param name="Delimiter"></param>
        /// <param name="ColumnList"></param>
        public DelimitedFileIndexInfo(string RawFilePath, string IndexFilePath, char? Delimiter, params string[] ColumnList)
            :this(IndexFilePath)
        {
            FilePath = RawFilePath;            
            Columns = ColumnList;
            FileDelimiter = Delimiter;
        }
        /// <summary>
        /// Creates an index info class without the delimiter specified
        /// </summary>
        /// <param name="RawFilePath"></param>
        /// <param name="IndexFilePath"></param>
        /// <param name="ColumnList"></param>
        public DelimitedFileIndexInfo(string RawFilePath, string IndexFilePath, params string[] ColumnList)
            :this(IndexFilePath)
        {
            FilePath = RawFilePath;
            Columns = ColumnList;
            FileDelimiter = null;
        }
    }
    /// <summary>
    /// Class representing an index for a Delimited Document. Takes an indexFile for a constructor
    /// </summary>
    public class DelimitedIndex: IEnumerable<DelimitedRecord>, IDisposable
    {
        /// <summary>
        /// For reading from the actual index file
        /// </summary>
        string[] Header; 
        /// <summary>
        /// Info describing the file being indexed. Meta data for the index file basically
        /// </summary>
        DelimitedFileIndexInfo _IndexInfo;
        /// <summary>
        /// Returns the name of the file being used for the index. Note that it should always have the extension ".sidx",
        /// unless the file was created outside of this class
        /// </summary>
        public string IndexFile { get { return _IndexInfo.IndexFile; } }
        const string PAGE_NUMBER = "_PAGE";
        const string RECORD_NUMBER = "_RECORD"; //set these values when creating a filter
        Dictionary<int, List<int>> FilterPlacement = null;//foreach page, the records that fit into the filter.    
        /// <summary>
        /// Sets up the filter on the index so that you can iterate on records that meet the conditions.        
        /// </summary>   
        /// <remarks>If the filter can be parsed as a double, it will look for an exact match -  otherwise just that it's contained
        /// <para>If the value is null, it will check for empty or white space values.
        /// </para></remarks>        
        /// <param name="filters">Column:Value pair to use for filtering the values stored in the index</param>
        public void SetFilter(Dictionary<string, string> filters)
        {
            if (FilterPlacement != null)
                FilterPlacement.Clear();
            else
                FilterPlacement = new Dictionary<int, List<int>>();
            //use the dictionary of Header:value pairs to filter the index content and get the records to be used.
            using(DelimitedDocumentReader doc = new DelimitedDocumentReader(_IndexInfo.IndexFile))
            {
                var page = doc.CurrentPage;
                while(page != null)
                {
                    FilterPlacement[doc.Page] = new List<int>();
                    for(int i= 0; i < page.Length; i++)
                    {
                        var record = page[i];
                        bool success = true;
                        foreach(var kv in filters)
                        {
                            double test;
                            if(kv.Value == null)
                            {
                                if (!string.IsNullOrWhiteSpace(record[kv.Value]))
                                {
                                    success = false;
                                    break;
                                }
                            }
                            else if(Double.TryParse(kv.Value, out test))
                            {
                                if(record[kv.Value] != kv.Value)
                                {
                                    success = false;
                                    break;
                                }
                            }                           
                            else if(record[kv.Key].Contains( kv.Value))
                            {
                                success = false;
                                break;
                            }
                        }
                        if (success)
                        {
                            FilterPlacement[doc.Page].Add(i);
                        }
                    }
                    page = doc.GetNextPage();
                }
            }
        }
        /// <summary>
        /// Sets up a delimited index based on the IndexInfo for setup
        /// </summary>
        /// <param name="setupInfo"></param>
        public DelimitedIndex(DelimitedFileIndexInfo setupInfo)
        {
            List<string> temp = new List<string>(setupInfo.Columns);
            if (!temp.Contains(PAGE_NUMBER))
                temp.Add(PAGE_NUMBER);
            if(!temp.Contains(RECORD_NUMBER))
                temp.Add(RECORD_NUMBER);
            _IndexInfo = setupInfo;
            Header = temp.ToArray();
            reader = new DelimitedDocumentReader(setupInfo.FilePath, setupInfo.FileDelimiter);
            
        }
        /// <summary>
        /// Creates a delimited Index object using the index's actual file
        /// </summary>
        /// <param name="IndexFile"></param>
        public DelimitedIndex(string IndexFile)
        {
            string FileLocation;
            
            using (DelimitedDocumentReader indexDoc = new DelimitedDocumentReader(IndexFile, '|', 2))
            {
                FileLocation = indexDoc.SkippedLines[0];
                char? Delim = null;
                string x = indexDoc.SkippedLines[1];
                if (x != null)
                    Delim = x[0];
                Header = indexDoc.GetHeader();
                
                reader = new DelimitedDocumentReader(FileLocation, Delim);
                
                _IndexInfo = new DelimitedFileIndexInfo(FileLocation, IndexFile, Delim, Header.Take(Header.Length - 2).ToArray());
            }
        }        
        

        /// <summary>
        /// Disposer
        /// </summary>
        public void Dispose()
        {
            if (reader != null)
                reader.Dispose();            
        }
        DelimitedDocumentReader reader;        
        /// <summary>
        /// Iterate through the indexed records, or through everything if it has not had a filter set
        /// </summary>
        /// <returns></returns>
        public IEnumerator<DelimitedRecord> GetEnumerator()
        {
            if (FilterPlacement == null || FilterPlacement.Count == 0)
            {
                foreach (var r in reader)
                    yield return r;
                yield break;
            }                
            foreach(var kv in FilterPlacement)
            {
                var p = reader.GetPage(kv.Key);
                foreach(var rIdx in kv.Value)
                {
                    yield return p[rIdx];
                }
            }            
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (FilterPlacement == null || FilterPlacement.Count == 0)
            {
                foreach (var r in reader)
                    yield return r;
                yield break;
            }
            foreach (var kv in FilterPlacement)
            {
                var p = reader.GetPage(kv.Key);
                foreach (var rIdx in kv.Value)
                {
                    yield return p[rIdx];
                }
            }
        }

        /// <summary>
        /// Checks if the index already exists - if it exists but is older than the actual file's last update, a new index will be created.
        /// <para>If a new index is created, the index will be reset, but otherwise it will stay at the same position of the index file.</para>
        /// <para>Will need to re-set the filter if a new index is created</para>
        /// </summary>
        /// <returns>False if the index did not need to be created - true if a new index was created</returns>
        public bool CreateIndex()
        {
            if (System.IO.File.Exists(_IndexInfo.IndexFile))
            {
                var info = new System.IO.FileInfo(reader.FilePath);
                var idxInfo = new System.IO.FileInfo(_IndexInfo.IndexFile);
                if (info.LastWriteTime <= idxInfo.LastWriteTime)
                    return false; //Do not need to recreate index
            }
            reader.Reset();
            using (DelimitedDocumentWriter ddw = new DelimitedDocumentWriter(_IndexInfo.IndexFile, Header))
            {
                var p = reader.CurrentPage;
                while (p != null)
                {
                    for(int i = 0; i < p.Length; i++)
                    {
                        var record  = p[i];
                        List<string> content = new List<string>();
                        foreach(var header in Header)
                        {
                            content.Add( record[header]); //Add the index's columns to the record for using to create filters
                        }
                        content.Add(reader.Page.ToString());
                        content.Add(i.ToString());
                        ddw.AddRecord(new DelimitedRecord(Header, content.ToArray(), content.Count));
                    }
                    p = reader.GetNextPage();
                }
            }
            FilterPlacement = null;
            reader.Reset();     
            return true;
        }
    }
}
