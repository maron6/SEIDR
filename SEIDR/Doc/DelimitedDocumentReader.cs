using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SEIDR.Doc
{
    /// <summary>
    /// Wrapper class for reading delimited files.
    /// </summary>
    public sealed class DelimitedDocumentReader : IDisposable, IEnumerable<DelimitedRecord>
    {
        /// <summary>
        /// Returns a delimited record with no content, but set up to match the format from this document... 
        /// </summary>
        public DelimitedRecord EMPTY
        {
            get
            {
                return new DelimitedRecord(_Header,
                    "".PadLeft(_Header.Length, _Delimiter.Value),
                    _Header.Length,
                    _Delimiter, ALIAS);             
            }
        }
        /// <summary>
        /// Alias for the document, can be used for determining source of a delimited record. Defaults to file name without extension
        /// </summary>
        public string ALIAS;
        /// <summary>
        /// Returns the header values delimited by the document's delimiter
        /// </summary>
        /// <param name="useDefaultHeader"></param>        
        /// <returns></returns>
        public string GetHeader(bool useDefaultHeader = false)
        {

            if (useDefaultHeader)
            {
                if (_Delimiter.HasValue)
                    return string.Join(_Delimiter.ToString(), _Header);
                return _Header[0];
            }
            if (_Delimiter.HasValue)
                return string.Join(_Delimiter.ToString(), PassHeader.ToArray());
            return PassHeader[0];
        }
        int linesSkipped;
        /// <summary>
        /// Resets the document to the start of the file. Does not change other settings like added headers.
        /// <para>Note that it will start off at the same place as it did originally.</para>
        /// </summary>
        public void Reset()
        {
            string f = qr.FilePath;
            qr.Dispose();
            qr = new FileReader(f) { ChangeLineEnding = true };
            LineNumber = linesSkipped;
            Page = 0;
            _Data = qr.Read(out _hasWork);
            pageRecordCount = _Data.Length - linesSkipped;
            minRecord = 0;
            maxRecord = pageRecordCount;            
        }
        /// <summary>
        /// get the records on the current page
        /// </summary>
        public DelimitedRecord[] CurrentPage
        {
            get
            {
                var r = DataPage();  //Data page in case we're on page 0              
                var h = PassHeader.ToArray();
                int l = _Header.Length;
                var records = (from record in r                               
                               select new DelimitedRecord(h, record, l, _Delimiter, ALIAS)
                               );
                return records.ToArray();
                /*
                List<DelimitedRecord> records = new List<DelimitedRecord>();
                for (int i = 0; i < r.Length; i++)
                {
                    records.Add(new DelimitedRecord(PassHeader.ToArray(), r[i], _Header.Length, _Delimiter));
                }
                return records.ToArray();
                */
            }
        }
        /// <summary>
        /// Increment the page and return its records
        /// </summary>
        /// <returns></returns>
        public DelimitedRecord[] GetNextPage()
        {
            //Will always be higher than page 0
            if (!_hasWork)
                return null;
            _Data = qr.Read(out _hasWork);
            Page++;
            pageRecordCount = _Data.Length;
            minRecord = maxRecord + 1;
            maxRecord += pageRecordCount;
            var h = PassHeader.ToArray();
            int l = _Header.Length;
            return (from record in _Data
                    select new DelimitedRecord(h, record, l, _Delimiter, ALIAS)
                    ).ToArray();
        }
        private string[] DataPage()
        {
            int i = 0;
            if (Page == 0)
                i = linesSkipped;
            List<string> temp = new List<string>();
            for (; i < _Data.Length; i++)
            {
                temp.Add(_Data[i]);
            }
            return temp.ToArray();
        }
        /// <summary>
        /// Get a 'page' of delimited records
        /// </summary>
        /// <param name="page">Zero based page number. If the page number goes out of range, returns null</param>
        /// <returns></returns>
        public DelimitedRecord[] GetPage(int page)
        {
            if (Page > page)
            {
                Reset();
            }
            while (Page < page)
            {
                if (!_hasWork)
                    return null; //If finished without reaching specified page, return null
                _Data = qr.Read(out _hasWork);
                Page++;                
                pageRecordCount = _Data.Length;
                minRecord = maxRecord + 1;
                maxRecord += pageRecordCount;
            }
            return CurrentPage;
        }
        int _Page = 0;
        /// <summary>
        /// Gets the current "page" of the document
        /// </summary>
        public int Page
        {
            get { return _Page; }
            private set
            {
                if (_Page == value)
                    return;
                _Page = value;
                if (value == 0)
                    LineNumber = linesSkipped;
                else
                    LineNumber = 0;
            }

        }
        /// <summary>
        /// Gets the header joined by a new delimiter instead of the original
        /// </summary>
        /// <param name="newDelimiter"></param>
        /// <param name="useDefaultHeader"></param>
        /// <returns></returns>
        public string GetHeader(char newDelimiter, bool useDefaultHeader = false)
        {

            if (useDefaultHeader)
            {
                if (_Delimiter.HasValue)
                    return string.Join(newDelimiter.ToString(), _Header);
                return _Header[0];
            }
            if (_Delimiter.HasValue)
                return string.Join(newDelimiter.ToString(), PassHeader.ToArray());
            return PassHeader[0];
        }
        /// <summary>
        /// Returns the headers as an an array of column names
        /// </summary>
        /// <returns></returns>
        public string[] GetHeader()
        {
            return new List<string>(_Header).ToArray();
        }
        /// <summary>
        /// Returns the lines skipped by the reader based on the line to skip parameter
        /// </summary>
        public ReadonlyLines SkippedLines { get; private set; }
        /// <summary>
        /// Default value for new readers
        /// </summary>
        public static int DefaultPageSize { get; set; }
        /// <summary>
        /// Modifies the number of characters read at a time when creating records and determines the maximum number of characters per page
        /// <para>Minimum size of 34,000</para>
        /// </summary>
        public int PageSize
        {
            get
            {
                if (qr == null)
                    return -1;
                return qr.block;
            }
            set
            {
                if (qr == null)
                    throw new Exception("Delimited Document has not been correctly initialized.");
                int x = value;
                if (x < 34000)
                    x = 34000;
                qr.block = x;
            }
        }
        /// <summary>
        /// Number of records on th current page
        /// </summary>
        public int pageRecordCount {get; private set;} = 0;
        /// <summary>
        /// Minimum record number on the current page
        /// </summary>
        public long minRecord { get; private set; } = 0;
        /// <summary>
        /// Maximum record number on the current page
        /// </summary>
        public long maxRecord { get; private set; } = 0;
        FileReader qr;
        int LineNumber;
        string[] _Data;
        char? _Delimiter;
        string[] _Header;
        string[] Header { get { return _Header; } set { _Header = value; PassHeader = new List<string>(value); } }
        List<string> PassHeader; 
        /// <summary>
        /// Adds a header to the document. Used for access in the Delimited record/adding extra columns if the record is being used elsewhere.
        /// </summary>
        /// <param name="Header"></param>
        public void AddHeader(string Header)
        {
            if (_Delimiter == null)
                throw new Exception("Document was created without a delimiter, cannot add columns.");
            PassHeader.Add(Header);
        }
        /// <summary>
        /// Returns the path used for the file being read.
        /// </summary>
        public string FilePath { get { return qr.FilePath; } }
        /// <summary>
        /// Resets the header for Delimited records to match what was originally set
        /// </summary>
        public void ResetHeader() { PassHeader = new List<string>(Header); }
        private DelimitedDocumentReader(int linesToSkip, string FilePath, char? Delimiter)
        {
            if (linesToSkip < 0)
                linesToSkip = 0;
            qr = new FileReader(FilePath, false) { ChangeLineEnding = true, block = DefaultPageSize };
            this.PageSize = DefaultPageSize;
            _Delimiter = Delimiter;
            LineNumber = linesToSkip;
            linesSkipped = LineNumber;
            ALIAS = System.IO.Path.GetFileNameWithoutExtension(FilePath);
        }
        bool _hasWork;
        /// <summary>
        /// Creates a simple instance fo a delimited document to iterate through line by line, with the column delimiter guessed
        /// </summary>
        /// <param name="FilePath"></param>
        /// <param name="linesToSkip"></param>
        /// <param name="Header">Optional, the headers for the document. If null, will use the first line</param>
        public DelimitedDocumentReader(string FilePath, int linesToSkip = 0, string[] Header = null)            
        {
            if (linesToSkip < 0)
                linesToSkip = 0;
            qr = new FileReader(FilePath) { ChangeLineEnding = true, block= DefaultPageSize };
            _Data = qr.Read(out _hasWork);
            string x = _Data[linesToSkip++];
            //pageRecordCount = _Data.Length;
            //minRecord = 0;
            //minRecord = pageRecordCount - linesToSkip;
            pageRecordCount = _Data.Length - linesSkipped;
            minRecord = 0;
            maxRecord = pageRecordCount;
            this.PageSize = DefaultPageSize;
            ALIAS = System.IO.Path.GetFileNameWithoutExtension(FilePath);
            try
            {
                _Delimiter = x.GuessDelimiter();
            }
            catch { _Delimiter = null; }            
            if(Header == null)
            {
                if (_Delimiter.HasValue)
                    Header = x.Split(_Delimiter.Value);
                else
                    Header = new string[] { x };                
            }
            string[] temp = new string[linesToSkip];
            for (int i = 0; i < linesToSkip; i++)
            {
                temp[i] = _Data[i];
            }
            SkippedLines = new ReadonlyLines(temp);
            this.Header = Header;
            this.LineNumber = linesToSkip;            
            this.linesSkipped = LineNumber;
        }
        /// <summary>
        /// Creates an instance of a delimited Document to iterate through, line by line.
        /// <para>Assumes that there is a header at the first line after finish skipping</para>
        /// </summary>
        /// <param name="FilePath"></param>
        /// <param name="Delimiter">If null, will treat as a single column</param>        
        /// <param name="linesToSkip"></param>
        public DelimitedDocumentReader(string FilePath, char? Delimiter, int linesToSkip = 0)
            :this(linesToSkip, FilePath, Delimiter)
        {                                 
            _Data = qr.Read(out _hasWork);
            string[] temp = new string[linesToSkip];
            for (int i = 0; i < linesToSkip; i++)
            {
                temp[i] = _Data[i];
            }
            SkippedLines = new ReadonlyLines(temp);
            if (Delimiter.HasValue)
                Header = _Data[linesToSkip++].Split(Delimiter.Value);
            else
                Header = new string[] { _Data[linesToSkip++] };
            pageRecordCount = _Data.Length - linesSkipped;
            minRecord = 0;
            maxRecord = pageRecordCount;
            LineNumber = linesToSkip; //override the default lineNumber start         
            this.linesSkipped = LineNumber;
            
        }
        /// <summary>
        /// Creates an instance of a Delimited Document to iterate through line by line
        /// </summary>
        /// <param name="Filepath"></param>
        /// <param name="Delimiter"></param>
        /// <param name="Header"></param>
        /// <param name="linesToSkip"></param>
        public DelimitedDocumentReader(string Filepath, char? Delimiter, string[] Header, int linesToSkip = 0)
            :this(linesToSkip, Filepath, Delimiter)
        {            
            this.Header = Header;
            _Data = qr.Read(out _hasWork);
            string[] temp = new string[linesToSkip];
            for(int i= 0; i < linesToSkip; i++)
            {
                temp[i] = _Data[i];
            }
            SkippedLines = new ReadonlyLines(temp);
            pageRecordCount = _Data.Length - linesSkipped;
            minRecord = 0;
            maxRecord = pageRecordCount;
        }

        /// <summary>
        /// Loops through the document's content and provides DelimitedRecords
        /// </summary>
        /// <returns></returns>
        public IEnumerator<DelimitedRecord> GetEnumerator()
        {
            if (_Data == null)
            {
                _Data = qr.Read(out _hasWork);
                Page++; //Shouldn't happen, but just to have it.
                pageRecordCount = _Data.Length;
                minRecord = 0;
                maxRecord = _Data.Length - linesSkipped;
            }
            while(_hasWork || LineNumber < _Data.Length)
            {
                if(LineNumber >= _Data.Length)
                {
                    // Quick reader still has content, just need to read it.
                    _Data = qr.Read(out _hasWork);
                    pageRecordCount = _Data.Length;
                    minRecord = maxRecord + 1;
                    maxRecord += pageRecordCount;
                    LineNumber = 0;
                    if (_Data.Length == 0)
                        yield break;
                }
                string r = _Data[LineNumber++];
                yield return new DelimitedRecord(PassHeader.ToArray(), r, _Header.Length, _Delimiter, ALIAS);
                if(LineNumber >= _Data.Length && _hasWork)
                {
                    _Data = qr.Read(out _hasWork);
                    LineNumber = 0;
                    Page++;
                    pageRecordCount = _Data.Length;
                    minRecord = maxRecord + 1;
                    maxRecord += pageRecordCount;
                }
            }
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Disposes the underlying file reader
        /// </summary>
        public void Dispose()
        {
            Dispose(!Disposed);
            GC.SuppressFinalize(this);
        }
        bool Disposed = false;
        private void Dispose(bool disposing)
        {
            if(disposing)
                ((IDisposable)qr).Dispose();

            Disposed = true;
        }
        /// <summary>
        /// 
        /// </summary>
        ~DelimitedDocumentReader()
        {
            if (!Disposed)
                Dispose(true);
        }
    }
    /// <summary>
    /// For maintaining a readonly array of strings
    /// </summary>
    public sealed class ReadonlyLines
    {
        string[] _Lines;
        /// <summary>
        /// Gets the skipped line from the specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string this[int index]
        {
            get { return _Lines[index]; }
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Lines"></param>
        public ReadonlyLines(string[] Lines)
        {
            _Lines = Lines;
        }
    }
}
