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
    /// Basic helper for <see cref="DocSorter"/>
    /// </summary>
    public sealed class SortColumn:IRecordColumnInfo
    {
        readonly int _Position;
        /// <summary>
        /// Sort order for <see cref="DocSorter"/>
        /// </summary>
        public bool SortASC { get; set; } = true;
        /// <summary>
        /// Column position for getting a record from an <see cref="IRecord"/>
        /// </summary>
        public int Position => _Position;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="position"></param>
        /// <param name="ascOrder"></param>
        public SortColumn(int position, bool ascOrder = true)
        {
            _Position = position;
            SortASC = ascOrder;
        }
    }
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
        /// <param name="createIndexFile">If true, will auto create the index at the end of construction</param>
        /// <param name="disposeCleansIndex">If true, <see cref="DocSorter{DocRecord}.Dispose()"/> will also delete the sort index file.</param>
        /// <param name="mainSort">Sort columns</param>
        public DocSorter(DocReader source, bool createIndexFile, bool disposeCleansIndex, params IRecordColumnInfo[] mainSort)
            : base(source, createIndexFile, disposeCleansIndex, mainSort)
        {
        }
    }

    /// <summary>
    /// Parameterized sorter for a parameterized DocReader.
    /// </summary>
    /// <typeparam name="G"></typeparam>
    public class DocSorter<G>:IEnumerable<G>, IDisposable where G:DocRecord, new()
    {
        DocReader<G> _source;
        List<IRecordColumnInfo> sortColumns = new List<IRecordColumnInfo>();
        /// <summary>
        /// Constructor. Parameters: Parameterized DocReader, column to sort on.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="createIndex">If true, will auto create the index at the end of construction</param>
        /// <param name="disposeCleansIndex">If true, <see cref="Dispose()"/> will also delete the sort index file.</param>
        /// <param name="mainSort"></param>
        public DocSorter(DocReader<G> source, bool createIndex, bool disposeCleansIndex, params IRecordColumnInfo[] mainSort)
        {
            FileInfo f = new FileInfo(source.FilePath);
            if (!f.Exists)
                throw new ArgumentException("FilePath of source not a valid file.", nameof(source));
            if (mainSort.Exists(r => r == null))
                throw new ArgumentException("Cannot use null as a sort column", nameof(mainSort));
            _source = source;

            sortColumns = new List<IRecordColumnInfo>(mainSort);
            INDEX_PATH = f.FullName + "." + INDEX_EXTENSION;
            index = new DocMetaData(INDEX_PATH, INDEX_EXTENSION)
                .AddDelimitedColumns(nameof(sortInfo.Page), nameof(sortInfo.Line))
                .SetFileAccess(FileAccess.ReadWrite)
                .SetDelimiter(DELIM)
                .SetLineEndDelimiter(LINE_END)
                .SetHasHeader(false);
            FileInfo idx = new FileInfo(INDEX_PATH);
            if (idx.Exists)
            {
                if (idx.CreationTime > f.LastWriteTime)
                {
                    indexReader = new DocReader<sortInfo>(index);
                    return; //don't need to sort index.
                }
                File.Delete(idx.FullName);
            }
            if(createIndex)
                CreateSortIndex();
            CleanIndexFile = disposeCleansIndex;
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
        public DocSorter(DocReader<G> source, params IRecordColumnInfo[] mainSort)
            :this(source, true, true, mainSort)
        {         
        }
        /// <summary>
        /// Constructor. Parameters: Parameterized DocReader, column to sort on.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="createIndex">If true, will auto create the index at the end of construction</param>
        /// <param name="mainSort"></param>
        public DocSorter(DocReader<G> source, bool createIndex, params IRecordColumnInfo[] mainSort) 
            :this(source, createIndex, true, mainSort)
        {

        }
        const string INDEX_EXTENSION = "sidxf";
        readonly string INDEX_PATH;
        DocMetaData index;
        DocReader<sortInfo> indexReader;
        #region sorted file access
        public G this[long position]
        {
            get
            {
                var idx = indexReader[position];
                return _source[idx.Page, idx.Line];
            }
        }
        public G this[int page, int pageLine]
        {
            get
            {
                long pl = _source.CheckLine(page, pageLine);
                var idx = indexReader[pl];
                return _source[idx.Page, idx.Line];
            }
        }
        public G[] GetPage(int page)
        {
            var p = _source.GetPageInfo(page);
            var start = _source.CheckLine(page, 0);
            var offset = start;
            long end = start + p.RecordCount;
            G[] gpage = new G[p.RecordCount];
            int counter = 0;
            for (int i = 0; i < _source.PageCount; i++)
            {
                for (long j = start; j < end; j++)
                {
                    var s = indexReader[j];
                    if (s.Page != i)
                        continue;
                    gpage[j - offset] = _source[s.Page, s.Line];
                    if (j == start)
                        start++;
                    if (j == end - 1)
                        end--; // can remove edges from looping
                    counter++;
                }
                if (counter == p.RecordCount) //after finishing all records
                    break;
            }
            return gpage;
        }
        #endregion
        #region enumeration
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
        #region index creation, management
        /// <summary>
        /// Deletes the sort index associated with the DocReader's file.
        /// </summary>
        public void DeleteSortIndex()
        {
            indexReader.Dispose();
            if (File.Exists(INDEX_PATH))
                File.Delete(INDEX_PATH);
        }
        /// <summary>
        /// Creates the sort index for pulling data from the initially passed <see cref="DocReader{ReadType}"/>
        /// </summary>
        public void CreateSortIndex()
        {
            if (sortColumns.Count == 0)
                throw new InvalidOperationException("No Sort columns specified");
            sort = new Comparison<IRecord>((a, b) =>
            {
                foreach (var col in sortColumns)
                {                    
                    string l = a[col.Position];
                    string r = b[col.Position];

                    if (l == r)
                        continue;
                    if (l == null)
                        return col.SortASC ? -1 : 1;
                    if (r == null)
                        return col.SortASC ? 1 : -1;

                    if (col.SortASC)
                        return l.CompareTo(r);
                    return r.CompareTo(l);
                }
                return 0;
            });
            for (int i = 0; i < _source.PageCount; i++)
            {
                sortPage(i);
            }
            SortIndexFiles();
            indexReader = new DocReader<sortInfo>(index);
        }
        void sortPage(int page)
        {
            DocMetaData pidx = new DocMetaData(INDEX_PATH + page, INDEX_EXTENSION + page)
                .AddDelimitedColumns(nameof(sortInfo.Page), nameof(sortInfo.Line))
                .SetFileAccess(FileAccess.Write)
                .SetPageSize(index.PageSize)
                .SetDelimiter(DELIM)
                .SetLineEndDelimiter(LINE_END)
                .SetHasHeader(false);
            using (var w = new DocWriter(pidx))
            {                
                var p = _source.GetPage(page);
                int groups = p.Count;
                if (groups == 0)
                    return;
                if(groups == 1)
                {
                    w.AddRecord($"{page}{DELIM}{0}");
                    return;
                }
                List<IList<int>> orders = new List<IList<int>>();
                for(int i = 0; i < groups; i++)
                {
                    orders.Add(new[] { i });
                }
                while(groups > 1)
                {
                    List<IList<int>> tOrders = new List<IList<int>>();
                    for (int i = 0; i < groups; i += 2)
                    {
                        IList<int> work = SortPositions(orders[i], orders[i + 1], p);
                        if (i == groups - 3)
                        {
                            //odd, we have three records remaining (i, i+ 1, i + 2), need all three sorted together to not lose a record.
                            work = SortPositions(work, orders[i + 2], p);
                            i += 3;
                        }
                        tOrders.Add(work);
                    }
                    orders = tOrders;
                    groups = orders.Count;
                }
                foreach(var o in orders[0]) //group count == 1
                {
                    w.AddRecord($"{page}{DELIM}{o}");
                }                
            }
        }    
        void SortIndexFiles()
        {
            DocMetaData w1 = new DocMetaData(INDEX_PATH + 0, nameof(w1))
                    .AddDelimitedColumns(nameof(sortInfo.Page), nameof(sortInfo.Line))
                    .SetFileAccess(FileAccess.ReadWrite)
                    .SetPageSize(index.PageSize)
                    .SetDelimiter(DELIM)
                    .SetLineEndDelimiter(LINE_END)
                    .SetHasHeader(false);

            DocMetaData w2 = new DocMetaData(INDEX_PATH + nameof(w2), nameof(w2))
                    .AddDelimitedColumns(nameof(sortInfo.Page), nameof(sortInfo.Line))
                    .SetFileAccess(FileAccess.ReadWrite)
                    .SetPageSize(index.PageSize)
                    .SetDelimiter(DELIM)
                    .SetLineEndDelimiter(LINE_END)
                    .SetHasHeader(false);
            bool useW1 = true;
            for (int page = 1; page < _source.PageCount; page++)
            {
                DocMetaData pidx = new DocMetaData(INDEX_PATH + page, INDEX_EXTENSION + page)
                    .AddDelimitedColumns(nameof(sortInfo.Page), nameof(sortInfo.Line))
                    .SetFileAccess(FileAccess.Read)
                    .SetPageSize(index.PageSize)
                    .SetDelimiter(DELIM)
                    .SetLineEndDelimiter(LINE_END)
                    .SetHasHeader(false);
                using (var fold = new DocReader<sortInfo>(pidx)) //sorted index information to be folded into the new file
                using (var r = new DocReader<sortInfo>(useW1 ? w1 : w2))                
                using (var w = new DocWriter(useW1 ? w2 : w1))
                {
                    foreach (var si in SortPositions(fold.ToList(), r.ToList()))
                        w.AddRecord(si);
                }
                useW1 = !useW1;
                try
                {
                    File.Delete(pidx.FilePath);
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.GetType() + ":  " + ex.Message);
                        
                }
            }
            File.Move(useW1 ? w1.FilePath : w2.FilePath, INDEX_PATH);
            if (useW1)
                File.Delete(w2.FilePath);
            else
                File.Delete(w1.FilePath);
        }
        
        
        IEnumerable<sortInfo> SortPositions(IList<sortInfo> A, IList<sortInfo> B)
        {
            if (B.Count == 0)
            {
                foreach (var s in A)
                    yield return s;
                yield break;
            }
            if (A.Count == 0)
            {
                foreach (var s in B)
                    yield return s;
                yield break;
            }
            //List<sortInfo> ret = new List<sortInfo>();
            int aidx = 0, bidx = 0;
            sortInfo sa = A[aidx];
            sortInfo sb = B[bidx];
            IRecord a = _source[sa.Page, sa.Line];
            IRecord b = _source[sb.Page, sb.Line];
            while (true)
            {
                var comp = sort(a, b);
                if (comp == 0)
                {
                    yield return sa;
                    yield return sb;
                    //ret.Add(sa);
                    //ret.Add(sb);
                    aidx++;
                    bidx++;
                    
                    if (aidx == A.Count)
                    {
                        for (; bidx < B.Count; bidx++)
                        {
                            //ret.Add(B[bidx]);
                            yield return B[bidx];
                        }
                        yield break; //finished
                    }
                    if (bidx == B.Count)
                    {
                        for (; aidx < A.Count; aidx++)
                        {
                            //ret.Add(A[aidx]);
                            yield return A[aidx];
                        }
                        /*return ret; *///finished
                        yield break;
                    }
                    sa = A[aidx];
                    sb = B[bidx];
                    a = _source[sa.Page, sa.Line];
                    b = _source[sb.Page, sb.Line];
                }
                else if (comp < 0)
                {
                    // a<b by compare...
                    //ret.Add(sa);
                    yield return sa;
                    aidx++;
                    if (aidx == A.Count)
                    {
                        for (; bidx < B.Count; bidx++)
                        {
                            //ret.Add(B[bidx]);
                            yield return B[bidx];
                        }
                        yield break;
                        //return ret; //finished
                    }
                    sa = A[aidx];
                    a = _source[sa.Page, sa.Line];
                }
                else
                {                    
                    yield return sb;
                    bidx++;
                    if (bidx == B.Count)
                    {
                        for (; aidx < A.Count; aidx++)
                        {
                            //ret.Add(A[aidx]);
                            yield return A[aidx];
                        }
                        //return ret;
                        yield break;
                    }
                    sb = B[bidx];
                    b = _source[sb.Page, sb.Line];
                }

            }
        }
        IList<int> SortPositions(IList<int> A, IList<int> B, List<G> page)
        {
            if (B.Count == 0)
                return A;
            if (A.Count == 0)
                return B;
            List<int> ret = new List<int>();
            int aidx = 0, bidx = 0;
            IRecord a = page[A[aidx]];
            IRecord b = page[B[bidx]];
            while (true)
            {
                var comp = sort(a, b);
                if(comp == 0)
                {
                    ret.Add(A[aidx++]);
                    ret.Add(B[bidx++]);                    
                    if(aidx == A.Count)
                    {
                        for(; bidx < B.Count; bidx++)
                        {
                            ret.Add(B[bidx]);
                        }
                        return ret; //finished
                    }
                    if(bidx == B.Count)
                    {
                        for(; aidx < A.Count; aidx++)
                        {
                            ret.Add(A[aidx]);
                        }
                        return ret; //finished
                    }
                    a = page[A[aidx]];
                    b = page[B[bidx]];
                }
                else if(comp < 0)
                {
                    // a<b by compare...
                    ret.Add(A[aidx++]);                    
                    if(aidx == A.Count)
                    {
                        for (; bidx < B.Count; bidx++)
                        {
                            ret.Add(B[bidx]);
                        }
                        return ret; //finished
                    }
                    a = page[A[aidx]];
                }
                else
                {
                    ret.Add(B[bidx++]);
                    if(bidx == B.Count)
                    {
                        for(; aidx < A.Count; aidx++)
                        {
                            ret.Add(A[aidx]);
                        }
                        return ret;
                    }
                    b = page[B[bidx]];
                }
                
            }
        }
        Comparison<IRecord> sort;
        IList<IRecord> GetIndexPage(int page)
        {
            DocMetaData dm = new DocMetaData(INDEX_PATH + page, INDEX_EXTENSION + page)
                .AddDelimitedColumns(nameof(sortInfo.Line))
                .SetFileAccess(FileAccess.Read)
                .SetHasHeader(false);
            using (var r = new DocReader(dm))
            {
                return (IList<IRecord>)r.ToList();
            }
        }
        #endregion
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
        }

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
                    // TODO: dispose managed state (managed objects).
                }
                indexReader.Dispose(); //because the indexReader has some unmanaged resources, I believe (stream fields)
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
