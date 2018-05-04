using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    public partial class DocSorter<G>
    {
        class sortInfoCache
        {
            public List<CacheEntry> list { get; private set; }
            List<int> pages = new List<int>();            
            public IEnumerable<int> GetPagesUsed()
            {
                foreach (var pageNum in pages)
                    yield return pageNum;
                yield break;
                //return (from e in list
                //        select e.Info.Page).Distinct().OrderBy(p => p);
            }
            public sortInfoCache(DocReader<sortInfo> index, long startLine, long endLine)
            {
                list = new List<CacheEntry>();
                for(; startLine < endLine; startLine++)
                {
                    var info = index[startLine];
                    if (info.Page < 0) //dupe
                        continue;
                    if (!pages.Contains(info.Page))
                        pages.Add(info.Page);                    
                    list.Add(new CacheEntry(startLine, info));
                }
                pages.Sort();
            }

            public sortInfoCache(IList<sortInfo> index, long startLine)
            {
                
                list = new List<CacheEntry>();
                for(int i = 0; i < index.Count; i++, startLine++)
                {                    
                    var info = index[i];
                    if (info.Page < 0)
                        continue; //dupe
                    if (!pages.Contains(info.Page))
                        pages.Add(info.Page);
                    list.Add(new CacheEntry(startLine, info));
                    
                }
                pages.Sort();
            }
            public List<CacheEntry> PullInfo(int page)
            {
                List<CacheEntry> ret = new List<CacheEntry>();
                int idx = 0;
                while(idx < list.Count)
                {
                    var e = list[idx];
                    if (e.Info.Page == page)
                    {
                        ret.Add(e);
                        list.RemoveAt(idx);
                        continue;
                    }
                    idx++;
                }
                return ret;
            }
            public class CacheEntry
            {
                public readonly long FileLine;
                public readonly sortInfo Info;
                public CacheEntry(long LineNumber, sortInfo value)
                {
                    FileLine = LineNumber;
                    Info = value;
                }
            }
        }
        class pageCache
        {
            public void Clear() => entryList.Clear();
            LinkedList<cacheEntry> entryList;
            readonly int CacheSize = 2;
            public pageCache(int cacheSize)
            {
                CacheSize = cacheSize;
                entryList = new LinkedList<cacheEntry>();
            }
            public bool CheckCache(int page)
            {
                return entryList.Exists(chk => chk.PageNumber == page);
            }
            public cacheEntry Get(int page, DocReader<G> source)
            {
                cacheEntry e;
                if (entryList.Count == 0)
                {
                    e = new cacheEntry(page, source.GetPage(page));
                    entryList.AddFirst(e);
                    return e;
                }
                else if (entryList.First.Value.PageNumber == page)
                    return entryList.First.Value;
                e = entryList.FirstOrDefault(chk => chk.PageNumber == page);
                if (e != null)
                {
                    if (e.PageNumber != entryList.First.Value.PageNumber)
                    {
                        entryList.Remove(e);
                        entryList.AddFirst(e);
                    }
                }
                else
                {
                    //Not found.
                    e = new cacheEntry(page, source.GetPage(page));
                    if (entryList.Count >= CacheSize)
                        entryList.RemoveLast();

                    entryList.AddFirst(e);
                }

                return e;
            }
            public class cacheEntry
            {
                List<G> Page;
                public readonly int PageNumber;
                public cacheEntry(int pageNumber, List<G> page)
                {
                    Page = page;
                    PageNumber = pageNumber;
                }
                public G this[int line]
                {
                    get
                    {
                        return Page[line];
                    }
                }
            }
        }
    }
}
