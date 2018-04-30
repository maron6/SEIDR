using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SEIDR.Doc
{
    public partial class DocSorter<G>
    {
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
                if (groups == 1)
                {
                    w.AddRecord($"{page}{DELIM}{0}");
                    return;
                }
                List<IList<int>> orders = new List<IList<int>>();
                for (int i = 0; i < groups; i++)
                {
                    orders.Add(new[] { i });
                }
                while (groups > 1)
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
                foreach (var o in orders[0]) //group count == 1
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
                //ToDo:...do the thing, closer to merge sort
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
                    foreach (var si in SortPositions(fold, r))
                        w.AddRecord(si);
                }
                useW1 = !useW1;
                try
                {
                    File.Delete(pidx.FilePath);
                }
                catch (Exception ex)
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


        IEnumerable<sortInfo> SortPositions(IEnumerable<sortInfo> A, IEnumerable<sortInfo> B)
        {           
            var aIdx = A.GetEnumerator();
            if (!aIdx.MoveNext())
            {
                foreach (var s in B)
                    yield return s;
                yield break;
            }
            var bIdx = B.GetEnumerator();
            if (!bIdx.MoveNext())
            {
                foreach (var s in A)
                    yield return s;
                yield break;
            }

            sortInfo sa = aIdx.Current;
            sortInfo sb = bIdx.Current;
            IRecord a = cache.Get(sa.Page, _source)[sa.Line];
            IRecord b = cache.Get(sb.Page, _source)[sb.Line];
            while (true)
            {
                var comp = sort(a, b);
                if (comp == 0)
                {
                    yield return sa;
                    yield return sb;
                    //ret.Add(sa);
                    //ret.Add(sb);
                    if (!aIdx.MoveNext())
                    {
                        while (bIdx.MoveNext())
                        {
                            yield return bIdx.Current;
                        }
                        yield break;
                    }
                    if (!bIdx.MoveNext())
                    {
                        do
                        {
                            yield return aIdx.Current;//already did move, and was successful.
                        }
                        while (aIdx.MoveNext());                         
                        yield break;
                    }
                    sa = aIdx.Current;
                    sb = bIdx.Current;
                    a = cache.Get(sa.Page, _source)[sa.Line];
                    b = cache.Get(sb.Page, _source)[sb.Line];
                }
                else if (comp < 0)
                {
                    // a<b by compare...
                    //ret.Add(sa);
                    yield return sa;
                    if (!aIdx.MoveNext())
                    {
                        yield return sb;
                        while (bIdx.MoveNext())
                            yield return bIdx.Current;
                        yield break;
                    }
                    sa = aIdx.Current;
                    a = cache.Get(sa.Page, _source)[sa.Line];
                }
                else
                {
                    yield return sb;
                    if (!bIdx.MoveNext())
                    {
                        yield return sa;
                        while (aIdx.MoveNext())
                        {
                            yield return aIdx.Current;
                        }
                        yield break;
                    }
                    sb = bIdx.Current;
                    b = cache.Get(sb.Page, _source)[sb.Line];
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
                if (comp == 0)
                {
                    ret.Add(A[aidx++]);
                    ret.Add(B[bidx++]);
                    if (aidx == A.Count)
                    {
                        for (; bidx < B.Count; bidx++)
                        {
                            ret.Add(B[bidx]);
                        }
                        return ret; //finished
                    }
                    if (bidx == B.Count)
                    {
                        for (; aidx < A.Count; aidx++)
                        {
                            ret.Add(A[aidx]);
                        }
                        return ret; //finished
                    }
                    a = page[A[aidx]];
                    b = page[B[bidx]];
                }
                else if (comp < 0)
                {
                    // a<b by compare...
                    ret.Add(A[aidx++]);
                    if (aidx == A.Count)
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
                    if (bidx == B.Count)
                    {
                        for (; aidx < A.Count; aidx++)
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
    }
}
