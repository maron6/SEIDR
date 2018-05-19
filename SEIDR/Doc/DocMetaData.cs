using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{

    public class DocMetaData
    {
        /// <summary>
        /// Treats the MetaData as the underlying ColumnsCollection
        /// </summary>
        /// <param name="data"></param>
        public static implicit operator DocRecordColumnCollection(DocMetaData data)
        {
            return data.Columns;
        }        
        static bool _TestMode = false;
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
        /// If true, Columns are fixed width and will use lengths. <para>Otherwise, will use the delimiter.</para>
        /// </summary>
        public bool FixedWidthMode => Columns.FixedWidthMode;
        /// <summary>
        /// Tries to set the doc to fixed width mode. Returns true if value is updated.
        /// </summary>
        /// <param name="useFixedWidth"></param>
        /// <returns></returns>
        public bool TrySetFixedWidthMode(bool useFixedWidth)
        {
            Columns.CheckForFixedWidthValid();
            if (Columns.CanUseAsFixedWidth)
                Columns.FixedWidthMode = useFixedWidth;
            else if (!useFixedWidth)
                Columns.FixedWidthMode = false;
            else
                return false;
            return true;
        }
        /// <summary>
        /// File encoding
        /// </summary>
        public Encoding FileEncoding { get; set; } = Encoding.Default;
        /// <summary>
        /// Sets the file encoding for reading and writing.
        /// </summary>
        /// <param name="fileEncoding"></param>
        /// <returns></returns>
        public DocMetaData SetFileEncoding(Encoding fileEncoding)
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
        public DocMetaData SetPageSize(int pageSize)
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
        /// <summary>
        /// The columns from the file
        /// </summary>
        public DocRecordColumnCollection Columns { get; private set; }
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
        /// <summary>
        /// Columns are in a valid state.
        /// </summary>
        public bool HeaderConfigured => Columns.Valid;
        public string LineEndDelimiter => Columns.LineEndDelimiter;

        /// <summary>
        /// If there are multiple possible line endings when reading.
        /// </summary>
        public string[] MultiLineEndDelimiter { get; private set; } = new string[0];
        /// <summary>
        /// Clears the multli line end delimiter
        /// </summary>
        public void ClearMultiLineEndDelimiter() => MultiLineEndDelimiter = new string[0];
        /// <summary>
        /// Use if there may be a mixture of /r/n, /r, /n, etc   
        /// </summary>
        /// <param name="endings"></param>
        public DocMetaData SetMultiLineEndDelimiters(params string[] endings)
        {
            List<string> l;
            if (!string.IsNullOrEmpty(Columns.LineEndDelimiter))
                l = new List<string>(endings.Include(Columns.LineEndDelimiter));
            else
                l = new List<string>(endings);

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
        public DocMetaData AddMultiLineEndDelimiter(params string[] endingToAdd)
        {
            List<string> l;
            if (!string.IsNullOrEmpty(Columns.LineEndDelimiter))
                l = new List<string>(endingToAdd.Include(Columns.LineEndDelimiter));
            else
                l = new List<string>(endingToAdd);

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
        /// <summary>
        /// Access mode for file opening. Indicates whether the DocMetaData will be used for Doc reading or doc writing
        /// </summary>
        public FileAccess AccessMode { get; set; } = FileAccess.ReadWrite;
        /// <summary>
        /// If true, allow writing.
        /// </summary>
        public bool CanWrite { get; set; } = false;
        /// <summary>
        /// Sets <see cref="AccessMode"/>
        /// </summary>
        /// <param name="myAccess">Should match the DocReader or DocWriter.</param>
        ///// <param name="writeMode">If true, sets to <see cref="FileAccess.Write"/>. Otherwise, sets to <see cref="FileAccess.Read"/></param>
        /// <returns></returns>
        public DocMetaData SetFileAccess(FileAccess myAccess /*bool writeMode*/)
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
        public bool Valid
        {
            get
            {                
                if (AccessMode == FileAccess.Write && !HeaderConfigured)
                    return false;
                return !string.IsNullOrWhiteSpace(FilePath)
                    .And(File.Exists(FilePath).Or(AccessMode == FileAccess.Write))
                    .And(HasHeader.Or(HeaderConfigured));                    
            }
        }
        /// <summary>
        /// If true, first line of the file after skip lines should be the header. If the header has been configured already, this also means that an additional line will be skipped so that we don't read the header as a normal line.
        /// </summary>
        public bool HasHeader { get; set; } = true;
        /// <summary>
        /// Gets the delimiter from <see cref="Columns"/>
        /// </summary>
        public char? Delimiter => Columns.Delimiter;
        /// <summary>
        /// Treat empty records as null when getting values/hashes
        /// </summary>
        public bool EmptyIsNull { get; set; } = true;
        /// <summary>
        /// Creates meta data for the given file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="alias"></param>
        public DocMetaData(string file, string alias = null)
        {
            FilePath = file;
            if (string.IsNullOrWhiteSpace(alias))
                Alias = Path.GetFileNameWithoutExtension(file);
            else
                Alias = alias;
            Columns = new DocRecordColumnCollection(Alias);
        }
        /// <summary>
        /// Creates meta Data for the given file
        /// </summary>
        /// <param name="DirectoryPath"></param>
        /// <param name="FileName"></param>
        /// <param name="alias"></param>
        public DocMetaData(string DirectoryPath, string FileName, string alias = null)
            :this(Path.Combine(DirectoryPath, FileName), alias)
        {

        }
        /// <summary>
        /// Sets <see cref="HasHeader"/>
        /// </summary>
        /// <param name="headerIncluded"></param>
        /// <returns></returns>
        public DocMetaData SetHasHeader(bool headerIncluded)
        {
            HasHeader = headerIncluded;
            return this;
        }
        public string GetHeader()
        {
            return string.Format(Columns.format, Columns.Columns.Select(c => c.ColumnName).ToArray());
        }        
        /// <summary>
        /// Number of lines to skip at the start of the file when reading. Does not include the header's line
        /// </summary>
        public int SkipLines { get; set; } = 0;
        public DocMetaData SetSkipLines(int linesToSkip)
        {
            SkipLines = linesToSkip;
            return this;
        }
        public DocMetaData SetEmptyIsNull(bool nullifyEmpty)
        {
            EmptyIsNull = nullifyEmpty;
            return this;
        }
        public DocMetaData SetDelimiter(char delimiter)
        {
            Columns.SetDelimiter(delimiter);
            return this;
        }
        /// <summary>
        /// Sets the string that indicates end of records. Default is <see cref="Environment.NewLine"/>
        /// <para>If dealing with fixed width and records should be split only by length, set to null</para>
        /// </summary>
        /// <param name="endLine"></param>
        /// <returns></returns>
        public DocMetaData SetLineEndDelimiter(string endLine)
        {
            Columns.LineEndDelimiter = endLine;
            return this;
        }    
        /// <summary>
        /// Adds basic columns to be delimited by <see cref="Delimiter"/>.
        /// </summary>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public DocMetaData AddDelimitedColumns(params string[] columnNames)
        {
            foreach(var col in columnNames)
            {
                Columns.AddColumn(col);
            }
            return this;
        }
        /// <summary>
        /// Adds one or more populated <see cref="DocRecordColumnInfo"/> instances to the Columns Collection
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public DocMetaData AddDetailedColumn(params DocRecordColumnInfo[] column)
        {
            foreach(var col in column)
            {
                Columns.AddColumn(col);
            }
            return this;
        }
        /// <summary>
        /// Copies the columns from the collection to the end of this doc's column collection. 
        /// <para>Note: Will replace the alias.</para>
        /// </summary>
        /// <param name="columnCollection"></param>
        /// <returns></returns>
        public DocMetaData AddDetailedColumnCollection(DocRecordColumnCollection columnCollection)
        {
            foreach (var col in columnCollection)
            {
                Columns.AddColumn(col.ColumnName, col.MaxLength, col.EarlyTerminator, col.LeftJustify, col.TextQualify);
            }
            return this;
        }
        /// <summary>
        /// Remove the columns from the column collection. Should be done *before* reading or writing - may lead to DocRecords being in an inconsistent state otherwise.
        /// </summary>
        /// <param name="toRemove"></param>
        /// <returns></returns>
        public DocMetaData RemoveColumn(params DocRecordColumnInfo[] toRemove)
        {
            foreach(var c in toRemove)
            {
                Columns.RemoveColumn(c);
            }            
            return this;
        }
        /// <summary>
        /// Attempts to remove the specified column.
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="alias"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public DocMetaData RemoveColumn(string ColumnName, string alias = null, int position = -1)
        {
            Columns.RemoveColumn(alias, ColumnName, position);
            return this;
        }
        /// <summary>
        /// Copies the columns from the collection to the end of this doc's column collection. Maintains alias.
        /// </summary>
        /// <param name="columnCollection"></param>
        /// <returns></returns>
        public DocMetaData CopyDetailedColumnCollection(DocRecordColumnCollection columnCollection)
        {
            foreach (var col in columnCollection)
            {
                Columns.CopyColumnIntoCollection(col);
            }            
            return this;
        }
        /// <summary>
        /// Creates a new <see cref="DocRecordColumnInfo"/> and adds it to the Columns collection
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="MaxLength">optional limit</param>
        /// <param name="EarlyTerminator"></param>
        /// <returns></returns>
        public DocMetaData AddColumn(string ColumnName, int? MaxLength = null, string EarlyTerminator = null)
        {
            if (this.FixedWidthMode && MaxLength == null)
                throw new ArgumentNullException(nameof(MaxLength), $"MetaData indicates fixed width mode, but a length was not provided for new column '{ColumnName}'");
            Columns.AddColumn(ColumnName, MaxLength, EarlyTerminator);
            return this;
        }
        string _FileHash = null;
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
        /// Check if the file exists
        /// </summary>
        /// <returns></returns>
        public bool CheckExists()
        {            
            return File.Exists(FilePath);
        }

    }   
}
