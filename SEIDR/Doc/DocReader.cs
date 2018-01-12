using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SEIDR.Doc
{
    /// <summary>
    /// Allows reading a delimited or fixed width file based on meta data. <para>
    /// Includes paging for working with large files in batches
    /// </para>
    /// </summary>
    public class DocReader:IEnumerable<DocRecord>, IDisposable
    {
        FileStream fs;
        StreamReader sr;
        DocMetaData md;
        /// <summary>
        /// Columns associated with the meta data.
        /// </summary>
        public DocRecordColumnCollection Columns => md.Columns;
        /// <summary>
        /// Full file path, from meta data
        /// </summary>
        public string FilePath => md.FilePath;
        /// <summary>
        /// File name associated with the doc
        /// </summary>
        public string FileName => Path.GetFileName(md.FilePath);
        /// <summary>
        /// File alias, from meta data
        /// </summary>
        public string Alias => md.Alias;
        bool firstLineHeader = true;
        /// <summary>
        /// Sets up a doc reader for DocRecord enumeration.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="firstLineIsHeader">When the columns are unknown, determines column names based on the first line. <para>
        /// If false, the first line will be used for the number of columns.</para> 
        /// If true, will also set the names of columns
        /// </param>
        public DocReader(DocMetaData info, bool firstLineIsHeader = true)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            if (!info.AccessMode.HasFlag(FileAccess.Read))
                throw new ArgumentException(nameof(info), "Not Configured for read mode");            
            md = info;
            firstLineHeader = firstLineIsHeader;
            SetupStream();
            
        }
        /// <summary>
        /// Reconfigures the Reader settings/paging information, using any changes to the DocMetaData that was provided
        /// </summary>
        public void ReConfigure() => SetupStream();
        private void SetupStream()
        {
            disposedValue = false;
            if (sr != null) { sr.Close(); sr = null; }
            if (fs != null) { fs.Close(); fs = null; }            
            fs = new FileStream(md.FilePath, FileMode.Open, md.AccessMode);
            sr = new StreamReader(fs, md.FileEncoding);
            if (PagePositions != null) PagePositions.Clear();
            else PagePositions = new List<long>();
            buffer = new char[md.PageSize];                                    
            int pp = md.SkipLines;
            if (md.HasHeader && md.HeaderConfigured)
                pp = md.SkipLines + 1;
            long position = 0;
            Tuple<bool, long> pageResult;            
            while((pageResult = ReadNextPage(ref pp, position)).Item1)
            {
                position = pageResult.Item2;
                if (pp == 0)
                    PagePositions.Add(position);
            }
            if (!md.Valid)
            {
                Dispose(true);
                throw new InvalidOperationException("Meta Data is not valid.");
            }
        }        
        List<long> PagePositions;
        
        Tuple<bool, long> ReadNextPage(ref int firstLine, long position)
        {
            //Position was 24, seems to have read from 30 again...?
            //fs.Position = position; //Move to specified position instead of storing strings. 
            fs.Seek(position, SeekOrigin.Begin);
            sr.DiscardBufferedData();    
            int x = sr.ReadBlock(buffer, 0, md.PageSize);            
            string content = /*working.ToString() +*/ new string(buffer, 0, x);
            IList<string> lines;
            if (md.Columns.FixedWidthMode && string.IsNullOrEmpty(md.LineEndDelimiter))
                lines = content.SplitOnString(md.Columns.MaxLength);
            else if (string.IsNullOrEmpty(md.LineEndDelimiter))
                throw new ArgumentException(nameof(md.LineEndDelimiter), "Line End delimiter is empty or null for a delimited document.");
            else
                lines = content.SplitOnString(md.LineEndDelimiter);

            bool end = x < md.PageSize;
            if (!end && content.IndexOf(md.LineEndDelimiter) < 0)
                throw new ArgumentException(nameof(md.PageSize), "Page Size is too small. Row starting at position " + position + " would not have a full row.");

            int endLine = end ? lines.Count : lines.Count - 1; 
            //for (int i = firstLine; i < endLine; i++){}
            if(firstLine == 0)
            {
                int i = 0;
                if (!md.HeaderConfigured)
                {
                    if (md.Columns.FixedWidthMode)
                        throw new ArgumentException(nameof(md.FixedWidthMode), "Columns are not set, but trying to use fixed width mode.");
                    if(!md.Delimiter.HasValue)
                        md.SetDelimiter(lines[0].GuessDelimiter());      
                    if(firstLineHeader)
                        md.AddDelimitedColumns(lines[0].Split(md.Delimiter.Value));
                    else
                    {
                        int hl = lines[0].Split(md.Delimiter.Value).Length;
                        for(int ti = 1; ti <= hl; ti++)
                        {
                            md.AddColumn("Column # " + ti);
                        }/*
                        string[] tempHeader = new string[lines[0].Split(md.Delimiter.Value).Length];
                        for(int ti= 1; ti <= tempHeader.Length; ti++)
                        {
                            tempHeader[ti - 1] = "Column # " + ti;
                        }
                        md.AddDelimitedColumns(tempHeader);
                        */
                    }
                    position = lines[0].Length + md.LineEndDelimiter.Length;
                    i++;
                    if (PagePositions.Count == 0)
                        PagePositions.Add(position);
                    else
                        PagePositions[0] = position; //Replace position 0.                    
                }
                //Move to next page.
                for(; i < endLine; i++)
                {
                    position += lines[i].Length + md.LineEndDelimiter.Length;
                    if (end && i == endLine - 1)
                        position -= md.LineEndDelimiter.Length;
                }
            }
            else
            {
                //This is slightly off... Maybe need to add 1?
                //Move position forward - get the very first line...
                for (int i = 0; i < firstLine && i < endLine; i++)
                {
                    position += lines[i].Length + md.LineEndDelimiter.Length;
                    if (end && i == endLine - 1)
                        position -= md.LineEndDelimiter.Length; //Need to do something with pages small pages
                }                
                if (firstLine > endLine)
                    firstLine -= endLine;
                else
                    firstLine = 0;                
                if(end)
                {
                    if (PagePositions.Count == 0)
                        PagePositions.Add(position);
                    else
                        PagePositions[0] = position;
                }
            }            
            if (end)
            {
                //done. Don't need to add more pages.
                return new Tuple<bool, long>(false, position);
            }            
            return new Tuple<bool, long>(true, position);
        }
        /// <summary>
        /// Sets up a basic delimited reader, assuming the file has headers starting on the first line.
        /// </summary>
        /// <param name="alias"></param>
        /// <param name="FilePath"></param>
        /// <param name="LineEnd">The line ending. If null, will use <see cref="Environment.NewLine"/></param>
        /// <param name="Delimiter">Column delimiter. If null, will try to guess when parsing, based on the content of the first line found.</param>
        public DocReader(string alias, string FilePath, string LineEnd = null, char? Delimiter = null)
        {
            md = new DocMetaData(FilePath, alias)
            {                
                HasHeader = true,
                SkipLines = 0,
                EmptyIsNull = true,                
            };
            if(Delimiter.HasValue)
                md.SetDelimiter(Delimiter.Value);

            md
                .SetLineEndDelimiter(LineEnd ?? Environment.NewLine)
                .SetFileAccess(FileAccess.Read)
                .SetFileEncoding(Encoding.Default);                
            SetupStream();
        }
        char[] buffer;
        public int PageCount => PagePositions.Count;

        public int LastPage => PagePositions.Count - 1;
        
        public IEnumerable<DocRecord> this[int pageNumber]
        {
            get
            {
                foreach (var record in GetPage(pageNumber))
                    yield return record;
            }
        }
        /// <summary>
        /// Attempts to grab a specific DocRecord off the specified page
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="pageLineNumber"></param>
        /// <returns></returns>
        public DocRecord this[int pageNumber, int pageLineNumber]
        {
            get
            {
                return GetPage(pageNumber)[pageLineNumber];
            }
        }
        
        /// <summary>
        /// 0 based page get
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        public IList<DocRecord> GetPage(int pageNumber)
        {
            if (sr == null)
                throw new InvalidOperationException("Not in a configured state. May have been disposed already");
            if (pageNumber < 0 || pageNumber >= PageCount)
                throw new ArgumentOutOfRangeException(nameof(pageNumber));
            string content;
            if (pageNumber == LastPage)
            {
                //fs.Position = PagePositions[pageNumber];
                fs.Seek(PagePositions[pageNumber], SeekOrigin.Begin);
                sr.DiscardBufferedData();
                int x = sr.ReadBlock(buffer, 0, md.PageSize);
                content = new string(buffer, 0, x);
            }
            else
            {
                long start = PagePositions[pageNumber];
                long end = PagePositions[pageNumber + 1];
                //fs.Position = start;
                fs.Seek(start, SeekOrigin.Begin);
                sr.DiscardBufferedData();
                int x = sr.ReadBlock(buffer, 0, (int)(end - start));
                content = new string(buffer, 0, x);
            }
            var lines = content.SplitOnString(md.LineEndDelimiter);
            List<DocRecord> LineRecords = new List<DocRecord>();
            foreach(var line in lines)
            {
                LineRecords.Add(Columns.ParseRecord(false, line));
            }
            return LineRecords;
        }
        public IEnumerator<DocRecord> GetEnumerator()
        {
            for(int pn = 0; pn < PageCount; pn ++)
            {
                foreach (var record in GetPage(pn))
                    yield return record;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    PagePositions.Clear();
                    PagePositions = null;
                }
                if (sr != null)
                {
                    sr.Close();
                    sr = null;
                }
                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }
                
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~DocReader()
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
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
