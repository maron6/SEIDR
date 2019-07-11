using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// Base class for meta data when reading/writing files. 
    /// <para>Implementations should be internal only.</para>
    /// </summary>
    public abstract class MetaDataBase
    {
        /// <summary>
        /// Indicates that we should trust the preamble to cover any control characters at the beginning.
        /// <para>Any control characters beside the preamble are expected and for the data, if true.</para>
        /// </summary>
        public bool TrustPreamble { get; set; } = true;
        public MetaDataBase SetTrustPreamble(bool trust)
        {
            TrustPreamble = trust;
            return this;
        }
        /// <summary>
        /// Sets whether or not to allow a reader or writer to include null as a record.
        /// </summary>
        public bool AllowNullRecords { get; set; } = false;
        /// <summary>
        /// Sets whether or not to allow a reader or writer to include null as a record.
        /// </summary>
        /// <param name="allowNull"></param>
        /// <returns></returns>
        public MetaDataBase SetAllowNullRecords (bool allowNull)
        {
            AllowNullRecords = allowNull;
            return this;
        }
        /// <summary>
        /// Character to use for escaping quotes when splitting lines/columns.
        /// </summary>
        public char QuoteEscape = '\\';
        /// <summary>
        /// Sets the character for escaping the TextQualifier.
        /// </summary>
        /// <param name="escape"></param>
        /// <returns></returns>
        public MetaDataBase SetQuoteEscape(char escape)
        {
            QuoteEscape = escape;
            return this;
        }
        /// <summary>
        /// Cleans quotes out from a value, unless they're escaped or in the middle of a line.
        /// <para>Quotes that were escaped will have their escape removed.</para>
        /// <para>NOTE: This only does anything if <see cref="AllowQuoteEscape"/> is true (default = false)</para>
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public string CleanQuotes(string source)
        {
            if (string.IsNullOrEmpty(TextQualifier) || !AllowQuoteEscape)
                return source;
            StringBuilder sb = new StringBuilder();
            int current = 0;            
            int finish = source.Length;
            if(source.StartsWith(TextQualifier))            
                current = TextQualifier.Length;            
            if (source.EndsWith(TextQualifier) && source[source.Length - TextQualifier.Length - 1] != QuoteEscape)
                finish -= TextQualifier.Length;
            int nextQuote = source.IndexOf(TextQualifier, current);
            while (current < finish)
            {
                if(nextQuote == -1)
                {
                    sb.Append(source.Substring(current, finish - current));
                    break;
                }
                if (source[nextQuote - 1] == QuoteEscape)
                    sb.Append(source.Substring(current, nextQuote - 1 - current));
                else
                    sb.Append(source.Substring(current, nextQuote - current));
                sb.Append(TextQualifier);
                current = nextQuote + TextQualifier.Length;
                nextQuote = source.IndexOf(TextQualifier, current);                
                if (nextQuote == finish)
                {
                    //at the last quote, which ends the string value. Drop that one, unless preceded by a quote escape.
                    sb.Append(source.Substring(current, finish - current));
                    break; 
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// Escape the quotes within a string.
        /// /// <para>NOTE: This only does anything if <see cref="AllowQuoteEscape"/> is true (default = false)</para>
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public string EscapeQuoteValues(string source)
        {
            if (string.IsNullOrEmpty(TextQualifier) || !AllowQuoteEscape)
                return source;
            int nextQuote = source.IndexOf(TextQualifier);
            if (nextQuote < 0)
                return source;
            int fromPosition = 0;
            StringBuilder sb = new StringBuilder(source.Length);
            while(nextQuote >= 0)
            {
                sb.Append(source, fromPosition, nextQuote - fromPosition);
                if (source[nextQuote - 1] != QuoteEscape)
                    sb.Append(QuoteEscape);
                sb.Append(TextQualifier);
                fromPosition = nextQuote + TextQualifier.Length;
                nextQuote = source.IndexOf(TextQualifier, fromPosition);                
            }
            if (fromPosition < source.Length)
                sb.Append(source.Substring(fromPosition));
            return sb.ToString();
        }
        public bool AllowQuoteEscape { get; set; }
        public MetaDataBase SetAllowQuoteEscape(bool allow)
        {
            AllowQuoteEscape = allow;
            return this;
        }

        /// <summary>
        /// Perform basic checks to make sure meta Data is valid.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public bool CheckFormatValid(FileAccess mode)
        {
            if(mode == FileAccess.Read)
            {
                if (string.IsNullOrWhiteSpace(FilePath))
                    return false;
                if (!File.Exists(FilePath))
                    return false;
                if (HasHeader && !HeaderConfigured)
                    return false;
                if(Format != DocRecordFormat.FIX_WIDTH)
                {
                    if (LineEndDelimiter == null && !ReadWithMultiLineEndDelimiter)
                        return false;
                }
            }            
            switch (Format)
            {
                case DocRecordFormat.FIX_WIDTH:
                case DocRecordFormat.RAGGED_RIGHT:
                    {
                        return true;
                    }                                    
                case DocRecordFormat.DELIMITED:
                case DocRecordFormat.VARIABLE_WIDTH:
                    {
                        if (Delimiter == null)
                            return false;
                        if (string.IsNullOrEmpty(LineEndDelimiter))
                        {
                            if (!ReadWithMultiLineEndDelimiter)
                                return false;
                        }
                        else if (LineEndDelimiter == Delimiter.Value.ToString())
                            return false;
                        return true;
                    }
                case DocRecordFormat.SBSON:
                default:
                    return true;
            }
        }
        /// <summary>
        /// Gets the Column collection to associate with a line of the file.
        /// <para>Note: the parameter is called <paramref name="DocumentLine"/>, but it should also be okay to pass just the key column's value.</para>
        /// </summary>
        /// <param name="DocumentLine"></param>
        /// <returns></returns>
        public abstract DocRecordColumnCollection GetRecordColumnInfos(string DocumentLine);

        /// <summary>
        /// Gets the Column collection to associate with a line of the file.
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public abstract DocRecordColumnCollection GetRecordColumnInfos(IDataRecord record);
        /// <summary>
        /// Parses a line from a document and returns a DocRecord.
        /// </summary>
        /// <param name="DocumentLine"></param>
        /// <returns></returns>
        public DocRecord Parse(string DocumentLine)
        {
            return ParseRecord(CanWrite, DocumentLine);
        }

        /// <summary>
        /// Full path of the file being described.
        /// </summary>
        public readonly string FilePath;
        /// <summary>
        /// Gets the name of the directory for the specified path (<see cref="FilePath"/>)
        /// </summary>
        public string Directory => Path.GetDirectoryName(FilePath);
        /// <summary>
        /// Returns name of file at specified path. (<see cref="FilePath"/>)
        /// </summary>
        public string FileName => Path.GetFileName(FilePath);
        /// <summary>
        /// Used for associating column information to an originating file when merging column collections
        /// </summary>
        public readonly string Alias;
        internal MetaDataBase(string FilePath, string Alias)
        {
            this.FilePath = FilePath;
            if (string.IsNullOrWhiteSpace(Alias))
                this.Alias = Path.GetFileNameWithoutExtension(FilePath);
            else
                this.Alias = Alias;
        }
        #region Header
        /// <summary>
        /// If true, first line of the file after skip lines should be the header. If the header has been configured already, this also means that an additional line will be skipped so that we don't read the header as a normal line.
        /// </summary>
        public abstract bool HasHeader { get; }
        /// <summary>
        /// Indicates that the Header is ready to use, if <see cref="HasHeader"/> is true.
        /// </summary>
        public abstract bool HeaderConfigured { get; }
        /// <summary>
        /// Gets the header as a string. Should ONLY be called if HasHeader is true.
        /// </summary>
        /// <returns></returns>
        public abstract string GetHeader();

        #endregion

        /// <summary>
        /// Sets <see cref="DocRecordColumnCollection.DefaultNullIfEmpty"/> and <see cref="DocRecordColumnInfo.NullIfEmpty"/> for any columns associated with underlying collections.
        /// </summary>
        /// <param name="nullifyEmpty"></param>
        /// <param name="SetDefault">If false, just sets the values on individual columns, not the collection's default.</param>
        /// <returns></returns>
        public abstract MetaDataBase SetEmptyIsNull(bool nullifyEmpty, bool SetDefault = true);
        /// <summary>
        /// Sets <see cref="DocRecordColumnCollection.DefaultNullIfEmpty"/>, and conditionally sets <see cref="DocRecordColumnInfo.NullIfEmpty"/> for any columns that match the predicate.
        /// </summary>
        /// <param name="NullifyEmpty"></param>
        /// <param name="columnPredicate"></param>
        /// <param name="SetDefault">If false, just sets the values on individual columns, not the collection's default.</param>
        /// <returns></returns>
        public abstract MetaDataBase SetEmptyIsNull(bool NullifyEmpty, Predicate<DocRecordColumnInfo> columnPredicate, bool SetDefault = true);

        static bool _TestMode = false;
        /// <summary>
        /// Indicates if the MetaData is in a valid state for usage.
        /// </summary>

        public abstract bool Valid { get; }
        /// <summary>
        /// Removes minimum on PageSize. Setting pageSize below the min is silently ignored outside of TestMode
        /// </summary>
        public static bool TESTMODE
        {
            get { return _TestMode; }
            set
            {
#if DEBUG
                _TestMode = value;
                return;
#endif
                throw new InvalidOperationException("Test Mode can only be set when compiled in Debug mode.");
            }

        }
        /// <summary>
        /// Number of lines to skip at the start of the file when reading. Does not include the header's line
        /// </summary>
        public int SkipLines { get; set; } = 0;
        /// <summary>
        /// Set <see cref="SkipLines"/>
        /// </summary>
        /// <param name="linesToSkip"></param>
        /// <returns></returns>
        public MetaDataBase SetSkipLines(int linesToSkip)
        {
            SkipLines = linesToSkip;
            return this;
        }
        /// <summary>
        /// Indicates whether or to expect newlines in a text qualified column value.
        /// </summary>
        public bool AllowQuotedNewLine { get; set; } = false;
        /// <summary>
        /// Delimiter for columns
        /// </summary>
        public char? Delimiter { get; set; }
        /// <summary>
        /// Line Ending delimiter, unless <see cref="ReadWithMultiLineEndDelimiter"/> is true.
        /// <para>When writing to a file, this is always used, though.</para>
        /// <para>Set to <see cref="Environment.NewLine"/> by default.</para>
        /// </summary>
        public string LineEndDelimiter { get; set; } = Environment.NewLine;
        /// <summary>
        /// Set delimiter.
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public MetaDataBase SetDelimiter(char delimiter, DocRecordFormat format = DocRecordFormat.DELIMITED)
        {
            Delimiter = delimiter;
            return SetFormat(format);            
        }
        /// <summary>
        /// Sets the string that indicates end of records.
        /// </summary>
        /// <param name="endLine"></param>
        /// <returns></returns>
        public MetaDataBase SetLineEndDelimiter(string endLine)
        {
            LineEndDelimiter = endLine;
            return this;
        }

        #region Encoding + Page Size
        /// <summary>
        /// File encoding
        /// </summary>
        public Encoding FileEncoding { get; set; } = Encoding.Default;
        /// <summary>
        /// Sets the file encoding for reading and writing.
        /// </summary>
        /// <param name="fileEncoding"></param>
        /// <returns></returns>
        public MetaDataBase SetFileEncoding(Encoding fileEncoding)
        {
            FileEncoding = fileEncoding;
            return this;
        }
        /// <summary>
        /// Max number of characters to have in a page when reading from a file
        /// <para>Will throw an exception if the page is too small to completely parse a line somewhere in the file</para>
        /// </summary>
        public int PageSize { get; private set; } = DEFAULT_PAGE_SIZE;
        /// <summary>
        /// Default value for <see cref="PageSize"/> 
        /// </summary>
        public const int DEFAULT_PAGE_SIZE = 10000000;
        /// <summary>
        /// Minimum page size (in characters)
        /// </summary>
        public const int MIN_PAGE_SIZE = 1028;
        /// <summary>
        /// Sets <see cref="PageSize"/> 
        /// </summary>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public MetaDataBase SetPageSize(int pageSize)
        {
            if (!TESTMODE && pageSize < MIN_PAGE_SIZE)
            {
                System.Diagnostics.Debug.WriteLine($"Page Size is below minimum. Setting to {MIN_PAGE_SIZE}.");
                PageSize = MIN_PAGE_SIZE;
            }
            else
                PageSize = pageSize;
            return this;
        }
        #endregion
        #region FileAccess 
        /// <summary>
        /// Access mode for file opening. Indicates whether the DocMetaData will be used for Doc reading or doc writing
        /// </summary>
        public FileAccess AccessMode { get; set; } = FileAccess.ReadWrite;
        /// <summary>
        /// If true, allow writing.
        /// </summary>
        public bool CanWrite { get; set; } = false;
        /// <summary>
        /// Sets whether or not to allow Modifying DocRecords that are parsed using this metaData
        /// </summary>
        /// <param name="CanWrite"></param>
        /// <returns></returns>
        public MetaDataBase SetCanWrite(bool CanWrite)
        {
            this.CanWrite = CanWrite;
            return this;
        }
        /// <summary>
        /// Sets <see cref="AccessMode"/>
        /// </summary>
        /// <param name="myAccess">Should match the DocReader or DocWriter.</param>
        ///// <param name="writeMode">If true, sets to <see cref="FileAccess.Write"/>. Otherwise, sets to <see cref="FileAccess.Read"/></param>
        /// <returns></returns>
        public MetaDataBase SetFileAccess(FileAccess myAccess /*bool writeMode*/)
        {
            //AccessMode = writeMode ? FileAccess.Write : FileAccess.Read;
            AccessMode = myAccess;
            if (FileAccess.Write == (AccessMode & FileAccess.Write))
                CanWrite = true;
            return this;
        }
        /// <summary>
        /// Meta data can be used for reading or writing to a file.
        /// <para>Ensures that file path is valid if access mode includes read</para>
        /// </summary>
        #endregion

        #region multi line ending

        /// <summary>
        /// If there are multiple possible line endings when reading.
        /// </summary>
        public string[] MultiLineEndDelimiter { get; protected set; } = new string[0];
        /// <summary>
        /// Clears the multli line end delimiter
        /// </summary>
        public MetaDataBase ClearMultiLineEndDelimiter()
        {
            MultiLineEndDelimiter = new string[0];
            return this;
        }
        /// <summary>
        /// Use if there may be a mixture of /r/n, /r, /n, etc   
        /// </summary>
        /// <param name="endings"></param>
        public virtual MetaDataBase SetMultiLineEndDelimiters(params string[] endings)
        {
            List<string> l = new List<string>(endings);

            l.Sort((a, b) =>
            {
                if (a.IsSuperSet(b)) //Earlier sort position
                    return -1;
                if (a.IsSubset(b))
                    return 1;
                return 0;
            });
            MultiLineEndDelimiter = l.Where(ln => !string.IsNullOrEmpty(ln)).ToArray();
            return this;
        }
        /// <summary>
        /// Indicates if the MutliLineEnd Delimiter information should be used by DocReader instances. True if there is more than one line ending in the <see cref="MultiLineEndDelimiter"/> array.
        /// </summary>
        public bool ReadWithMultiLineEndDelimiter => MultiLineEndDelimiter.Length > 1;
        /// <summary>
        /// Adds the strings to <see cref="MultiLineEndDelimiter"/>, and sorts it so that super sets are earlier. 
        /// <para>E.g., ensures \r\n comes before \r or \n, while the order of \r and \n are arbitrary.</para>
        /// </summary>
        /// <param name="endingToAdd"></param>
        /// <returns></returns>
        public virtual MetaDataBase AddMultiLineEndDelimiter(params string[] endingToAdd)
        {
            List<string> l = new List<string>(endingToAdd);

            if (MultiLineEndDelimiter != null)
                l.AddRange(MultiLineEndDelimiter);
            l.Sort((a, b) =>
            {
                if (a == null || b == null)
                    return 0;
                if (a.IsSuperSet(b)) //Earlier sort position
                    return -1;
                if (a.IsSubset(b))
                    return 1;
                return 0;
            });
            MultiLineEndDelimiter = l.Where(ln => !string.IsNullOrEmpty(ln)).Distinct().ToArray();
            return this;
        }

        #endregion

        #region File MetaData
        long _Length = -1;
        string _FileHash = null;
        /// <summary>
        /// Gets the length of the file, cached.
        /// </summary>
        public long FileLength
        {
            get
            {
                if (_Length < 0)
                    _Length = new FileInfo(FilePath).Length;
                return _Length;                
            }
        }
        /// <summary>
        /// Returns a hash of the file content, based on <see cref="DocExtensions.GetFileHash(FileInfo)"/>
        /// </summary>
        public string FileHash
        {
            get
            {
                if (!Valid)
                    return null;
                if (_FileHash == null)
                    _FileHash = FilePath.GetFileHash();
                return _FileHash;
            }
        }
        /// <summary>
        /// Removes the cached filehash and returns a fresh evaluation of <see cref="FileHash"/>
        /// </summary>
        /// <returns></returns>
        public string RefreshFileHash()
        {
            _FileHash = null;
            return FileHash;
        }
        /// <summary>
        /// Removes the cached file length.
        /// </summary>
        /// <returns></returns>
        public long RefreshFileSize()
        {
            _Length = -1;
            return FileLength;
        }
        /// <summary>
        /// Check if the file exists
        /// </summary>
        /// <returns></returns>
        public bool CheckExists()
        {
            return File.Exists(FilePath);
        }


        #endregion


        #region Record Parse

        /// <summary>
        /// Format for reading/writing from a file.
        /// </summary>
        public virtual DocRecordFormat Format { get; protected set; } = DocRecordFormat.DELIMITED;
        /// <summary>
        /// Sets the <see cref="Format"/>. Implementations of MetaDataBase may perform validations when setting.
        /// </summary>
        /// <param name="NewFormat"></param>
        /// <returns></returns>
        public virtual MetaDataBase SetFormat(DocRecordFormat NewFormat)
        {
            Format = NewFormat;
            return this;
        }        
        /// <summary>
        /// Indicates whether or not the file should be treated as FixedWidth
        /// </summary>
        public bool FixWidthMode => Format == DocRecordFormat.FIX_WIDTH;
        /// <summary>
        /// Variable width - FixWidth limit but allow using delimiters to end a column early.
        /// </summary>
        public bool VariableWidthMode => Format == DocRecordFormat.VARIABLE_WIDTH;
        /// <summary>
        /// Fix width, but the last column does not need to be full length.
        /// </summary>
        public bool RaggedRightMode => Format == DocRecordFormat.RAGGED_RIGHT;
        /// <summary>
        /// Use when individual column metadata indicates to use text qualifiers to surround column content inside a delimited setting. Not used with FixWidth/Ragged Right
        /// <para>Note: Cannot be null - will default to " if not provided.</para>
        /// </summary>
        public string TextQualifier { get; private set; } = "\"";
        /// <summary>
        /// Sets the Text Qualifier for writing, or formatting a DocRecord as a string.
        /// </summary>
        /// <param name="TextQual"></param>
        /// <returns></returns>
        public MetaDataBase SetTextQualifier(string TextQual)
        {
            if(string.IsNullOrEmpty(TextQual))
                throw new ArgumentException("Text Qualifier cannot be empty.", nameof(TextQual));
            TextQualifier = TextQual;
            return this;
        }        
        /// <summary>
        /// Doesn't throw <see cref="MissingColumnException"/> when the number of columns is lower than expected.
        /// </summary>
        public bool AllowMissingColumns { get; set; } = false;
        /// <summary>
        /// Parses a DocRecord out of the string. The string should end at <see cref="LineEndDelimiter"/>, but not include it.
        /// </summary>
        /// <param name="writeMode"></param>
        /// <param name="record"></param>
        /// <returns></returns>
        public DocRecord ParseRecord(bool writeMode, string record)
        {
            if (string.IsNullOrEmpty(record))
                return null;
            if (!Valid)
                throw new InvalidOperationException("Collection state is not valid.");
            var colSet = GetRecordColumnInfos(record);
            if (colSet == null)
                return null;
            if(Format == DocRecordFormat.SBSON)
            {
                return ParseBSON<DocRecord>(writeMode, record);
            }
            var Columns = colSet.Columns;
            string[] split = new string[colSet.LastPosition];
            int position = 0;
            for (int i = 0; i < colSet.Columns.Count; i++)
            {
                if (position >= record.Length)
                {
                    //have gone beyond length of record
                    if (ThrowExceptionColumnCountMismatch)
                        throw new MissingColumnException(i, colSet.Columns.Count - 1);
                    break;
                }
                if (FixWidthMode || RaggedRightMode)
                {
                    int x = colSet.Columns[i].MaxLength.Value;
                    if (x + position > record.Length)
                        x = record.Length - position; //Number of characters to read                                        
                    split[i] = record.Substring(position, x);
                    position += x;
                    if (ThrowExceptionColumnCountMismatch && i == colSet.Columns.Count - 1 && position < record.Length)
                        throw new ColumnOverflowException(record.Length - position, colSet.Columns.Count, record.Length);
                }
                else if (VariableWidthMode) //Almost like delimited mode, but columns have a max length..
                {
                    int x = record.IndexOf(Delimiter.Value, position) - position;
                    int y = Columns[i].MaxLength ?? x;
                    if (y < x || x < 0)
                    {
                        x = y;
                        y = 0;
                    }
                    else
                        y = 1; //Skip delimiter          
                    if (y < 0)
                    {
                        if (i == Columns.Count - 1)
                        {
                            split[i] = record.Substring(position);
                            break;
                        }
                        throw new VariableColumnNotFoundException(record.Length - position, i, Columns.Count, record.Length, Delimiter.Value);
                    }
                    if (x + position > record.Length)
                        x = record.Length - position; //Number of characters to read                                                                
                    split[i] = CleanQuotes(record.Substring(position, x));
                    position += x + y;
                    if (ThrowExceptionColumnCountMismatch && i == Columns.Count - 1 && position < record.Length)
                        throw new ColumnOverflowException(record.Length - position, Columns.Count, record.Length);
                }
                else
                {
                    //split = record.SplitOutsideQuotes(Delimiter.Value, TextQualifier);

                    var source = FormatHelper.DelimiterHelper.EnumerateSplits(record, Delimiter.ToString(), true, this, false);
                    split = (from string s in source
                            select CleanQuotes(s)).ToArray();                    
                    
                    if (ThrowExceptionColumnCountMismatch)
                    {
                        if (split.Length < Columns.Count)
                        {
                            if (!AllowMissingColumns)
                                throw new MissingColumnException(split.Length, Columns.Count);
                        }
                        else if (split.Length > Columns.Count)
                            throw new ColumnOverflowException(split.Length, Columns.Count);
                    }
                    break;
                }

            }
            DocRecord r = new DocRecord(colSet, writeMode, split);
            return r;
        }

        /// <summary>
        /// Parses a DocRecord out of the string. The string should end at <see cref="LineEndDelimiter"/>, but not include it.
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public ReadType ParseRecord<ReadType>(string record) where ReadType : IDataRecord, new()
        {
            return ParseRecord<ReadType>(CanWrite, record);
        }
        /// <summary>
        /// Parses a DocRecord out of the string. The string should end at <see cref="LineEndDelimiter"/>, but not include it.
        /// </summary>
        /// <param name="writeMode"></param>
        /// <param name="record"></param>
        /// <returns></returns>
        public ReadType ParseRecord<ReadType>(bool writeMode, string record) where ReadType : IDataRecord, new()
        {
            if (string.IsNullOrEmpty(record))
                return default;
            if (!Valid)
                throw new InvalidOperationException("Collection state is not valid.");
            if(Format == DocRecordFormat.SBSON)
            {
                return ParseBSON<ReadType>(writeMode, record);
            }
            var colSet = GetRecordColumnInfos(record);
            var Columns = colSet.Columns;
            string[] split = new string[colSet.LastPosition];
            int position = 0;
            for (int i = 0; i < colSet.Columns.Count; i++)
            {
                if (position >= record.Length)
                {
                    //have gone beyond length of record
                    if (ThrowExceptionColumnCountMismatch)
                        throw new MissingColumnException(i, colSet.Columns.Count - 1);
                    break;
                }
                if (FixWidthMode || RaggedRightMode)
                {
                    int x = Columns[i].MaxLength.Value;
                    if (x + position > record.Length)
                        x = record.Length - position; //Number of characters to read                                        
                    split[i] = record.Substring(position, x);
                    position += x;
                    if (ThrowExceptionColumnCountMismatch && i == Columns.Count - 1 && position < record.Length) //Extends beyond the last column
                        throw new ColumnOverflowException(record.Length - position, Columns.Count, record.Length);
                }
                else if (VariableWidthMode) //Almost like delimited mode, but columns have a max length..
                {
                    int x = record.IndexOf(Delimiter.Value, position) - position;
                    int y = Columns[i].MaxLength ?? x;
                    if (y < x || x < 0)
                    {
                        x = y;
                        y = 0;
                    }
                    else
                        y = 1; //Skip delimiter          
                    if (y < 0)
                    {
                        if (i == Columns.Count - 1)
                        {
                            split[i] = record.Substring(position);
                            break;
                        }
                        throw new VariableColumnNotFoundException(record.Length - position, i, Columns.Count, record.Length, Delimiter.Value);
                    }
                    if (x + position > record.Length)
                        x = record.Length - position; //Number of characters to read                                                                
                    split[i] = CleanQuotes(record.Substring(position, x));
                    position += x + y;
                    if (ThrowExceptionColumnCountMismatch && i == Columns.Count - 1 && position < record.Length)
                        throw new ColumnOverflowException(record.Length - position, Columns.Count, record.Length);
                }
                else
                {
                    //split = record.SplitOutsideQuotes(Delimiter.Value, TextQualifier);

                    var source = FormatHelper.DelimiterHelper.EnumerateSplits(record, Delimiter.ToString(), true, this, false);
                    split = (from string s in source
                             select CleanQuotes(s)).ToArray();

                    if (ThrowExceptionColumnCountMismatch)
                    {
                        if (split.Length < Columns.Count)
                        {
                            if (!AllowMissingColumns)
                                throw new MissingColumnException(split.Length, Columns.Count);
                        }
                        else if (split.Length > Columns.Count)
                            throw new ColumnOverflowException(split.Length, Columns.Count);
                    }
                    break;
                }

            }
            ReadType r = new ReadType();
            r.Configure(colSet, writeMode, split);
            return r;
        }

        /// <summary>
        /// Parses the record into a list of strings, for use with one of the DocRecord constructors (mainly, when using a class that inherits from DocRecord) 
        /// </summary>
        /// <param name="record"></param>
        /// <returns>IList of strings separated by delimiter. Or based on column sizes in fixed width mode.</returns>
        public IList<string> ParseRecord(string record)
        {
            if (string.IsNullOrEmpty(record))
                return null;
            if (!Valid)
                throw new InvalidOperationException("Collection state is not valid.");
            var colSet = GetRecordColumnInfos(record);
            var byteSet = FileEncoding.GetBytes(record);
            var Columns = colSet.Columns;
            string[] split = new string[colSet.LastPosition];
            int position = 0;
            for (int i = 0; i < Columns.Count; i++)
            {
                if (position >= record.Length)
                {
                    //have gone beyond length of record
                    if (ThrowExceptionColumnCountMismatch)
                        throw new MissingColumnException(i, Columns.Count - 1);
                    break;
                }
                if (FixWidthMode || RaggedRightMode)
                {
                    int x = Columns[i].MaxLength.Value;
                    if (x + position > record.Length)
                        x = record.Length - position; //Number of characters to read                                        
                    split[i] = record.Substring(position, x);
                    position += x;
                    if (ThrowExceptionColumnCountMismatch && i == Columns.Count - 1 && position < record.Length)
                        throw new ColumnOverflowException(record.Length - position, Columns.Count, record.Length);
                }                
                else if(Format == DocRecordFormat.SBSON)
                {                    
                    DocRecordColumnType dataType;
                    var temp = SBSONHelper.GetValue(byteSet, ref position, out dataType, FileEncoding);
                    var colResult = dataType.FormatObject(temp);

                    if (dataType == DocRecordColumnType.Unknown)
                    {
                        split[i] = colResult;
                    }
                    else
                    {
                        var expected = colSet.Columns[i].DataType;
                        if (expected == DocRecordColumnType.Unknown || expected == dataType)
                            split[i] = colResult;
                        else
                            System.Diagnostics.Debug.WriteLine("Data Type Mismatch ({2}): Expected {0}, Found {1}", expected, dataType, colSet.Columns[i].ColumnName);
                    }
                }
                else if (VariableWidthMode) //Almost like delimited mode, but columns have a max length..
                {
                    int x = record.IndexOf(Delimiter.Value, position) - position;
                    int y = Columns[i].MaxLength ?? x;
                    if (y < x || x < 0)
                    {
                        x = y;
                        y = 0;
                    }
                    else
                        y = 1; //Skip delimiter          
                    if (y < 0)
                    {
                        if (i == Columns.Count - 1)
                        {
                            split[i] = record.Substring(position);
                            break;
                        }
                        throw new VariableColumnNotFoundException(record.Length - position, i, Columns.Count, record.Length, Delimiter.Value);
                    }
                    if (x + position > record.Length)
                        x = record.Length - position; //Number of characters to read                                                                
                    split[i] = CleanQuotes(record.Substring(position, x));
                    position += x + y;
                    if (ThrowExceptionColumnCountMismatch && i == Columns.Count - 1 && position < record.Length)
                        throw new ColumnOverflowException(record.Length - position, Columns.Count, record.Length);
                }
                else
                {
                    //split = record.SplitOutsideQuotes(Delimiter.Value, TextQualifier);

                    var source = FormatHelper.DelimiterHelper.EnumerateSplits(record, Delimiter.ToString(), true, this, false);
                    split = (from string s in source
                             select CleanQuotes(s)).ToArray();
                    //Note that CleanQuotes does nothing if QuoteEscape isn't turned on.

                    if (ThrowExceptionColumnCountMismatch)
                    {
                        if (split.Length < Columns.Count)
                        {
                            if (!AllowMissingColumns)
                                throw new MissingColumnException(split.Length, Columns.Count);
                        }
                        else if (split.Length > Columns.Count)
                            throw new ColumnOverflowException(split.Length, Columns.Count);
                    }
                    break;
                }
            }
            return split;
        }
        
  

        /// <summary>
        /// If true, throws an exception if the size of a record is too big or too small, based on number of records.
        /// <para>If false, ignores extra columns, and missing columns are treated as null</para>
        /// </summary>
        public bool ThrowExceptionColumnCountMismatch { get; set; } = true;
        /// <summary>
        /// Set whether or not to throw exceptions if the column count doesn't match expected.
        /// </summary>
        /// <param name="throwMismatch"></param>
        /// <returns></returns>
        public MetaDataBase SetThrowExceptionOnColumnCountMismatch(bool throwMismatch)
        {
            ThrowExceptionColumnCountMismatch = throwMismatch;
            return this;
        }
        public byte[] GetRecordByteSet(IDataRecord record)
        {
            return FileEncoding.GetBytes(FormatRecord(record, true));
        }      
        public byte[] GetRecordByteSet(IEnumerable<IDataRecord> recordList)
        {
            StringBuilder sb = new StringBuilder();
            foreach(var rec in recordList)
            {
                sb.Append(FormatRecord(rec, true));
            }
            return FileEncoding.GetBytes(sb.ToString());
        }
        public virtual int CheckByteCount(IDataRecord record)
        {
            return FileEncoding.GetByteCount(FormatRecord(record, true)); 
            //Can override this to be more efficient in DocMetaData, but here we might need the string for getting column set.
        }
        #endregion
        /// <summary>
        /// Maps DocRecord to a string for writing out into a file.
        /// </summary>
        /// <param name="record"></param>
        /// <param name="IncludeLineEndDelimiter"></param>
        /// <param name="columnMapping"></param>
        /// <returns></returns>
        public string FormatRecord(IDataRecord record, bool IncludeLineEndDelimiter, IDictionary<int, DocRecordColumnInfo> columnMapping)
        {
            if (Format == DocRecordFormat.SBSON)
                return FormatBSON(record, IncludeLineEndDelimiter, columnMapping);
            if(columnMapping == null || columnMapping.Count == 0)
            {
                return FormatRecord(record, IncludeLineEndDelimiter);
            }
            StringBuilder sb = new StringBuilder();
            //var Columns = record.Columns;
            var Columns = GetRecordColumnInfos(record) ?? record.Columns; //Prefer Columns from this meta data when possible, for formatting + Mapping purposes. (E.g., may be taking in a record that has more columns compared to what we want to write, or different date format)
            int Last = Columns.LastPosition.MaxOfComparison(columnMapping.Max(k => k.Key));
            for(int idx = 0; idx <= Last; idx++)             
            {
                DocRecordColumnInfo col = null;
                string s = string.Empty;
                if (columnMapping != null && columnMapping.ContainsKey(idx))
                    col = columnMapping[idx];                
                else if(idx < Columns.Count)
                    col = Columns[idx];
                /*
                 * else 
                 * {
                 *  col = null, s = string.Empty; //Mapping goes above original col count. Put blanks in between.
                 * }
                 */
                if (col != null)
                {
                    object o;
                    if (record.TryGet(col.ColumnName, out o, col.OwnerAlias))
                        s = col.FormatValue(o) ?? string.Empty;
                    else
                        s = string.Empty;
                    //s = record.GetBestMatch(col.ColumnName, col.OwnerAlias) ?? string.Empty;
                }

                if (FixWidthMode || RaggedRightMode)
                {
                    if (col == null)
                        throw new Exception("Mapping column collection does not include a column with position " + idx);
                    if (RaggedRightMode && idx == Columns.LastPosition)
                        sb.Append(s);
                    else if (col.LeftJustify)
                        sb.Append(s.PadRight(col.MaxLength.Value));
                    else
                        sb.Append(s.PadLeft(col.MaxLength.Value));
                }
                else if (VariableWidthMode)
                {
                    if (col == null)
                    {
                        if (idx < Last)
                            sb.Append(Delimiter.Value);
                    }
                    else if (!col.MaxLength.HasValue || s.Length < col.MaxLength.Value || idx == Columns.LastPosition)
                    {
                        if (col.TextQualify)
                            sb.Append(TextQualifier);
                        else if(col.CheckNeedTextQualifier(Delimiter, s) || col.CheckNeedTextQualifier(LineEndDelimiter, s))                             
                        {
                            sb.Append(TextQualifier);
                            col.TextQualify = true; //force text qualify in the column going forward.
                        }
                        sb.Append(EscapeQuoteValues(s));
                        if (col.TextQualify)
                            sb.Append(TextQualifier);
                        if (idx < Last)
                            sb.Append(Delimiter.Value);
                    }
                    else
                    {
                        sb.Append(s.Substring(0, col.MaxLength.Value));
                    }
                }
                else
                {
                    if (col != null)
                    {
                        if (col.TextQualify)
                            sb.Append(TextQualifier);
                        else if (col.CheckNeedTextQualifier(Delimiter, s) || col.CheckNeedTextQualifier(LineEndDelimiter, s))
                        {
                            sb.Append(TextQualifier);
                            col.TextQualify = true; //force text qualify in the column going forward.
                        }
                        sb.Append(EscapeQuoteValues(s)); //if col == null, then this is going to be empty string.
                        if (col.TextQualify)
                            sb.Append(TextQualifier);
                    }

                    if (idx < Last)
                        sb.Append(Delimiter.Value);
                }
            }
            if (IncludeLineEndDelimiter)
            {
                CheckAddLineDelimiter(sb);
            }
            return sb.ToString();
        }
        /// <summary>
        /// Links a column set to the metadata, overriding whatever the current column set is.
        /// </summary>
        /// <param name="columnSet"></param>
        /// <returns></returns>
        public abstract MetaDataBase LinkColumnSet(DocRecordColumnCollection columnSet);
        /// <summary>
        /// Conditionally adds a LineEnd delimiter.
        /// <para>If <see cref="LineEndDelimiter"/> is not null, then that will be added.</para>
        /// <para>Else, will add <see cref="Environment.NewLine"/>, unless the <see cref="Format"/> is either <see cref="DocRecordFormat.FIX_WIDTH"/> or <see cref="DocRecordFormat.SBSON"/></para>
        /// </summary>
        /// <param name="sb"></param>
        public void CheckAddLineDelimiter(StringBuilder sb)
        {
            if (Format.In(DocRecordFormat.BSON, DocRecordFormat.SBSON)) //Each record or "document" should be prepended with length. No newline.
                return;
            string le = LineEndDelimiter;
            if (le != null)
            {
                sb.Append(le);
                return;
            }
            if (FixWidthMode)
                return;
            sb.Append(Environment.NewLine);
        }
        /// <summary>
        /// Gets a basic DocRecord to that can be used (e.g. for adding new content to a DocWriter)
        /// </summary>
        /// <returns></returns>
        public virtual DocRecord GetBasicRecord() 
        {
            var colSet = GetRecordColumnInfos((string) null);
            return new DocRecord(colSet) { CanWrite = CanWrite };
        }
        /// <summary>
        /// Gets a basic TypedDataRecord that can be used.
        /// </summary>
        /// <returns></returns>
        public virtual TypedDataRecord GetBasicTypedDataRecord()
        {
            var colSet = GetRecordColumnInfos((string) null);
            return new TypedDataRecord(colSet) { CanWrite = CanWrite };
        }
        
        protected ReadType ParseBSON<ReadType>(bool WriteMode, string Record) where ReadType : IDataRecord, new()
        {
            var colSet = GetRecordColumnInfos(Record);
            var result = new ReadType();
            var byteSet = FileEncoding.GetBytes(Record);
            object[] content = new object[colSet.Columns.Count];
            int position = 0;
            for (int i = 0; i < colSet.Columns.Count; i++)
            {
                if(position > byteSet.Length)
                {
                    if(AllowMissingColumns)
                        break;
                    throw new MissingColumnException(i, colSet.Columns.Count - 1);
                }
                DocRecordColumnType dataType;
                var colResult = SBSONHelper.GetValue(byteSet, ref position, out dataType, FileEncoding);
                if(dataType == DocRecordColumnType.Unknown)
                {
                    content[i] = colResult;
                }
                else if(dataType == DocRecordColumnType.NUL)
                {
                    content[i] = null;
                }
                else
                {                    
                    var expected = colSet.Columns[i].DataType;
                    if (expected == DocRecordColumnType.Unknown)
                    {
                        content[i] = colResult;
                        colSet.Columns[i].DataType = dataType;
                    }
                    else if (expected == dataType)
                        content[i] = colResult;
                    else
                        System.Diagnostics.Debug.WriteLine("Data Type Mismatch ({2}): Expected {0}, Found {1}", expected, dataType, colSet.Columns[i].ColumnName);
                }
                
            }
            result.Configure(colSet, WriteMode, content);
            //result.SetParsedContent(content);
            return result;
        }
        /// <summary>
        /// Formats a string for a null record.
        /// </summary>
        /// <returns></returns>
        public string FormatNullRecord()
        {
            StringBuilder sb = new StringBuilder();
            switch (Format)
            {
                case DocRecordFormat.BSON:
                    return FileEncoding.GetString(new byte[] { 0, 0, 0, 0 });
                case DocRecordFormat.FIX_WIDTH:
                case DocRecordFormat.RAGGED_RIGHT:
                    if (LineEndDelimiter != null)
                    {
                        var colSource = GetRecordColumnInfos(null as string);
                        sb.Append(' ', colSource.MaxLength);
                    }
                    CheckAddLineDelimiter(sb);
                    break;
                case DocRecordFormat.VARIABLE_WIDTH:
                case DocRecordFormat.DELIMITED:
                default:
                    CheckAddLineDelimiter(sb);
                    break;
            }
            return sb.ToString();
        }
        protected string FormatBSON(IDataRecord record, bool IncludeLineEndDelimiter)
        {
            StringBuilder sb = new StringBuilder();
            var Columns = GetRecordColumnInfos(record) ?? record.Columns; 
            //Prefer column information from this meta Data when formatting to write.
            int len = 0;
            Columns.ForEachIndex((col, idx) =>
            {
                object o;
                if (record.TryGet(col, out o))
                {
                    sb.Append(SBSONHelper.SetResult(col, o, FileEncoding, ref len));
                }
                else
                    sb.Append(SBSONHelper.SetResult(col, null, FileEncoding, ref len));
            });
            //if (IncludeLineEndDelimiter)
            //    CheckAddLineDelimiter(sb);
            var prefix = BitConverter.GetBytes(len);
            return FileEncoding.GetString(prefix) + sb.ToString();
        }
        protected string FormatBSON(IDataRecord record, bool IncludeLineEndDelimiter, IDictionary<int, DocRecordColumnInfo> columnMapping)
        {
            if (columnMapping == null || columnMapping.Count == 0)
                return FormatBSON(record, IncludeLineEndDelimiter);
            StringBuilder sb = new StringBuilder();
            var Columns = record.Columns;
            int byteCount = 0;
            int Last = Columns.LastPosition.MaxOfComparison(columnMapping.Max(k => k.Key));
            for (int idx = 0; idx <= Last; idx++)
            {
                DocRecordColumnInfo col;
                if (columnMapping != null && columnMapping.ContainsKey(idx))
                    col = columnMapping[idx];
                else
                    col = Columns[idx];
                object o;
                if (record.TryGet(col, out o))
                {
                    sb.Append(SBSONHelper.SetResult(col, o, FileEncoding, ref byteCount));
                }
                else
                    sb.Append(SBSONHelper.SetResult(col, null, FileEncoding, ref byteCount));                                
            }
            //if (IncludeLineEndDelimiter)
            //    CheckAddLineDelimiter(sb);
            var prefix = BitConverter.GetBytes(byteCount);
            return FileEncoding.GetString(prefix) + sb.ToString();
        }
        /// <summary>
        /// Formats a record for writing to output.
        /// </summary>
        /// <param name="record"></param>
        /// <param name="IncludeLineEndDelimiter"></param>
        /// <returns></returns>
        public string FormatRecord(IDataRecord record, bool IncludeLineEndDelimiter)
        {
            if (Format == DocRecordFormat.SBSON)
                return FormatBSON(record, IncludeLineEndDelimiter);
            StringBuilder sb = new StringBuilder();
            //var Columns = record.Columns;
            var Columns = GetRecordColumnInfos(record) ?? record.Columns; //Prefer column data from this metaData if possible for formatting purposes.
            Columns.ForEachIndex((col, idx) =>
            {
                object o;
                string s;
                if (record.TryGet(col, out o))
                    s = col.FormatValue(o) ?? string.Empty;
                else
                    s = string.Empty;

                if (FixWidthMode || RaggedRightMode)
                {
                    if (RaggedRightMode && idx == Columns.LastPosition)
                        sb.Append(s);
                    else if (col.LeftJustify)
                        sb.Append(s.PadRight(col.MaxLength.Value));
                    else
                        sb.Append(s.PadLeft(col.MaxLength.Value));
                }
                else if (VariableWidthMode)
                {
                    if(!col.MaxLength.HasValue || s.Length < col.MaxLength.Value || idx == Columns.LastPosition)
                    {
                        if (col.TextQualify)
                            sb.Append(TextQualifier);
                        else if (s.Contains(Delimiter.Value))
                        {
                            sb.Append(TextQualifier);
                            col.TextQualify = true; //force text qualify in the column going forward.
                        }
                        sb.Append(EscapeQuoteValues(s));
                        if (col.TextQualify)
                            sb.Append(TextQualifier);
                        if (idx < Columns.LastPosition)
                            sb.Append(Delimiter.Value);
                    }
                    else
                    {                        
                        sb.Append(s.Substring(0, col.MaxLength.Value));
                    }
                }
                else
                {
                    if (col.TextQualify)
                        sb.Append(TextQualifier);
                    else if (s.Contains(Delimiter.Value))
                    {
                        sb.Append(TextQualifier);
                        col.TextQualify = true; //force text qualify in the column going forward.
                    }

                    sb.Append(EscapeQuoteValues(s));
                    if (col.TextQualify)
                        sb.Append(TextQualifier);
                    if (idx < Columns.LastPosition)
                        sb.Append(Delimiter.Value);
                }

            });
            if (IncludeLineEndDelimiter)
                CheckAddLineDelimiter(sb);
            return sb.ToString();
        }
    }
}
