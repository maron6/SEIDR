using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery
{
    class HashJoin
    {
        DocMetaData File;
        public const JoinType myType = JoinType.INNER_EXPLICIT;        
        //Dictionary<ulong, SortedSet<ulong>> Buckets;
        Dictionary<ulong, BigValueFlag> HashMatches;
        DocRecordColumnInfo[] LeftColumns;
        //DelimitedRecordHashTable Right;
        public HashJoin(IEnumerable<DocRecordColumnInfo> leftColumns, 
            IEnumerable<DocRecordColumnInfo> rightColumns, DocMetaData rightFile)
        {
            LeftColumns = leftColumns.ToArray();
            //Left = new DelimitedRecordHashTable(leftColumns);
            //Right = new DelimitedRecordHashTable(rightColumns);
            //Buckets = new Dictionary<ulong, SortedSet<ulong>>();
            HashMatches = new Dictionary<ulong, BigValueFlag>();
            File = rightFile;
            using(DelimitedDocumentReader dr = new DelimitedDocumentReader(rightFile.FilePath)
            { ALIAS = rightFile.Alias })
            {
                var Counter = 0ul;
                foreach(var record in dr)
                {
                    var h = record.GetPartialHash(true, File.EmptyIsNull, false, rightColumns.ToArray());
                    if (h.HasValue)
                    {
                        BigValueFlag bvf; //use big value flag to avoid needing a ulong for every single line that needs to be stored
                        if (!HashMatches.TryGetValue(h.Value, out bvf))
                        {
                            bvf = new BigValueFlag();
                            HashMatches[h.Value] = bvf;
                        }
                        bvf[Counter] = true;
                        /*
                        SortedSet<ulong> work;
                        if (!Buckets.TryGetValue(h.Value, out work))
                        {
                            work = new SortedSet<ulong>();
                            Buckets[h.Value] = work;

                        }
                        work.Add(Counter);*/
                    }
                    Counter++;                                
                }
            }
        }
        /// <summary>
        /// Returns all matches for current record from the left
        /// </summary>
        /// <param name="left"></param>        
        /// <returns></returns>
        public IEnumerable<DelimitedRecord> DoJoin(DelimitedRecord left)
        {
            var h = left.GetPartialHash(true, File.EmptyIsNull, false, LeftColumns);
            
            if (!h.HasValue)
                yield break;
            BigValueFlag bvf;
            if (!HashMatches.TryGetValue(h.Value, out bvf) || bvf.Count == 0)
                yield break;
            
            ulong Max = bvf.MaxFlagged.Value;
            /*
            SortedSet<ulong> indexes;
            if (!Buckets.TryGetValue(h.Value, out indexes) || indexes.Count == 0)
                yield break;       */
            using (DelimitedDocumentReader dr = new DelimitedDocumentReader(File.FilePath)
            { ALIAS = File.Alias })
            {
                ulong Counter = 0;
                foreach(var record in dr)
                {
                    if (bvf[Counter])
                        yield return DelimitedRecord.Merge(left, record);
                    /*
                    if (indexes.Contains(Counter))
                        yield return record;                    
                    */
                    if (Counter == Max) //If found the last match, exit now
                        yield break;
                    Counter++;
                }
            }
        }
    }
}
