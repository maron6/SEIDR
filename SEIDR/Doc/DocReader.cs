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
        #region operators.
        /// <summary>
        /// Pass the Reader as MetaDataBase.
        /// </summary>
        /// <param name="r"></param>
        public static implicit operator MetaDataBase(DocReader r)
        {
            return r.MetaData;
        }
        /// <summary>
        /// Returns the MetaData as a <see cref="DocMetaData"/> instance, if it's that type of metaData.
        /// </summary>
        /// <param name="r"></param>
        public static implicit operator DocMetaData(DocReader r)
        {
            return r.MetaData as DocMetaData;
        }
        /// <summary>
        /// Returns the MetaData as a <see cref="MultiRecordDocMetaData"/> instance, if it's that type of metadata.
        /// </summary>
        /// <param name="r"></param>
        public static implicit operator MultiRecordDocMetaData(DocReader r)
        {
            return r.MetaData as MultiRecordDocMetaData;
        }
        /// <summary>
        /// Returns the underlying DocRecordColumn Collection, but only if the MetaData is type <see cref="DocMetaData"/>.
        /// </summary>
        /// <param name="r"></param>
        public static implicit operator DocRecordColumnCollection(DocReader r)
        {
            if(r.MetaData is DocMetaData)
                return ((DocMetaData)r).Columns;
            return null;
        }
        #endregion

        /// <summary>
        /// Basic constructor, no meta data configured yet.
        /// </summary>
        public DocReader() : base() { }
        /// <summary>
        /// Sets up a doc reader for DocRecord enumeration.
        /// </summary>
        /// <param name="info"></param>
        public DocReader(MetaDataBase info) : base(info) { }
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


        /*
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
                var rec = _MetaData.Parse(line);
                if (rec == null)
                {
                    System.Diagnostics.Debug.WriteLine("Empty Record found! Page: " + pageNumber + ", LineNumber: " + idx);
                    /*
                    if (idx == lines.Count)
                    {
                        System.Diagnostics.Debug.WriteLine("Last Line of page - Skipping empty record.");
                        return;
                    }// /
                }
                LineRecords.Add(rec);
            }, 1, 1);
            CurrentPage = LineRecords;
            return LineRecords;
        } */
    }

    /// <summary>
    /// Allows reading a delimited or fixed width file based on meta data. <para>
    /// Includes paging for working with large files in batches
    /// </para>
    /// </summary>
    public class DocReader<ReadType>:IEnumerable<ReadType>, IDisposable where ReadType: IDataRecord, new()
    {
        /// <summary>
        /// Indicates whether or not empty lines are skipped, or returned as null.
        /// </summary>
        public readonly bool IncludeNullRecords = false;
        /// <summary>
        /// Preview the first <paramref name="charCount"/> characters from the file.
        /// </summary>
        /// <param name="FilePath"></param>
        /// <param name="charCount"></param>
        /// <returns></returns>
        public static string Preview(string FilePath, int charCount = 3000)
        {
            char[] ret = new char[charCount];
            using (var sr = new StreamReader(FilePath))
            {
                int x = sr.ReadBlock(ret, 0, charCount);
                return new string(ret, 0, x);
            }
        }
        /// <summary>
        /// Goes through the data in the associated DocReader and attempts to check all columns for data types and attempts to narrow down the most likely data type.        
        /// </summary>
        /// <param name="RecordLimit">Maximum number of lines to go through from the Document. If value is less than or equal to 0, will go through all records in the file.</param>
        public void GuessColumnDataTypes(int RecordLimit = 0)
        {
            long counter = 0;
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            var AlphaCheck = new System.Text.RegularExpressions.Regex("([a-zA-Z]+?)", System.Text.RegularExpressions.RegexOptions.Compiled);
            string num = "([0-9"
                + culture.NumberFormat.NegativeSign.Replace("-", @"\-")
                + culture.NumberFormat.PositiveSign.Replace("-", @"\-")
                + "]+?)";
            string dec = "([0-9"
                + culture.NumberFormat.NumberDecimalSeparator.Replace("-", @"\-")
                + culture.NumberFormat.NumberGroupSeparator.Replace("-", @"\-")
                + culture.NumberFormat.NegativeSign.Replace("-", @"\-")
                + culture.NumberFormat.PositiveSign.Replace("-", @"\-");
            string money = dec + culture.NumberFormat.CurrencySymbol.Replace("-", @"\-") + "]?)";
            dec += "]?)";
            var decimalCheck = new System.Text.RegularExpressions.Regex(dec, System.Text.RegularExpressions.RegexOptions.Compiled);
            var NumericCheck = new System.Text.RegularExpressions.Regex(num, System.Text.RegularExpressions.RegexOptions.Compiled);
            var MoneyCheck = new System.Text.RegularExpressions.Regex(money, System.Text.RegularExpressions.RegexOptions.Compiled);
            Dictionary<DocRecordColumnInfo, List<DocRecordColumnType>> PossibleMaps = new Dictionary<DocRecordColumnInfo, List<DocRecordColumnType>>();
            foreach(var record in this)
            {
                if (RecordLimit > 0 && counter++ > RecordLimit)
                    break;
                var cols = _MetaData.GetRecordColumnInfos(record).Where(c => c.DataType == DocRecordColumnType.Unknown);
                if (cols.UnderMaximumCount(0))
                    continue;
                foreach(var col in cols)
                {                    
                    string x = record[col];
                    if (string.IsNullOrWhiteSpace(x))
                        continue;
                    x = x.Trim();
                    List<DocRecordColumnType> l;
                    if (!PossibleMaps.ContainsKey(col))
                    {
                        l = new List<DocRecordColumnType>();
                        if (AlphaCheck.IsMatch(x))
                        {
                            l.AddRange(new[] { DocRecordColumnType.DateTime, DocRecordColumnType.Date, DocRecordColumnType.Bool });
                        }                        
                        else if (NumericCheck.IsMatch(x))
                        {
                            l.AddRange(new[] { DocRecordColumnType.Int, DocRecordColumnType.Bigint, DocRecordColumnType.Bool, DocRecordColumnType.Date, DocRecordColumnType.DateTime });
                            if (MoneyCheck.IsMatch(x))
                            {
                                l.AddRange(new[] { DocRecordColumnType.Decimal, DocRecordColumnType.Money });
                            }
                            else if (decimalCheck.IsMatch(x))
                            {
                                l.Add(DocRecordColumnType.Decimal);
                            }
                        }
                        else
                        {
                            l.AddRange(new[] { DocRecordColumnType.DateTime, DocRecordColumnType.Date, DocRecordColumnType.Bool });
                        }
                        PossibleMaps.Add(col, l);
                    }
                    else
                        l = PossibleMaps[col];
                    if (l.Contains(DocRecordColumnType.Decimal))
                    {         
                        if(!decimalCheck.IsMatch(x))
                        {
                            l.Remove(DocRecordColumnType.Decimal);                            
                        }
                    }
                    if (l.Contains(DocRecordColumnType.Money))
                    {
                        if (!MoneyCheck.IsMatch(x))
                        {
                            l.Remove(DocRecordColumnType.Money);
                        }
                    }
                    string throwAway;
                    if (l.Contains(DocRecordColumnType.DateTime))
                    {
                        if(!DateConverter.GuessFormatDateTime(x, out throwAway))
                        {
                            l.Remove(DocRecordColumnType.DateTime);
                        }
                    }
                    if (l.Contains(DocRecordColumnType.Date))
                    {
                        if (!DateConverter.GuessFormatDate(x, out throwAway))
                        {
                            l.Remove(DocRecordColumnType.Date);
                        }
                    }
                    if (l.Contains(DocRecordColumnType.Bool))
                    {
                        bool b;
                        if (!bool.TryParse(x, out b))
                        {
                            if (x.ToUpper().NotIn("YES", "Y", "N", "NO"))
                                l.Remove(DocRecordColumnType.Bool);
                        }
                    }
                    long tempL;
                    if (l.Contains(DocRecordColumnType.Int))
                    {                        
                        if(long.TryParse(x, out tempL))
                        {
                            if (tempL > Int32.MaxValue || tempL < Int32.MinValue)
                                l.Remove(DocRecordColumnType.Int);
                        }
                        else
                        {
                            l.Remove(DocRecordColumnType.Int);                            
                            l.Remove(DocRecordColumnType.Bigint);
                        }
                    }
                    else if (l.Contains(DocRecordColumnType.Bigint))
                    {
                        if (!long.TryParse(x, out tempL))
                        {
                            l.Remove(DocRecordColumnType.Bigint);
                        }
                    }

                    if (l.Count == 0)
                    {
                        col.DataType = DocRecordColumnType.Varchar;
                        PossibleMaps.Remove(col);
                    }
                }
            }

            foreach (var maps in PossibleMaps)
            {
                var l = maps.Value;
                if (l.Contains(DocRecordColumnType.Bool))
                {
                    maps.Key.DataType = DocRecordColumnType.Bool;
                    continue;
                }
                if (l.Contains(DocRecordColumnType.DateTime))
                {
                    maps.Key.DataType = DocRecordColumnType.DateTime;
                    continue;
                }
                if (l.Contains(DocRecordColumnType.Date))
                {
                    maps.Key.DataType = DocRecordColumnType.Date;
                    continue;
                }
                if (l.Contains(DocRecordColumnType.Int))
                {
                    maps.Key.DataType = DocRecordColumnType.Int;
                    continue;
                }
                if (l.Contains(DocRecordColumnType.Bigint))
                {
                    maps.Key.DataType = DocRecordColumnType.Bigint;
                    continue;
                }
                if (l.Contains(DocRecordColumnType.Decimal))
                {
                    maps.Key.DataType = DocRecordColumnType.Decimal;
                    continue;
                }
                if (l.Contains(DocRecordColumnType.Money))
                {
                    maps.Key.DataType = DocRecordColumnType.Money;
                    continue;
                }
            }
        }
        FileStream fs;
        StreamReader sr;
        /// <summary>
        /// True underlying meta data
        /// </summary>
        protected MetaDataBase _MetaData;
        /// <summary>
        /// Underlying MetaData
        /// </summary>
        public MetaDataBase MetaData => _MetaData;
        /// <summary>
        /// Columns associated with the meta data.
        /// <para>Returns null for multi record.</para>
        /// </summary>
        public DocRecordColumnCollection Columns
        {
            get
            {
                var md = _MetaData as DocMetaData;
                if (md != null)
                    return md.Columns;

                return null;
            }
        }
        /// <summary>
        /// Full file path, from meta data
        /// </summary>
        public string FilePath => _MetaData.FilePath;
        /// <summary>
        /// File name associated with the doc
        /// </summary>
        public string FileName => Path.GetFileName(_MetaData.FilePath);
        /// <summary>
        /// File alias, from meta data
        /// </summary>
        public string Alias => _MetaData.Alias;

        /// <summary>
        /// Sets up a doc reader for DocRecord enumeration.
        /// </summary>
        /// <param name="info"></param>
        public DocReader(MetaDataBase info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            if (!info.AccessMode.HasFlag(FileAccess.Read))
                throw new ArgumentException(nameof(info), "Not Configured for read mode");
            _MetaData = info;
            IncludeNullRecords = info.AllowNullRecords;
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
            if(_MetaData.LineEndDelimiter != null && !_MetaData.MultiLineEndDelimiter.Contains(_MetaData.LineEndDelimiter))
                    _MetaData.AddMultiLineEndDelimiter(_MetaData.LineEndDelimiter);
            else if (_MetaData.ReadWithMultiLineEndDelimiter && _MetaData.LineEndDelimiter == null)
            {
                _MetaData.SetLineEndDelimiter( _MetaData.MultiLineEndDelimiter[0]);
            }
            if (_MetaData is DocMetaData)
            {
                var dmd = _MetaData as DocMetaData;
                if (dmd.Format.In(DocRecordFormat.DELIMITED, DocRecordFormat.VARIABLE_WIDTH) && string.IsNullOrEmpty(_MetaData.LineEndDelimiter))
                    throw new InvalidOperationException("Cannot do " + dmd.Format.GetDescription() + " document without a line End delimiter.");
                if (dmd.FixWidthMode && !_MetaData.HeaderConfigured)
                    throw new InvalidOperationException("Cannot do " + dmd.Format.GetDescription() + " without header configured already.");
                if (dmd.FixWidthMode && _MetaData.PageSize < dmd.Columns.MaxLength)
                    throw new InvalidOperationException($"Page Size({_MetaData.PageSize}) is smaller than FixedWidth line Length ({dmd.Columns.MaxLength})");
            }
            disposedValue = false;
            //if (_MetaData.Format == DocRecordFormat.SBSON && helper == null)
            //    helper = new BitCONHelper();
            if (sr != null) { sr.Close(); sr = null; }
            if (fs != null) { fs.Close(); fs = null; }
            fs = new FileStream(_MetaData.FilePath, FileMode.Open, _MetaData.AccessMode);
            if (_MetaData.FileEncoding == null)
                sr = new StreamReader(fs);
            else
                sr = new StreamReader(fs, _MetaData.FileEncoding);

            if (Pages == null)
                Pages = new List<PageHelper>();
            else
                Pages.Clear();
            CurrentPage = null;
            int extra = 0; //Add extra space for buffer so we don't have to discard buffer when going from one page to the next while avoiding including the ending newLine information (because we don't need it in the output)
            if (_MetaData.ReadWithMultiLineEndDelimiter)
            {
                extra = _MetaData.MultiLineEndDelimiter.Max(ml => ml.Length);
            }
            else if (_MetaData.LineEndDelimiter != null)
                extra = _MetaData.LineEndDelimiter.Length;
            buffer = new char[_MetaData.PageSize + extra];
            int pp = _MetaData.SkipLines;
            if (_MetaData.HasHeader && _MetaData.HeaderConfigured)
                pp = _MetaData.SkipLines + 1;
            sr.Peek(); //BOM issue shows up after initial open, peek allows us to manage during set up
            if (_MetaData.FileEncoding == null)
                _MetaData.FileEncoding = sr.CurrentEncoding;
            long position = 0;
            if (_MetaData.TrustPreamble)
                position = _MetaData.FileEncoding.GetPreamble().Length;
            while(SetupPageMetaDataV2(ref position, ref pp)) { }
            //while(SetupPageMetaData(ref position, ref pp)) { }
            if (!_MetaData.Valid)
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
        /// <summary>
        /// Gets information about page number <paramref name="page"/>
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public PageHelper GetPageInfo(int page) => Pages[page];

        bool SetupPageMetaDataV2(ref long startPosition, ref int skipLine)
        {
            fs.Seek(startPosition, SeekOrigin.Begin);
            sr.DiscardBufferedData();
            int x = sr.ReadBlock(buffer, 0, _MetaData.PageSize);
            if (x == 0)
                return false; //empty, nothing to do. Shouldn't happen, though, since startPosition should be the previous end position after removing the end...
            //bool end = x < _MetaData.PageSize;
            //if(!end && startPosition + x > )
            if (startPosition < _MetaData.PageSize && !_MetaData.TrustPreamble)
            {
                var preamble = sr.CurrentEncoding.GetPreamble();
                char pc = buffer[0];
                if (char.IsControl(pc)
                    || (!char.IsWhiteSpace(pc) && !char.IsLetterOrDigit(pc) && !char.IsNumber(pc)
                            && !char.IsSeparator(pc) && !char.IsPunctuation(pc) && !char.IsSymbol(pc)
                    ))
                //if (((int)buffer[0]).In(65279, 65533, 0))
                {
                    if (preamble.Length > 0 && buffer.StartsWithByteSet(sr.CurrentEncoding, preamble))
                    {
                        startPosition += preamble.Length; //If we have the right encoding, then this should take care of it. If the encoding passed doesn't use a preamble, then just go forward 1 char at a time (original approach). If it's the wrong preamble... May have other issues
                    }
                    else
                        startPosition += 1; // BOM fix: move initial position forward 1 by 1 until we have our initial position completely past the BOM 
                                            /*
                                                Behaviour seen: 
                                                First Pass: Looks normal
                                                Second pass: character 65279 shows up in position 0
                                                Third pass: 65533 shows up twice. 

                                            May be able to just go forward a hardcoded value if we see 65279 in position 0 with the throwaway from setup, but this way we can be sure that we get a clean start.
                                            Would need to see if other encodings that make use of a byte order specification at the start of the stream could have similar issue but with different char values or required position offsets for startPosition
                                                */
                    return true;
                }
                if (preamble.Length > 0 && buffer.StartsWithByteSet(sr.CurrentEncoding, preamble))
                {
                    startPosition += preamble.Length;
                    return true;
                }
            }
            Encoding readCode = sr.CurrentEncoding;
            string content = /*working.ToString() +*/ new string(buffer, 0, x);
            int contentLength = readCode.GetByteCount(content);
            bool end = (startPosition + contentLength) >= _MetaData.FileLength;
            IList<string> lines;

            bool fixWidth_NoNewLine = false;
            if (_MetaData is DocMetaData)
            {
                fixWidth_NoNewLine = _MetaData.FixWidthMode
                                        && string.IsNullOrEmpty(_MetaData.LineEndDelimiter) //RaggedRight/Variable width need line end as well
                                        && !_MetaData.ReadWithMultiLineEndDelimiter;
            }
            int endLine;
            //int lastNLSize = 0; //Size of the NewLine Delimiter if the page ends on a NewLine delimiter. //Note: Potential for extra line if we have multiple line ends and end a page between an \r and \n, but that would be adding an empty record (null)
            int maxNLSize;
            if (_MetaData.ReadWithMultiLineEndDelimiter)
                maxNLSize = _MetaData.MultiLineEndDelimiter.Max(c => c.Length);
            else
                maxNLSize = _MetaData.LineEndDelimiter.Length;

            long endPosition;
            int removedBytes;
            int startOffset = 0;
            if (_MetaData.Format == DocRecordFormat.SBSON)
            {
                lines = SBSONHelper.SplitString(content, MetaData, out removedBytes);
                /*
                lines = helper.SplitString(content, _MetaData).ToList();
                removed = helper.RemainingBytesFromSplit;
                endLine = lines.Count;
                endPosition = startPosition + contentLength - helper.RemainingBytesFromSplit;
                */
            }
            else if (fixWidth_NoNewLine)
                lines = FormatHelper.FixWidthHelper.SplitStringByLength(content, _MetaData, out removedBytes); //Must be single column set metadata. (DocMetaData)
            else if (_MetaData.ReadWithMultiLineEndDelimiter)                            
                lines = FormatHelper.DelimiterHelper.SplitString(content, _MetaData.MultiLineEndDelimiter, out startOffset, out removedBytes, _MetaData.AllowQuotedNewLine, _MetaData, end, SkipEmpty: !IncludeNullRecords);             
            else
                lines = FormatHelper.DelimiterHelper.SplitString(content, _MetaData.MultiLineEndDelimiter, out startOffset, out removedBytes, _MetaData.AllowQuotedNewLine, _MetaData, end, SkipEmpty: !IncludeNullRecords);                
            
            endPosition = startPosition + contentLength - removedBytes;
            startPosition += startOffset;
            startOffset = 0;
            endLine = lines.Count;
            if (skipLine > 0)
            {
                if (skipLine >= endLine)
                {
                    startPosition = endPosition;
                    skipLine -= endLine;
                    return !end;
                }
                //endLine > skipLine, remove skipLine # records, then continue to next section...
                if (fixWidth_NoNewLine)
                {
                    //Must be single column type, so max length would be consistent.
                    //startPosition += skipLine * ((DocMetaData)_MetaData).Columns.MaxLength; //Move forward by skipLine lines
                    int charSkip = skipLine * ((DocMetaData)_MetaData).Columns.MaxLength; //Move forward by skipLine lines
                    startPosition += sr.CurrentEncoding.GetByteCount(buffer, 0, charSkip);
                }
                else if(_MetaData.Format == DocRecordFormat.SBSON)
                {
                    startPosition += SBSONHelper.GetSkipPosition(content, readCode, skipLine);
                }
                else
                {
                    //int posHelper = 0; //offset from content[0]
                    int forward;
                    if (_MetaData.ReadWithMultiLineEndDelimiter)
                        forward = FormatHelper.DelimiterHelper.GetSkipPosition(content, 
                            _MetaData.MultiLineEndDelimiter, 
                            false, _MetaData, skipLine, skipEmpty: !IncludeNullRecords);
                    else
                        forward = FormatHelper.DelimiterHelper.GetSkipPosition(content, 
                            new string[] { _MetaData.LineEndDelimiter }, 
                            false, _MetaData, skipLine, skipEmpty: !IncludeNullRecords);
                    startPosition += forward;
                }
                skipLine = 0;
                return true; //re-read from the correct starting position instead of trying to mess with the list.
            }
            int headerCount = 0;
            if (!_MetaData.HeaderConfigured)
            {                
                if (_MetaData.Format.In(DocRecordFormat.VARIABLE_WIDTH, DocRecordFormat.DELIMITED) && !_MetaData.Delimiter.HasValue)
                {
                    _MetaData.SetDelimiter(lines.GuessDelimiter());
                    if (!_MetaData.Delimiter.HasValue)
                        throw new Exception("Unable to guess column delimiter.");
                }
                startOffset = 0;
                if (_MetaData is DocMetaData)
                {                    
                    var md = _MetaData as DocMetaData;
                    List<DocRecordColumnInfo> cols;
                    bool checkOffSet = !fixWidth_NoNewLine && md.HasHeader;
                    switch (_MetaData.Format)
                    {
                        case DocRecordFormat.FIX_WIDTH:
                        case DocRecordFormat.RAGGED_RIGHT:
                        //case DocRecordFormat.VARIABLE_WIDTH: 
                            cols = FormatHelper.FixWidthHelper.InferColumnset(lines, md); //ToDo
                            break;
                        case DocRecordFormat.SBSON:
                            checkOffSet = false;//Note: header is going to be *mostly* human readable if available.
                            cols = SBSONHelper.InferColumnList(lines[headerCount], readCode, md.Alias, md.HasHeader);
                            break;
                        default:
                            cols = FormatHelper.DelimiterHelper.InferColumnList(lines[headerCount], _MetaData, true, ref startOffset);
                            break;
                    }
                    
                    foreach(var col in cols)
                    {
                        md.AddColumn(col);
                    }
                    if (checkOffSet)
                    {
                        var byteSet = readCode.GetBytes(content.ToCharArray(lines[headerCount].Length, maxNLSize));
                        if (md.ReadWithMultiLineEndDelimiter)
                        {
                            foreach(string le in md.MultiLineEndDelimiter)
                            {
                                int byteCount = readCode.GetByteCount(le);
                                if (byteCount + startOffset > contentLength)
                                    continue;
                                if (readCode.GetString(byteSet, 0, byteCount) == le)
                                {
                                    startOffset += byteCount;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            int off = readCode.GetByteCount(md.LineEndDelimiter); //single delimiter - if we're splitting on it, then just add its byte count.
                            if(startOffset + off < contentLength) //Only include the delimiter as part of offset if it didn't go over... Shouldn't happen, though.
                                startOffset += off;
                        }
                    }
                    startPosition += startOffset;
                    headerCount++;
                }
                else if(_MetaData is MultiRecordDocMetaData)
                {
                    var md = (MultiRecordDocMetaData)_MetaData;                    
                    int headerStop = 0;
                    bool checkOffSet = !fixWidth_NoNewLine && md.HasHeader;
                    while (true)
                    {                        
                        List<DocRecordColumnInfo> cols;
                        switch (_MetaData.Format)
                        {
                            case DocRecordFormat.FIX_WIDTH:
                            case DocRecordFormat.RAGGED_RIGHT:
                            case DocRecordFormat.VARIABLE_WIDTH:
                                //cols = FormatHelper.FixWidthHelper.InferColumnset(lines, md);
                                throw new Exception("Header inferring not available for Fix width mutli record.");                                
                            case DocRecordFormat.SBSON:
                                checkOffSet = false;
                                cols = SBSONHelper.InferColumnList(lines[headerCount], md.FileEncoding, md.Alias, md.HasHeader);
                                break;
                            default:
                                cols = FormatHelper.DelimiterHelper.InferColumnList(lines[headerCount], _MetaData, true, ref startOffset);
                                break;
                        }
                        if (md.ColumnSets.ContainsKey(cols[0].ColumnName))
                            break;                        
                        md.CreateCollection(cols);
                        if (checkOffSet)
                        {
                            headerStop += lines[headerCount].Length;
                            var byteSet = readCode.GetBytes(content.ToCharArray(headerStop, maxNLSize));
                            if (md.ReadWithMultiLineEndDelimiter)
                            {
                                foreach (string le in md.MultiLineEndDelimiter)
                                {
                                    int byteCount = readCode.GetByteCount(le);
                                    if (byteCount + startOffset > contentLength)
                                        continue;
                                    if (readCode.GetString(byteSet, 0, byteCount) == le)
                                    {
                                        startOffset += byteCount;
                                        headerStop += le.Length;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                int off = readCode.GetByteCount(md.LineEndDelimiter); //single delimiter - if we're splitting on it, then just add its byte count.
                                if (startOffset + off < contentLength)
                                {
                                    //Only include the delimiter as part of offset if it didn't go over... Shouldn't happen, though.
                                    startOffset += off;
                                    headerStop += maxNLSize; // char count of line end.
                                }
                            }
                        }
                        headerCount++;
                        if (headerCount > lines.Count)
                            throw new Exception("Buffer too small to infer column set. Condsider increased buffer size or explicit column values.");
                    }
                }
            }

            int recordCount = endLine;
            if (_MetaData.HasHeader && headerCount != 0)
            {
                if (recordCount == headerCount)
                    return !end;//Small page size, only content was header info... we don't want to use it as the first page then, though.
                recordCount -= headerCount;                
            }
            if (recordCount == 0) //Probably only possible when doing headers (headerCount == recordCount), or if there's a bunch of empty lines and we aren't including those.
                return !end; 

            Pages.Add(new PageHelper(startPosition, endPosition, _MetaData.PageSize, recordCount: recordCount));
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
            _MetaData = new DocMetaData(FilePath, alias)
            {
                SkipLines = 0,
                //EmptyIsNull = true,
            };
            ((DocMetaData)_MetaData).SetHasHeader(true);
            if(Delimiter.HasValue)
                _MetaData.SetDelimiter(Delimiter.Value);

            _MetaData
                .SetLineEndDelimiter(LineEnd ?? Environment.NewLine)
                .SetFileAccess(FileAccess.Read) //allow multiple docReaders to access the same file.
                .SetFileEncoding(Encoding.Default);
            if(pageSize != null)
                _MetaData.SetPageSize(pageSize.Value);

            SetupStream();
        }
        /// <summary>
        /// Basic constructor, no meta data configured yet.
        /// <para>Will need to call <see cref="Configure(DocMetaData)"/> before using.</para>
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
            {
                System.Diagnostics.Debug.WriteLine("Forcing Access mode to ReadWrite.");
                metaData.SetFileAccess(FileAccess.ReadWrite);
                //throw new ArgumentException(nameof(metaData), "Not Configured for read mode");
            }

            Dispose();
            _MetaData = metaData;
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
        
        private IEnumerable<Tuple<string, int>> GetPageTupleLines(int pageNumber)
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
            
            if(_MetaData.Format == DocRecordFormat.SBSON)
            {
                int idx = 0;
                foreach (string s in SBSONHelper.EnumerateLines(content, _MetaData))
                    yield return new Tuple<string, int>(s, idx++);
                yield break;
            }
            else if (_MetaData.ReadWithMultiLineEndDelimiter)
            {
                int idx = 0;
                foreach (string s in FormatHelper.DelimiterHelper.EnumerateSplits(content, _MetaData.MultiLineEndDelimiter, _MetaData.AllowQuotedNewLine, _MetaData, SkipEmpty: !IncludeNullRecords))
                    yield return new Tuple<string, int>(s, idx++);
                yield break;
                //lines = content.Split(_MetaData.MultiLineEndDelimiter, StringSplitOptions.None);
            }
            else if (string.IsNullOrEmpty(_MetaData.LineEndDelimiter)) //FixWidth mode, no line ending.
            {
                IList<string> lines = content.SplitOnString(((DocMetaData)_MetaData).Columns.MaxLength);
                for(int i = 0; i < lines.Count; i++)
                {
                    yield return new Tuple<string, int>(lines[i], i);
                }         
            }
            else
            {
                int idx = 0;
                foreach (string s in FormatHelper.DelimiterHelper.EnumerateSplits(content, _MetaData.LineEndDelimiter, _MetaData.AllowQuotedNewLine, _MetaData, SkipEmpty: !IncludeNullRecords))
                    yield return new Tuple<string, int>(s, idx++);
                yield break;
                //lines = content.SplitOnString(_MetaData.LineEndDelimiter);
            }
            yield break;
        }

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
            if(_MetaData.Format == DocRecordFormat.SBSON)
            {
                lines = SBSONHelper.SplitString(content, _MetaData, out _);
            }
            else if (_MetaData.ReadWithMultiLineEndDelimiter)
            {
                lines = FormatHelper.DelimiterHelper.EnumerateSplits(content, _MetaData.MultiLineEndDelimiter, _MetaData.AllowQuotedNewLine, _MetaData, SkipEmpty: !IncludeNullRecords).ToArray();
                //lines = content.Split(_MetaData.MultiLineEndDelimiter, StringSplitOptions.None);
            }
            else if (string.IsNullOrEmpty(_MetaData.LineEndDelimiter)) //FixWidth mode, no line ending.
            {
                lines = content.SplitOnString(((DocMetaData)_MetaData).Columns.MaxLength);
            }
            else
            {
                lines = FormatHelper.DelimiterHelper.EnumerateSplits(content, _MetaData.LineEndDelimiter, _MetaData.AllowQuotedNewLine, _MetaData, SkipEmpty: !IncludeNullRecords).ToArray();
                //lines = content.SplitOnString(_MetaData.LineEndDelimiter);
            }
            return lines;
        }
        /// <summary>
        /// cached list, the most recent result from calling <see cref="GetPage(int, bool)"/>
        /// </summary>
        protected List<ReadType> CurrentPage = null;
        public int MaxDegreeParallelism = -1;
        /// <summary>
        /// Parallelism options for populating the List in <see cref="GetPage(int, bool)"/>
        /// </summary>
        //public static ParallelOptions ParallelismOptions { get; private set; } = new ParallelOptions();
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
            long offset = 0;
            if(pageNumber > 0)
            {
                for(int i = 0; i < pageNumber; i++)
                {
                    offset += Pages[i].RecordCount;
                }
            }
            //var lines = GetPageLines(pageNumber);
            var LineRecords = new ReadType[Pages[pageNumber].RecordCount];
            //List<ReadType> LineRecords = new List<ReadType>(Pages[pageNumber].RecordCount);            
#if DEBUG
            if (MaxDegreeParallelism == 1)
            {
                GetPageLines(pageNumber).ForEachIndex((line, idx) =>
                {
                    var rec = _MetaData.ParseRecord<ReadType>(line);
                    if (rec is DocEditor.IDetailDataRecord)
                    {
                        (rec as DocEditor.IDetailDataRecord).SetID(offset + idx);                        
                    }
                    LineRecords[idx] = rec;

                });

                CurrentPage = new List<ReadType>(LineRecords);
                return CurrentPage;
            }
#endif
            Parallel.ForEach(GetPageTupleLines(pageNumber), 
                new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeParallelism }, 
                line =>
            {
                var rec = _MetaData.ParseRecord<ReadType>(line.Item1);

                if (rec is DocEditor.IDetailDataRecord)
                {
                    (rec as DocEditor.IDetailDataRecord).SetID(offset + line.Item2);
                }
                LineRecords[line.Item2] = rec; //maintain original index.
            });
            
            CurrentPage = new List<ReadType>(LineRecords);
            return CurrentPage;
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
