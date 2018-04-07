﻿using System;
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
            if (!md.FixedWidthMode && string.IsNullOrEmpty(md.LineEndDelimiter))
                throw new InvalidOperationException("Cannot Do Delimited document without a line End delimiter.");
            if (md.FixedWidthMode && !md.HeaderConfigured)
                throw new InvalidOperationException("Cannot do fixed width without header configured already.");
            if(md.FixedWidthMode && md.PageSize < Columns.MaxLength)
                throw new InvalidOperationException($"Page Size({md.PageSize}) is smaller than FixedWidth line Length ({Columns.MaxLength})");

            disposedValue = false;
            if (sr != null) { sr.Close(); sr = null; }
            if (fs != null) { fs.Close(); fs = null; }            
            fs = new FileStream(md.FilePath, FileMode.Open, md.AccessMode);
            sr = new StreamReader(fs, md.FileEncoding);
            if (Pages == null)
                Pages = new List<PageHelper>();
            else
                Pages.Clear();

            buffer = new char[md.PageSize];                                    
            int pp = md.SkipLines;
            if (md.HasHeader && md.HeaderConfigured)
                pp = md.SkipLines + 1;
            long position = 0;
            while(ReadNextPage(ref position, ref pp)) { }
            if (!md.Valid)
            {
                Dispose(true);
                throw new InvalidOperationException("Meta Data is not valid.");
            }
        }                
        List<PageHelper> Pages;
        public class PageHelper
        {
            /// <summary>
            /// Start character position of this page.
            /// </summary>
            public readonly long StartPosition;
            /// <summary>
            ///  End character position in the file of this page
            /// </summary>
            public readonly long EndPosition;
            ///<summary>Percentage filled of page size used</summary>
            public readonly decimal Fullness;
            /// <summary>
            /// Size of the page.
            /// </summary>
            public readonly int Length;
            public PageHelper(long position, long endPosition, int pageSize)
            {
                StartPosition = position;
                EndPosition = endPosition;                
                Fullness = ((decimal)(endPosition - position)) / pageSize;
                Length = (int)(endPosition - position);
            }

        }
        bool ReadNextPage(ref long startPosition, ref int skipLine)
        {
            fs.Seek(startPosition, SeekOrigin.Begin);
            sr.DiscardBufferedData();
            int x = sr.ReadBlock(buffer, 0, md.PageSize);
            if (x == 0)
                return false; //empty, nothing to do. Shouldn't happen, though, since startPosition should be the previous end position after removing the end...
            bool end = x < md.PageSize;
            
            string content = /*working.ToString() +*/ new string(buffer, 0, x);
            IList<string> lines = null;

            bool fwnl = md.Columns.FixedWidthMode && string.IsNullOrEmpty(md.LineEndDelimiter);            
            int endLine;
            int removed = 0;
            long endPosition;
            if (!fwnl)
            {
                lines = content.SplitOnString(md.LineEndDelimiter);
                if (end)
                {                    
                    endPosition = startPosition + x;
                }
                else
                {                    
                    int temp = lines.Count - 1;
                    if (temp == 0)
                        throw new Exception("BufferSize too small");
                    removed = lines[temp].Length;
                    lines.RemoveAt(temp);
                    endPosition = startPosition + x - removed;
                }                
                endLine = lines.Count;
            }
            else
            {
                //No newLine, just dividing by positions....
                if (end)
                {
                    endPosition = startPosition + x;
                    endLine = x / md.Columns.MaxLength;
                }
                else
                {
                    removed = md.Columns.MaxLength % md.PageSize;
                    content = new string(buffer, 0, x - removed);
                    endPosition = startPosition + x - removed;
                    endLine = content.Length / md.Columns.MaxLength;
                }                
            }
            if(skipLine > 0)
            {
                if(skipLine >= endLine)
                {
                    startPosition = endPosition;
                    skipLine -= endLine;
                    return !end;
                }
                //endLine > skipLine, remove skipLine # records, then continue to next section...
                if (fwnl)
                {
                    startPosition += skipLine * md.Columns.MaxLength; //Move forward by skipLine lines
                }
                else
                {
                    for(int i = 0; i < skipLine; i ++)
                    {
                        startPosition = startPosition + lines[i].Length + md.LineEndDelimiter.Length;
                    }
                    skipLine = 0;
                    return true; //re-read from the correct starting position instead of trying to mess with the list.
                }
                
            }
            if (!md.HeaderConfigured)
            {
                string firstLine = lines[0];
                //must be delimited in this section...                
                if (!md.Delimiter.HasValue)
                    md.SetDelimiter(firstLine.GuessDelimiter());
                string[] firstLineS = firstLine.Split(md.Delimiter.Value);
                if (md.HasHeader)
                {
                    md.AddDelimitedColumns(firstLineS);
                    startPosition += firstLine.Length + md.LineEndDelimiter.Length; //move forward by a line...
                    //lines.RemoveAt(0); // ... Probably don't really care about this at this point actually.
                }
                else
                {
                    int hl = firstLineS.Length;
                    for (int ti = 1; ti <= hl; ti++)
                    {
                        md.AddColumn("Column # " + ti);
                    }
                }
            }
            
            Pages.Add(new PageHelper(startPosition, endPosition, md.PageSize));
            startPosition = endPosition;
            return !end;
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
        public int PageCount => Pages.Count;

        public int LastPage => Pages.Count - 1;
        
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

        int lastPage = -2;
        
        /// <summary>
        /// Gets the content of the specified 'page'
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        public IList<DocRecord> GetPage(int pageNumber)
        {
            if (sr == null)
                throw new InvalidOperationException("Not in a configured state. May have been disposed already");
            if (pageNumber < 0 || pageNumber > LastPage)
                throw new ArgumentOutOfRangeException(nameof(pageNumber));
            string content;
            int x;
            PageHelper p = Pages[pageNumber];
            if(pageNumber == lastPage + 1)
            {
                x = sr.ReadBlock(buffer, 0, p.Length); //Need discard? Shouldn't, since not seeking
            }
            else
            {
                fs.Seek(p.StartPosition, SeekOrigin.Begin);
                sr.DiscardBufferedData();
                x = sr.ReadBlock(buffer, 0, p.Length);                
            }
            content = new string(buffer, 0, x);
            lastPage = pageNumber;
            IList<string> lines;
            if (string.IsNullOrEmpty(md.LineEndDelimiter))
            {
                lines = content.SplitOnString(md.Columns.MaxLength);
            }
            else
            {
                lines = content.SplitOnString(md.LineEndDelimiter);
            }
            List<DocRecord> LineRecords = new List<DocRecord>();
            lines.ForEachIndex((line, idx) =>
            {
                var rec = Columns.ParseRecord(md.CanWrite, line);
                if (rec == null)
                {
                    System.Diagnostics.Debug.WriteLine("Empty Record found! Page: " + pageNumber + ", LineNumber: " + idx);
                    if (idx == lines.Count)
                    {
                        System.Diagnostics.Debug.WriteLine("Last Line of page - Skipping empty record.");
                        return;
                    }
                }
                LineRecords.Add(rec);
            }, 1, 1);
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
                    if (Pages != null)
                    {
                        Pages.Clear();
                        Pages = null;
                    }                   
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
