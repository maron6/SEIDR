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
        public static bool TESTMODE = false;
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
        public int PageSize { get; private set; } = 10000000; 
        /// <summary>
        /// Minimum page size (in characters)
        /// </summary>
        public const int minPageSize = 1028;
        public DocMetaData SetPageSize(int pageSize)
        {
            if (!TESTMODE && pageSize < minPageSize)
            {
                System.Diagnostics.Debug.WriteLine($"Page Size is below minimum. Setting to {minPageSize}.");
                PageSize = minPageSize;
            }
            else
                PageSize = pageSize;
            return this;
        }
        /// <summary>
        /// The columns from the file
        /// </summary>
        public DocRecordColumnCollection Columns { get; private set; }
        public readonly string FilePath;
        public readonly string Alias;
        public bool HeaderConfigured => Columns.Valid;
        public string LineEndDelimiter => Columns.LineEndDelimiter;
        /// <summary>
        /// Access mode for file opening. Indicates whether the DocMetaData will be used for Doc reading or doc writing
        /// </summary>
        public FileAccess AccessMode { get; set; } = FileAccess.ReadWrite;
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
        /// If true, first line of the file after skip lines should be the header
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
        public DocMetaData(string file, string alias)
        {
            FilePath = file;
            if (string.IsNullOrWhiteSpace(alias))
                Alias = System.IO.Path.GetFileNameWithoutExtension(file);
            else
                Alias = alias;
            Columns = new DocRecordColumnCollection();
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
    }   
}
