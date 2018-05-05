using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
  
    /// <summary>
    /// Default DocSorter, for <see cref="DocRecord"/> collection
    /// </summary>
    public class DocSorter : DocSorter<DocRecord>
    {
        /// <summary>
        /// Constructor with DocReader, columns to sort on
        /// </summary>
        /// <param name="source"></param>
        /// <param name="mainSort">Sort columns</param>
        public DocSorter(DocReader source, params IRecordColumnInfo[] mainSort)
            :base(source, mainSort)
        {
        }
        /// <summary>
        /// Constructor with DocReader, columns to sort on
        /// </summary>
        /// <param name="source"></param>
        /// <param name="createIndexFile">If true, will auto create the index at the end of construction</param>
        /// <param name="mainSort">Sort columns</param>
        public DocSorter(DocReader source, bool createIndexFile, params IRecordColumnInfo[] mainSort)
            : base(source, createIndexFile, mainSort)
        {
        }
        /// <summary>
        /// Constructor with DocReader, columns to sort on
        /// </summary>
        /// <param name="source"></param>
        /// <param name="pageCacheSize"></param>
        /// <param name="createIndexFile">If true, will auto create the index at the end of construction</param>
        /// <param name="disposeCleansIndex">If true, <see cref="DocSorter{DocRecord}.Dispose()"/> will also delete the sort index file.</param>
        /// <param name="mainSort">Sort columns</param>
        public DocSorter(DocReader source, int pageCacheSize, bool createIndexFile, bool disposeCleansIndex,
             params IRecordColumnInfo[] mainSort)
            : base(source, pageCacheSize, createIndexFile, disposeCleansIndex, DuplicateHandling.Ignore, mainSort)
        {
        }
        /// <summary>
        /// Constructor with DocReader, columns to sort on
        /// </summary>
        /// <param name="source"></param>
        /// <param name="pageCacheSize"></param>
        /// <param name="createIndexFile">If true, will auto create the index at the end of construction</param>
        /// <param name="disposeCleansIndex">If true, <see cref="DocSorter{DocRecord}.Dispose()"/> will also delete the sort index file.</param>
        /// <param name="handling">Determines what to do when duplicate records are found based on sorting. Note: Handled in the index logic.</param>
        /// <param name="mainSort">Sort columns</param>
        public DocSorter(DocReader source, int pageCacheSize, bool createIndexFile, bool disposeCleansIndex,
            DuplicateHandling handling, params IRecordColumnInfo[] mainSort)
            : base(source, pageCacheSize, createIndexFile, disposeCleansIndex, handling, mainSort)
        {
        }
    }

    /// <summary>
    /// Parameterized sorter for a parameterized DocReader.
    /// </summary>
    /// <typeparam name="G"></typeparam>
    public partial class DocSorter<G>:IEnumerable<G>, IDisposable where G:DocRecord, new()
    {
        DocReader<G> _source;
        List<IRecordColumnInfo> sortColumns = new List<IRecordColumnInfo>();
        
        pageCache cache; //Not as useful now, but may still help for iterating
        DocMetaData index;
        DocReader<sortInfo> indexReader;

        #region constructors
        /// <summary>
        /// Constructor. Parameters: Parameterized DocReader, column to sort on.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="cacheSize"></param>
        /// <param name="createIndex">If true, will auto create the index at the end of construction</param>
        /// <param name="disposeCleansIndex">If true, <see cref="Dispose()"/> will also delete the sort index file.</param>
        /// <param name="handling">Determines what to do when duplicate records are discovered by the sort.</param>
        /// <param name="mainSort"></param>
        public DocSorter(DocReader<G> source, int cacheSize, bool createIndex, bool disposeCleansIndex,
            DuplicateHandling handling, params IRecordColumnInfo[] mainSort)
            :this(source, 
                 cacheSize, 
                 (source.MetaData.PageSize * mainSort.Length.MaxCompare(1)) 
                    / source.Columns.Count.MaxCompare(1), 
                 createIndex, 
                 disposeCleansIndex, 
                 handling,
                 mainSort) { }
        /// <summary>
        /// Constructor. Parameters: Parameterized DocReader, column to sort on.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="indexPageSize"></param>
        /// <param name="cacheSize"></param>
        /// <param name="createIndex">If true, will auto create the index at the end of construction</param>
        /// <param name="disposeCleansIndex">If true, <see cref="Dispose()"/> will also delete the sort index file.</param>
        /// <param name="mainSort"></param>
        public DocSorter(DocReader<G> source, int cacheSize, int indexPageSize, bool createIndex, bool disposeCleansIndex, params IRecordColumnInfo[] mainSort)
            :this(source, cacheSize, indexPageSize, createIndex, disposeCleansIndex, DuplicateHandling.Ignore, mainSort) { }
        /// <summary>
        /// Constructor. Parameters: Parameterized DocReader, column to sort on.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="cacheSize"></param>
        /// <param name="indexPageSize"></param>
        /// <param name="createIndex">If true, will auto create the index at the end of construction</param>
        /// <param name="disposeCleansIndex">If true, <see cref="Dispose()"/> will also delete the sort index file.</param>
        /// <param name="handling">Determines what to do when duplicate records are found based on sorting. Note: Handled in the index logic.</param>
        /// <param name="mainSort"></param>
        public DocSorter(DocReader<G> source, int cacheSize, int indexPageSize, bool createIndex, bool disposeCleansIndex, DuplicateHandling handling, params IRecordColumnInfo[] mainSort)
        {
            FileInfo f = new FileInfo(source.FilePath);
            if (!f.Exists)
                throw new ArgumentException("FilePath of source not a valid file.", nameof(source));
            if (mainSort.Exists(r => r == null))
                throw new ArgumentException("Cannot use null as a sort column", nameof(mainSort));
            _source = source;
            cache = new pageCache(cacheSize);            
            
            sortColumns = new List<IRecordColumnInfo>(mainSort);
            INDEX_PATH = f.FullName + "." + INDEX_EXTENSION;
            index = new DocMetaData(INDEX_PATH, INDEX_EXTENSION)
                //.AddDelimitedColumns(nameof(sortInfo.Page), nameof(sortInfo.Line))
                .SetFileAccess(FileAccess.ReadWrite)
                .SetDelimiter(DELIM)
                .SetPageSize(indexPageSize)
                .SetLineEndDelimiter(LINE_END)
                .SetHasHeader(false);
            FileInfo idx = new FileInfo(INDEX_PATH);
            CleanIndexFile = disposeCleansIndex;
            if (idx.Exists)
            {
                if (idx.CreationTime > f.LastWriteTime)
                {
                    indexReader = new DocReader<sortInfo>(index);
                    if (indexReader.RecordCount == _source.RecordCount && indexReader.Columns.Count == mainSort.Length + 2) //sort columns + page/line
                        return; //don't need to sort index.
                    else
                    {
                        indexReader.Dispose();
                        indexReader = null;
                    }
                }
                File.Delete(idx.FullName);
            }
            if(createIndex)
                CreateSortIndex(handling);
        }
        /// <summary>
        /// Check if the sort index has been created.
        /// </summary>
        public bool SortIndexConfigured => index.CheckExists();
        /// <summary>
        /// Constructor. Parameters: Parameterized DocReader, column to sort on.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="mainSort"></param>
        public DocSorter(DocReader<G> source,  params IRecordColumnInfo[] mainSort)
            :this(source, 4, true, true, DuplicateHandling.Ignore, mainSort)
        {         
        }
        /// <summary>
        /// Constructor. Parameters: Parameterized DocReader, column to sort on.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="createIndex">If true, will auto create the index at the end of construction</param>
        /// <param name="mainSort"></param>
        public DocSorter(DocReader<G> source, bool createIndex,  params IRecordColumnInfo[] mainSort) 
            :this(source, 4, createIndex, true, DuplicateHandling.Ignore, mainSort)
        {

        }
        const string INDEX_EXTENSION = "sidxf";
        readonly string INDEX_PATH;
        #endregion
        #region sorted file access
        /// <summary>
        /// Gets the record at position <paramref name="position"/> after mapping from sort. Note: If <see cref="DuplicateHandling"/> is <see cref="DuplicateHandling.Delete"/>, result may be null.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public G this[long position]
        {
            get
            {
                var idx = indexReader[position];
                return _source[idx.Page, idx.Line];
            }
        }
        /// <summary>
        /// Gets the record at position <paramref name="page"/>/<paramref name="pageLine"/> after mapping from sort. Note: If <see cref="DuplicateHandling"/> is <see cref="DuplicateHandling.Delete"/>, result may be null.
        /// </summary> 
        /// <param name="page"></param>
        /// <param name="pageLine"></param>
        /// <returns></returns>
        public G this[int page, int pageLine]
        {
            get
            {
                long pl = _source.CheckLine(page, pageLine);
                var idx = indexReader[pl];
                return _source[idx.Page, idx.Line];
            }
        }
        /// <summary>
        /// Returns the records mapped to this index after sorting.Note: duplicate records may be marked as null and placed somewhat arbitrarily.(When index is created with <see cref="DuplicateHandling.Delete"/>)
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public G[] GetPage(int page)
        {
            if (!index.CheckExists())
                throw new InvalidOperationException("Index not found.");
            var p = _source.GetPageInfo(page);
            var start = _source.CheckLine(page, 0);
            var offset = start;
            long end = start + p.RecordCount;
            G[] gpage = new G[p.RecordCount];
            sortInfoCache infoCache = new sortInfoCache(indexReader, start, end); //ignores records with page < 0 (dupes)
            foreach(var pageNum in infoCache.GetPagesUsed())
            {
                var pageData = cache.Get(pageNum, _source);
                var cacheList = infoCache.PullInfo(pageNum);
                foreach (var cacheEntry in cacheList)
                    gpage[cacheEntry.FileLine - offset] = pageData[cacheEntry.Info.Line];
            }
            return gpage;
        }
        #endregion
        #region enumeration
        /// <summary>
        /// Enumerate records. Note: if deduping, duplicates will be returned as null.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<G> GetEnumerator()
        {
            for (int pn = 0; pn < _source.PageCount; pn++)
            {
                foreach (var record in GetPage(pn))
                    yield return record;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region helpers
        /// <summary>
        /// Adds one or more column sort information to the list of columns to sort.
        /// </summary>
        /// <param name="toSort"></param>
        /// <returns></returns>
        public DocSorter<G> AddSortColumn(params IRecordColumnInfo[] toSort)
        {
            if (toSort.Exists(r => r == null))
                throw new ArgumentException("Cannot use null as a sort column", nameof(toSort));

            sortColumns.AddRange(toSort);
            return this;
        }
        /// <summary>
        /// clears the columns associated with sorting. Does not clear the index if that has been created
        /// </summary>
        /// <returns></returns>
        public DocSorter<G> ResetSortColumns()
        {
            sortColumns.Clear();
            return this;
        }
        const char DELIM = '|';
        const string LINE_END = "\n";
        class sortInfo : DocRecord
        {
            public sortInfo():base()
            {

            }
            public sortInfo(DocRecordColumnCollection owner, IList<string>parsed)
                :base( owner, false, parsed)
            {

            }
            protected internal override void Configure(DocRecordColumnCollection owner, bool? canWrite = null, IList<string> parsedContent = null)
            {
                base.Configure(owner, canWrite, parsedContent);
                Page = Convert.ToInt32(parsedContent[0]);
                Line = Convert.ToInt32(parsedContent[1]);

            }

            public int Page;// => Convert.ToInt32(this["PAGE"]);
            public int Line;// => Convert.ToInt32(this["LINE"]);
            public string GetData(int index)
            {
                return this[index + 2];//PAGE|LINE|DATA0|DATA1|...
            }
        }
        #endregion
        //ToDo: SortMethod - takes target filepath as parameter, and sorts content itself into sub files and merges into end file, instead of only the index columns. For when no processing is being done.
        //DuplicateHandling enum: Ignore, Remove, Exception
       

        /// <summary>
        /// If true, delete the index file during <see cref="Dispose"/>
        /// </summary>
        public bool CleanIndexFile { get; set; } = true;
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cache.Clear();
                    // TODO: dispose managed state (managed objects).
                }
                indexReader?.Dispose(); //because the indexReader has some unmanaged resources, I believe (stream fields)
                if (File.Exists(INDEX_PATH) && CleanIndexFile)
                    File.Delete(INDEX_PATH);
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }
        
         ~DocSorter()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        /// <summary>
        /// IDisposable support
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion

    }
}
