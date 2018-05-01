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
    public class DocReader : DocReader<DocRecord>
    {

        /// <summary>
        /// Basic constructor, no meta data configured yet.
        /// </summary>
        public DocReader() : base() { }
        /// <summary>
        /// Sets up a doc reader for DocRecord enumeration.
        /// </summary>
        /// <param name="info"></param>
        public DocReader(DocMetaData info) : base(info) { }
        /// <summary>
        ///
        /// </summary>
        /// <param name="alias"></param>
        /// <param name="FilePath"></param>
        /// <param name="LineEnd"></param>
        /// <param name="Delimiter"></param>
        /// <param name="pageSize"></param>
        public DocReader(string alias, string FilePath, char? Delimiter = null, string LineEnd = null,  int? pageSize = null)
            :base(alias, FilePath,  Delimiter, LineEnd, pageSize) { }
        /// <summary>
        /// Creates a DocReader for the specified file by combining Directory and FileName
        /// </summary>
        /// <param name="alias"></param>
        /// <param name="Directory"></param>
        /// <param name="FileName"></param>
        /// <param name="LineEnd"></param>
        /// <param name="Delimiter"></param>
        /// <param name="pageSize"></param>
        public DocReader(string alias, string Directory, string FileName, char? Delimiter = null, string LineEnd = null, int? pageSize = null)
            : this(alias, Path.Combine(Directory, FileName), Delimiter, LineEnd, pageSize) { }

        /// <summary>
        /// Gets the DocRecords from the page.
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="useCache">If true and a specific page is accessed more than once in a row, will immediately return a cached reference to the List.</param>
        /// <returns></returns>
        public override List<DocRecord> GetPage(int pageNumber, bool useCache = true)
        {
            if (pageNumber == lastPage && CurrentPage != null && useCache)
                return CurrentPage;
            var lines = GetPageLines(pageNumber);
            List<DocRecord> LineRecords = new List<DocRecord>();
            lines.ForEachIndex((line, idx) =>
            {
                var rec = Columns.ParseRecord(md.CanWrite, line);
                if (rec == null)
                {
                    System.Diagnostics.Debug.WriteLine("Empty Record found! Page: " + pageNumber + ", LineNumber: " + idx);
                    /*
                    if (idx == lines.Count)
                    {
                        System.Diagnostics.Debug.WriteLine("Last Line of page - Skipping empty record.");
                        return;
                    }*/
                }
                LineRecords.Add(rec);
            }, 1, 1);
            CurrentPage = LineRecords;
            return LineRecords;
        }
    }

    /// <summary>
    /// Allows reading a delimited or fixed width file based on meta data. <para>
    /// Includes paging for working with large files in batches
    /// </para>
    /// </summary>
    public class DocReader<ReadType>:IEnumerable<ReadType>, IDisposable where ReadType:DocRecord, new()
    {

        FileStream fs;
        StreamReader sr;
        protected DocMetaData md;
        /// <summary>
        /// Underlying MetaData
        /// </summary>
        public DocMetaData MetaData => md;
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

        /// <summary>
        /// Sets up a doc reader for DocRecord enumeration.
        /// </summary>
        /// <param name="info"></param>
        public DocReader(DocMetaData info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            if (!info.AccessMode.HasFlag(FileAccess.Read))
                throw new ArgumentException(nameof(info), "Not Configured for read mode");
            md = info;
            SetupStream();
        }
        /// <summary>
        /// Total number of records in the file after set up.
        /// <para>If the file cannot be parsed, or has not yet been parsed, the value will be -1.</para>
        /// </summary>
        public long RecordCount { get; private set; } = 0;
        /// <summary>
        /// Reconfigures the Reader settings/paging information, using any changes to the DocMetaData that was provided
        /// </summary>
        public void ReConfigure() => SetupStream();
        private void SetupStream()
        {
            if(md.LineEndDelimiter != null && !md.MultiLineEndDelimiter.Contains(md.LineEndDelimiter))
                    md.AddMultiLineEndDelimiter(md.LineEndDelimiter);
            else if (md.ReadWithMultiLineEndDelimiter && md.LineEndDelimiter == null)
            {
                md.Columns.LineEndDelimiter = md.MultiLineEndDelimiter[0];
            }
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
            CurrentPage = null;
            int extra = 0; //Add extra space for buffer so we don't have to discard buffer when going from one page to the next while avoiding including the ending newLine information (because we don't need it in the output)
            if (md.ReadWithMultiLineEndDelimiter)
            {
                extra = md.MultiLineEndDelimiter.Max(ml => ml.Length);
            }
            else if (md.LineEndDelimiter != null)
                extra = md.LineEndDelimiter.Length;
            buffer = new char[md.PageSize + extra];
            int pp = md.SkipLines;
            if (md.HasHeader && md.HeaderConfigured)
                pp = md.SkipLines + 1;
            long position = 0;
            while(SetupPageMetaData(ref position, ref pp)) { }
            if (!md.Valid)
            {
                RecordCount = -1;
                Dispose(true);
                throw new InvalidOperationException("Meta Data is not valid.");
            }
            RecordCount = Pages.Sum(p => (long)p.RecordCount);
        }
        List<PageHelper> Pages;
        /// <summary>
        /// Paging information for reading from the file.
        /// </summary>
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
            /// <summary>
            /// Number of records in page
            /// </summary>
            public readonly int RecordCount;
            /// <summary>
            /// Helper Class for DocReader to divide a file into segments for processing.
            /// </summary>
            /// <param name="position"></param>
            /// <param name="endPosition"></param>
            /// <param name="pageSize"></param>
            /// <param name="recordCount"></param>
            public PageHelper(long position, long endPosition, int pageSize, int recordCount)
            {
                StartPosition = position;
                EndPosition = endPosition;
                Fullness = ((decimal)(endPosition - position)) / pageSize;
                Length = (int)(endPosition - position);
                RecordCount = recordCount;
            }
           

        }
        public PageHelper GetPageInfo(int page) => Pages[page];
        bool SetupPageMetaData(ref long startPosition, ref int skipLine)
        {
            fs.Seek(startPosition, SeekOrigin.Begin);
            sr.DiscardBufferedData();
            int x = sr.ReadBlock(buffer, 0, md.PageSize);
            if (x == 0)
                return false; //empty, nothing to do. Shouldn't happen, though, since startPosition should be the previous end position after removing the end...
            bool end = x < md.PageSize;
            string content = /*working.ToString() +*/ new string(buffer, 0, x);
            IList<string> lines = null;

            bool fixWidth_NoNewLine = md.Columns.FixedWidthMode && string.IsNullOrEmpty(md.LineEndDelimiter) && !md.ReadWithMultiLineEndDelimiter;
            bool removeHeaderFromRecordCount = false;
            int endLine;
            int removed = 0;
            int lastNLSize = 0; //Size of the NewLine Delimiter if the page ends on a NewLine delimiter. //Note: Potential for extra line if we have multiple line ends and end a page between an \r and \n, but that would be adding an empty record (null)
            long endPosition;
            if (!fixWidth_NoNewLine)
            {
                if (md.ReadWithMultiLineEndDelimiter)
                    lines = content.Split(md.MultiLineEndDelimiter, StringSplitOptions.None);
                else
                    lines = content.SplitOnString(md.LineEndDelimiter);

                if (end && lines[lines.Count-1].Trim() != string.Empty)
                {
                    endPosition = startPosition + x;
                    if (md.ReadWithMultiLineEndDelimiter)
                    {
                        foreach(string delim in md.MultiLineEndDelimiter)
                        {
                            if (content.EndsWith(delim))
                            {
                                lastNLSize = delim.Length;
                                break;
                            }
                        }
                    }
                    else if (content.EndsWith(md.LineEndDelimiter))
                        lastNLSize = md.LineEndDelimiter.Length;


                    endLine = lines.Count;
                }
                else
                {
                    int temp = lines.Count - 1;
                    if (temp == 0)
                        throw new Exception("BufferSize too small - may be missing LineEndDelimiter");
                    removed = lines[temp].Length;
                    //lines.RemoveAt(temp);
                    endPosition = startPosition + x - removed; //doesn't include the newline...whatever it may have been.
                    if (md.ReadWithMultiLineEndDelimiter)
                    {
                        int s = x - removed;
                        foreach(string delim in md.MultiLineEndDelimiter) //first multi line delimiter that would match and cause a split - take its length.
                        {
                            if(content.Substring(s - delim.Length, delim.Length) == delim)
                            {
                                lastNLSize = delim.Length;
                                break;
                            }
                        }
                    }
                    else
                        lastNLSize = md.LineEndDelimiter.Length;

                    endLine = lines.Count - 1;
                }
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
                if (fixWidth_NoNewLine)
                {
                    startPosition += skipLine * md.Columns.MaxLength; //Move forward by skipLine lines
                }
                else
                {
                    int posHelper = 0; //offset from content[0]
                    for(int i = 0; i < skipLine; i ++)
                    {
                        if (md.ReadWithMultiLineEndDelimiter)
                        {
                            int temp = lines[i].Length;
                            foreach(string delim in md.MultiLineEndDelimiter)
                            {
                                if(content.Substring(posHelper + temp, delim.Length) == delim)
                                {
                                    temp += delim.Length;
                                    break;
                                }
                            }
                            posHelper += temp;
                            startPosition += temp;
                        }
                        else
                        {
                            startPosition = startPosition + lines[i].Length + md.LineEndDelimiter.Length;
                        }
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
                    md.SetDelimiter(lines.GuessDelimiter());
                string[] firstLineS = firstLine.Split(md.Delimiter.Value);
                if (md.HasHeader)
                {
                    removeHeaderFromRecordCount = true;
                    md.AddDelimitedColumns(firstLineS);
                    if (md.ReadWithMultiLineEndDelimiter)
                    {
                        int temp = firstLine.Length;
                        foreach (string delim in md.MultiLineEndDelimiter)
                        {
                            if(content.Substring(temp, delim.Length) == delim)
                            {
                                temp += delim.Length;
                                break;
                            }
                        }
                        startPosition += temp;
                    }
                    else
                        startPosition += firstLine.Length + md.LineEndDelimiter.Length; //move forward by a line...

                    if (startPosition > endPosition - lastNLSize)
                        return true; //don't add a page, instead go to next page so we can start adding lines together.
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
            int recordCount = endLine - (removeHeaderFromRecordCount ? 1 : 0);
            Pages.Add(new PageHelper(startPosition, endPosition - lastNLSize, md.PageSize, recordCount: recordCount));
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
        /// <param name="pageSize">Overwrites the meta data page size of the inferred DocMetaData</param>
        public DocReader(string alias, string FilePath,  char? Delimiter = null, string LineEnd = null, int? pageSize = null)
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
                .SetFileAccess(FileAccess.Read) //allow multiple docReaders to access the same file.
                .SetFileEncoding(Encoding.Default);
            if(pageSize != null)
                md.SetPageSize(pageSize.Value);

            SetupStream();
        }
        /// <summary>
        /// Basic constructor, no meta data configured yet.
        /// </summary>
        public DocReader()
        {
            sr = null;
        }
        /// <summary>
        /// Configures the DocReader to use the passed meta data.
        /// </summary>
        /// <param name="metaData"></param>
        public void Configure(DocMetaData metaData)
        {

            if (metaData == null)
                throw new ArgumentNullException(nameof(metaData));
            if (!metaData.AccessMode.HasFlag(FileAccess.Read))
                throw new ArgumentException(nameof(metaData), "Not Configured for read mode");

            Dispose();
            md = metaData;
            SetupStream();
        }
        char[] buffer;
        /// <summary>
        /// Count of pages based on configuration.
        /// </summary>
        public int PageCount => Pages?.Count ?? 0;

        int LastPage => Pages.Count - 1;
        #region records, indexer logic

        /// <summary>
        /// Checks the page that the specified line number (file wide, from the start after skipping any lines/header)
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <returns>Tuple indicating the page number and position of the line.</returns>
        public Tuple<int, int> CheckPage(long lineNumber)
        {
            for (int i = 0; i < Pages.Count; i++)
            {
                PageHelper p = Pages[i];
                if (lineNumber < p.RecordCount)
                    return new Tuple<int, int>(i, (int)lineNumber);
                lineNumber -= p.RecordCount;
            }
            throw new ArgumentOutOfRangeException(nameof(lineNumber));
        }
        /// <summary>
        /// Gets the overall line number based on number of records per page used.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageLine"></param>
        /// <returns></returns>
        public long CheckLine(int page, int pageLine)
        {
            long LineNumber = 0;
            for (int i = 0; i < page; i ++)
                LineNumber += Pages[i].RecordCount;
            return LineNumber + pageLine; //portion of page = page used
        }
        /// <summary>
        /// Enumerates the record contents from the spefied line numbers
        /// </summary>
        /// <param name="Lines"></param>
        /// <returns></returns>
        public IEnumerable<string> GetSpecificLines(IEnumerable<long> Lines)
        {
            var pp = GetPagePositions(Lines);
            for (int i = 0; i < pp.Length; i++)
            {
                var pl = GetPageLines(i);
                foreach (var p in pp[i])
                {
                    yield return pl[p];
                }
            }
        }
        /// <summary>
        /// Enumerates specific records from the file, based on line number
        /// </summary>
        /// <param name="Lines"></param>
        /// <returns></returns>
        public IEnumerable<ReadType> GetSpecificRecords(IEnumerable<long> Lines)
        {
            var pp = GetPagePositions(Lines);
            for (int i = 0; i < pp.Length; i++)
            {
                var pl = GetPageLines(i);
                foreach (var p in pp[i])
                    yield return this[i, p];
            }
        }
        /// <summary>
        /// Gets the record from the specified position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public ReadType this[long position]
        {
            get
            {
                var pl = CheckPage(position);
                return this[pl.Item1, pl.Item2];
            }
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="lineNumberList"></param>
        /// <returns></returns>
        public List<int>[] GetPagePositions(params long[] lineNumberList)
            => GetPagePositions((IEnumerable<long>)lineNumberList);
        /// <summary>
        /// Gets all the line numbers used for a given line Number
        /// </summary>
        /// <param name="lineNumberList"></param>
        /// <returns></returns>
        public List<int>[] GetPagePositions(IEnumerable<long> lineNumberList)
        {
            List<int>[] ret = new List<int>[Pages.Count];
            for (int i = 0; i < Pages.Count; i++)
            {
                ret[i] = new List<int>();
            }
            foreach (var ln in lineNumberList)
            {
                var tpl = CheckPage(ln);
                ret[tpl.Item1].Add(tpl.Item2);
            }
            return ret;
        }

        public IEnumerable<ReadType> this[int pageNumber]
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
        public ReadType this[int pageNumber, int pageLineNumber]
        {
            get
            {
                return GetPage(pageNumber)[pageLineNumber];
            }
        }

        /// <summary>
        /// The last pageNumber that was used for grabbing content from a file
        /// </summary>
        protected int lastPage = -2;

        /// <summary>
        /// Returns an IList of strings. May be either an <see cref="Array"/> of strings(MultiLineEnd Delimiter mode..) or a <see cref="List{string}"/>.
        /// <para>May be more useful when dealing with a class that inherits from <see cref="DocRecord"/></para>
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        public IList<string> GetPageLines(int pageNumber)
        {
            if (sr == null)
                throw new InvalidOperationException("Not in a configured state. May have been disposed already");
            if (pageNumber < 0 || pageNumber > LastPage)
                throw new ArgumentOutOfRangeException(nameof(pageNumber));
            string content;
            int x;
            int drop = 0;
            PageHelper p = Pages[pageNumber];
            if (pageNumber == lastPage + 1)
            {
                drop = (int)(p.StartPosition - Pages[lastPage].EndPosition); //If ended on a newline which gets dropped.
                x = sr.ReadBlock(buffer, 0, p.Length + drop); //Need discard? Shouldn't, since not seeking
            }
            else
            {
                fs.Seek(p.StartPosition, SeekOrigin.Begin);
                sr.DiscardBufferedData();
                x = sr.ReadBlock(buffer, 0, p.Length);
            }
            if (x > p.Length)
                x = p.Length; //throw away extra characters read because of dropping newlines when not adding a seek because we're going through pages sequentially.
            content = new string(buffer, drop, x);
            lastPage = pageNumber;
            IList<string> lines;
            if (md.ReadWithMultiLineEndDelimiter)
            {
                lines = content.Split(md.MultiLineEndDelimiter, StringSplitOptions.None);
            }
            else if (string.IsNullOrEmpty(md.LineEndDelimiter))
            {
                lines = content.SplitOnString(md.Columns.MaxLength);
            }
            else
            {
                lines = content.SplitOnString(md.LineEndDelimiter);
            }
            return lines;
        }
        /// <summary>
        /// cached list, the most recent result from calling <see cref="GetPage(int, bool)"/>
        /// </summary>
        protected List<ReadType> CurrentPage = null;
        /// <summary>
        /// Gets the content of the specified 'page'
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="cached">If the same page is accessed more than once in a row, the same List of objects will be returned</param>
        /// <returns></returns>
        public virtual List<ReadType> GetPage(int pageNumber, bool cached = true)
        {
            if (pageNumber == lastPage && CurrentPage != null && cached)
                return CurrentPage;
            var lines = GetPageLines(pageNumber);
            List<ReadType> LineRecords = new List<ReadType>();
            lines.ForEachIndex((line, idx) =>
            {
                var rec = Columns.ParseRecord<ReadType>(md.CanWrite, line);
                if (rec == null)
                {
                    System.Diagnostics.Debug.WriteLine("Empty Record found! Page: " + pageNumber + ", LineNumber: " + idx);
                    /*
                    if (idx == lines.Count)
                    {
                        System.Diagnostics.Debug.WriteLine("Last Line of page - Skipping empty record.");
                        return;
                    }*/
                }
                LineRecords.Add(rec);
            }, 1, 1);
            CurrentPage = LineRecords;
            return LineRecords;
        }

        #endregion

        public IEnumerator<ReadType> GetEnumerator()
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
