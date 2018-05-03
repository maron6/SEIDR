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
        /// Handling for duplicates when creating the index file.
        /// </summary>
        public enum DuplicateHandling
        {
            /// <summary>
            /// Do nothing when a duplicate appears
            /// </summary>
            Ignore,
            /// <summary>
            /// Remove duplicates
            /// </summary>
            Delete,
            /// <summary>
            /// Throw an exception if a duplicate is found.
            /// </summary>
            Exception
        }
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
            DuplicateHandling handling = DuplicateHandling.Ignore; //ToDo: Parameter..
            if (sortColumns.Count == 0)
                throw new InvalidOperationException("No Sort columns specified");
            index.Columns.ClearColumns();
            index.Columns.AddColumn("PAGE");
            index.Columns.AddColumn("LINE");
            sortColumns.ForEach(c => index.Columns.AddColumn("DATA" + c.Position));
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
            sortComp = new Comparison<sortInfo>((a, b) =>
            {

                for(int i = 0; i < sortColumns.Count; i++)
                {
                    var col = sortColumns[i];
                    
                    string l = a.GetData(i);//col.Position);
                    string r = b.GetData(i);//col.Position);

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
                sortPage(i, handling);
            }
            MergeIndexFiles();
            //SortIndexFiles();
            indexReader = new DocReader<sortInfo>(index);
        }
        void sortPage(int page, DuplicateHandling handling)
        {
            DocMetaData pidx = new DocMetaData(INDEX_PATH + page, INDEX_EXTENSION + page);
            addSortCols(pidx);            

            using (var w = new DocWriter(pidx))
            {
                var p = _source.GetPage(page);
                int groups = p.Count;
                if (groups == 0)
                    return;
                if (groups == 1)
                {
                    StringBuilder data = new StringBuilder($"{page}{DELIM}0");
                    sortColumns.ForEach(c => { data.AppendFormat("{0}{1}", DELIM, p[0][c.Position]); });
                    w.AddRecord(data.ToString());                    
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
                        IList<int> work = SortPositions(orders[i], orders[i + 1], p, handling);
                        if (i == groups - 3)
                        {
                            //odd, we have three records remaining (i, i+ 1, i + 2), need all three sorted together to not lose a record.
                            work = SortPositions(work, orders[i + 2], p, handling);
                            i += 3;
                        }
                        tOrders.Add(work);
                    }
                    orders = tOrders;
                    groups = orders.Count;
                }
                foreach (var o in orders[0]) //group count == 1
                {

                    StringBuilder data = new StringBuilder($"{page}{DELIM}{o}");
                    sortColumns.ForEach(c => { data.AppendFormat("{0}{1}", DELIM, p[o][c.Position]); });
                    w.AddRecord(data.ToString());
                }
            }
        }
        void MergeIndexFiles(DuplicateHandling handling = DuplicateHandling.Ignore)
        {
            string W2 = INDEX_PATH + nameof(W2);            
            bool loopL = false;
            string sfx = loopL ? "L" : "";
            int count = _source.PageCount;
            while (count > 1) //if count starts at 1, there's just one file at 0.
            {
                
                for(int page = 0; page < count-1; page += 2)
                {
                    string dest = INDEX_PATH + (page / 2) + (loopL ? "" : "L"); //for the next loop.
                    DocMetaData even = new DocMetaData(INDEX_PATH + page + sfx, INDEX_EXTENSION + page);
                       //.AddDelimitedColumns(nameof(sortInfo.Page), nameof(sortInfo.Line))
                       //.SetFileAccess(FileAccess.Read)
                       //.SetPageSize(index.PageSize)                       
                       //.SetDelimiter(DELIM)
                       //.SetLineEndDelimiter(LINE_END)
                       //.SetHasHeader(false);
                    addSortCols(even);

                    DocMetaData odd = new DocMetaData(INDEX_PATH + (page + 1) + sfx, INDEX_EXTENSION + page);
                    addSortCols(odd);

                    DocMetaData w1 = new DocMetaData(dest, nameof(w1));
                    addSortCols(w1);

                    if (page == count - 3)
                    {
                        DocMetaData w2 = new DocMetaData(W2, nameof(w2));
                        addSortCols(w2);
                        //merge to w2, then to dest.

                        using (var fold = new DocReader<sortInfo>(even)) //sorted index information to be folded into the new file
                        using (var r = new DocReader<sortInfo>(odd))
                        using (var w = new DocWriter(w2))
                        {
                            foreach (var si in SortPositions(fold, r, handling))
                                w.AddRecord(si);
                        }
                        try
                        {
                            File.Delete(even.FilePath);
                            File.Delete(odd.FilePath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex.GetType() + ":  " + ex.Message);
                        }
                        odd = new DocMetaData(INDEX_PATH + (page + 2) + sfx, INDEX_EXTENSION + page);
                        addSortCols(odd);

                        using (var fold = new DocReader<sortInfo>(w2)) //sorted index information to be folded into the new file
                        using (var r = new DocReader<sortInfo>(odd))
                        using (var w = new DocWriter(w1))
                        {
                            foreach (var si in SortPositions(fold, r, handling))
                                w.AddRecord(si);
                        }
                        try
                        {
                            File.Delete(odd.FilePath);                            
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex.GetType() + ":  " + ex.Message);
                        }


                    }
                    else
                    {

                        using (var fold = new DocReader<sortInfo>(even)) //sorted index information to be folded into the new file
                        using (var r = new DocReader<sortInfo>(odd))
                        using (var w = new DocWriter(w1))
                        {
                            foreach (var si in SortPositions(fold, r, handling))
                                w.AddRecord(si);
                        }
                        try
                        {
                            File.Delete(even.FilePath);
                            File.Delete(odd.FilePath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex.GetType() + ":  " + ex.Message);
                        }

                    }
                }
                loopL = !loopL;
                sfx = loopL ? "L" : ""; //match destination just used.
                count /= 2;
            }
            //Move 0 -> destination. Delete w2 if it exists.
            if (File.Exists(W2))
                File.Delete(W2);
            File.Move(INDEX_PATH + 0 + sfx, INDEX_PATH);
        }
        void SortIndexFiles(DuplicateHandling handling = DuplicateHandling.Ignore)
        {
            DocMetaData w1 = new DocMetaData(INDEX_PATH + 0, nameof(w1));
            addSortCols(w1);
            DocMetaData w2 = new DocMetaData(INDEX_PATH + nameof(w2), nameof(w2));
            addSortCols(w2);
            bool useW1 = true;      
            for (int page = 1; page < _source.PageCount; page++) //For 0 -> count, set w1 to w1 as original instead. Merge i, i+1 into a work file... then loop through work files...
            {
                //ToDo:...do the thing, closer to merge sort
                DocMetaData pidx = new DocMetaData(INDEX_PATH + page, INDEX_EXTENSION + page);
                addSortCols(pidx);
                using (var fold = new DocReader<sortInfo>(pidx)) //sorted index information to be folded into the new file
                using (var r = new DocReader<sortInfo>(useW1 ? w1 : w2))
                using (var w = new DocWriter(useW1 ? w2 : w1))
                {
                    foreach (var si in SortPositions(fold, r, handling))
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


        IEnumerable<sortInfo> SortPositions(IEnumerable<sortInfo> A, IEnumerable<sortInfo> B, DuplicateHandling handling)
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
            //IRecord a = cache.Get(sa.Page, _source)[sa.Line];
            //IRecord b = cache.Get(sb.Page, _source)[sb.Line];
            while (true)
            {
                while(sa.Page < 0)
                {
                    yield return sa; //duplicates that are being ignored
                    if (aIdx.MoveNext())
                    {
                        sa = aIdx.Current;
                    }
                    else
                    {
                        yield return sb;
                        while (bIdx.MoveNext())
                        {
                            yield return bIdx.Current;
                        }
                        yield break;
                    }
                }
                while(sb.Page < 0)
                {
                    yield return sb;
                    if (bIdx.MoveNext())
                    {
                        sb = bIdx.Current;
                    }
                    else
                    {
                        yield return sa;
                        while (aIdx.MoveNext())
                        {
                            yield return aIdx.Current;
                        }
                        yield break;
                    }
                }
                var comp = sortComp(sa, sb);
                if (comp == 0)
                {
                    if (handling == DuplicateHandling.Exception)
                        throw new Exception($"Duplicate records via sort: P{sa.Page}#{sa.Line} and {sb.Page}#{sb.Line}.");                    
                    yield return sa;

                    if (handling == DuplicateHandling.Ignore)
                        yield return sb;
                    else
                        yield return new sortInfo(sb.Columns, new List<string> { "-1", "-1" });

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
                    //a = cache.Get(sa.Page, _source)[sa.Line];
                    //b = cache.Get(sb.Page, _source)[sb.Line];
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
                    //a = cache.Get(sa.Page, _source)[sa.Line];
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
                    //b = cache.Get(sb.Page, _source)[sb.Line];
                }

            }
        }
        IList<int> SortPage(IList<int> A, IList<int> B, List<G> page)
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
        IList<int> SortPositions(IList<int> A, IList<int> B, List<G> page, DuplicateHandling handling)
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
                    if (handling == DuplicateHandling.Exception)
                        throw new Exception("Duplicate records found within page..");
                    ret.Add(A[aidx++]);
                    if (handling == DuplicateHandling.Ignore)
                        ret.Add(B[bidx++]);
                    else
                        ret.Add(-1);
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
        Comparison<sortInfo> sortComp;


        void addSortCols(DocMetaData addto)
        {
            addto
                .AddDelimitedColumns(nameof(sortInfo.Page), nameof(sortInfo.Line))
                .SetFileAccess(FileAccess.Read)
                .SetPageSize(index.PageSize)
                .SetDelimiter(DELIM)
                .SetLineEndDelimiter(LINE_END)
                .SetHasHeader(false);
            foreach (var col in sortColumns)
            {
                addto.AddColumn("DATA" + col.Position);
            }
            //addto.CanWrite = true;
        }
    }
}
