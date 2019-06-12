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
        /// Gets the Column collection to associate with a line of the file.
        /// </summary>
        /// <param name="DocumentLine"></param>
        /// <returns></returns>
        public abstract DocRecordColumnCollection GetRecordColumnInfos(string DocumentLine);

        /// <summary>
        /// Gets the Column collection to associate with a line of the file.
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public abstract DocRecordColumnCollection GetRecordColumnInfos(IRecord record);
        /// <summary>
        /// Parses a line from a document and returns a DocRecord.
        /// </summary>
        /// <param name="DocumentLine"></param>
        /// <returns></returns>
        public DocRecord Parse(string DocumentLine)
        {
            var col = GetRecordColumnInfos(DocumentLine);
            if (col != null)
                return col.ParseRecord(this.CanWrite, DocumentLine);
            return null;
        }
        /// <summary>
        /// Parses a line from a Document and returns a DocRecord derived class instance.
        /// </summary>
        /// <typeparam name="ReadType"></typeparam>
        /// <param name="DocumentLine"></param>
        /// <returns></returns>
        public ReadType Parse<ReadType>(string DocumentLine) where ReadType: DocRecord, new()
        {
            var col = GetRecordColumnInfos(DocumentLine);
            if (col != null)
                return col.ParseRecord<ReadType>(CanWrite, DocumentLine);
            return null;
        }
        public IList<string> ParseRecord(string record)
        {
            var col = GetRecordColumnInfos(record);
            if (col != null)
                return col.ParseRecord(record);
            return null;
        }
        public DocRecord ParseRecord(bool writeMode, string record)
        {
            var col = GetRecordColumnInfos(record);
            if (col != null)
                return col.ParseRecord(writeMode, record);
            return null;
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
        /// Default value for <see cref="DocRecordColumnCollection.NullIfEmpty"/> for underlying column collections.
        /// </summary>
        public bool EmptyIsNull { get; set; } = true;
        /// <summary>
        /// Sets <see cref="EmptyIsNull"/>
        /// </summary>
        /// <param name="nullifyEmpty"></param>
        /// <returns></returns>
        public MetaDataBase SetEmptyIsNull(bool nullifyEmpty)
        {
            EmptyIsNull = nullifyEmpty;
            return this;
        }

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
        public abstract char? Delimiter { get; }
        /// <summary>
        /// Line Ending delimiter, unless <see cref="ReadWithMultiLineEndDelimiter"/> is true.
        /// </summary>
        public abstract string LineEndDelimiter { get; }
        /// <summary>
        /// Set delimiter.
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public abstract MetaDataBase SetDelimiter(char delimiter);
        /// <summary>
        /// Sets the string that indicates end of records.
        /// </summary>
        /// <param name="endLine"></param>
        /// <returns></returns>
        public abstract MetaDataBase SetLineEndDelimiter(string endLine);

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
        public void ClearMultiLineEndDelimiter() => MultiLineEndDelimiter = new string[0];
        /// <summary>
        /// Use if there may be a mixture of /r/n, /r, /n, etc   
        /// </summary>
        /// <param name="endings"></param>
        public virtual MetaDataBase SetMultiLineEndDelimiters(params string[] endings)
        {
            List<string> l  = new List<string>(endings);

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
            List<string> l  = new List<string>(endingToAdd);

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
        /// Removes the cached filehash and returns a fresh evaluation of <see cref="FileHash"/>
        /// </summary>
        /// <returns></returns>
        public string RefreshFileHash()
        {
            _FileHash = null;
            return FileHash;
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
    }
}
