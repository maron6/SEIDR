using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery
{
    internal class DelimitedJoin
        //:IEnumerable<JoinedDelimitedRecords>
        : IEnumerable<DelimitedRecord>
    {
        JoinType DelimitedJoinType;
        ConditionTree JoinConditions;
        DocMetaData myContent;
        //JoinedDelimitedRecords join = null;
        DelimitedRecord join = null;
        HashJoin hj = null;
        ulong? jh = null;
        readonly bool doHashJoin = false;
        bool LookForUnmatched = true;
        public DelimitedJoin(DocMetaData docInfo, ConditionTree conditions, JoinType joinType)
        {
            myContent = docInfo;            
            if (docInfo == null)
                throw new ArgumentNullException(nameof(docInfo));

            JoinConditions = conditions;
            DelimitedJoinType = joinType;

            if ((JoinConditions == null || JoinConditions.ConditionCount == 0) && DelimitedJoinType != JoinType.NOT_A_JOIN)
                throw new InvalidOperationException("Join conditions are missing!");

            if (JoinConditions.LeftHashColumns != null && JoinConditions.LeftHashColumns.Length > 0)
                doHashJoin = true;
        }
        IList<DocRecordColumnInfo> LeftSideColumns = null;
        //public void SetJoinRecord(JoinedDelimitedRecords toJoin)
        //{
        //    join = toJoin;
        //}
        public void SetJoinRecord (DelimitedRecord toJoin)
        {
            join = toJoin;
            if (LeftSideColumns == null)
                LeftSideColumns = toJoin.HeaderList;
            if (doHashJoin)
                jh = toJoin.GetPartialHash(true, myContent.EmptyIsNull, false, JoinConditions.LeftHashColumns);
            else jh = null;
        }
        bool CheckHash(DelimitedRecord check)
        {
            //Do HashJoin will only  be true if the root of the condition tree is able to do a hash join after optimization.
            if (!doHashJoin || jh == null)
                return false;
            var rh = check.GetPartialHash(true, myContent.EmptyIsNull, false, JoinConditions.RightHashColumns);
            return jh == rh;
        }
        /// <summary>
        /// Use for tracking positions in file that need to be returned in a right or outer join
        /// </summary>
        BigValueFlag unmatched = new BigValueFlag();
        //public IEnumerator<JoinedDelimitedRecords> GetEnumerator()
        public IEnumerator<DelimitedRecord> GetEnumerator()
        {
            if (join == null)
                throw new InvalidOperationException("Join record has not been set!");
            if (doHashJoin)
            {
                if (hj == null)
                    hj = new HashJoin(JoinConditions.LeftHashColumns, JoinConditions.RightHashColumns, myContent);
                foreach(var record in hj.DoJoin(join))
                {
                    yield return record;
                }
                yield break;
            }
            using (var reader = new DelimitedDocumentReader(myContent.FilePath))            
            {
                reader.ALIAS = myContent.Alias;
                bool matched = false;
                ulong counter = 0;
                //ToDo:store hash if join conditions allow hash
                foreach (var r in reader)
                {
                    //join.Set(r);
                    //var work = DelimitedRecord.Merge(join, r);
                    if (DelimitedJoinType == JoinType.NOT_A_JOIN //Always yield return
                                                                 //|| JoinConditions.CheckConditions(join.Content)
                        || CheckHash(r) //Returns false right away if not doing a hash join (needs to be optimized up to root. Otherwise, leave up to condition nodes to get hash)
                        || JoinConditions.CheckConditions(join, r, myContent.EmptyIsNull)
                        //|| JoinConditions.CheckJoinedConditions(work)
                        )
                    {
                        matched = true; //if the left side is matched.
                        unmatched[counter] = false; //right side has matched
                        yield return DelimitedRecord.Merge(join, r);
                        //yield return join;
                        //yield return work;
                    }
                    else if (LookForUnmatched)
                        unmatched[counter] = true; //Only flag on the first time around

                    counter++;
                }
                if (!matched && DelimitedJoinType.In(JoinType.LEFT, JoinType.FULL))
                {
                    //yield return join.Replace(reader.EMPTY);
                    yield return DelimitedRecord.Merge(join, reader.EMPTY);
                }
            }            
            //Have finished the first join on this alias. Don't need to look for unmatched anymore, just unflag any
            // that do match
            LookForUnmatched = false; 
            join = null;
            yield break;
        }

        /// <summary>
        /// For right/outer joins, returns the records that had no match, with the columns to the left prepended as empty.
        /// <para>Ends early once there are no more unmatched records</para>
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DelimitedRecord> GetUnmatched()
        {
            if (LeftSideColumns == null)
            {
                throw new InvalidOperationException("No Joining has been performed!");
            }
            if (DelimitedJoinType.NotIn(JoinType.RIGHT, JoinType.FULL) || unmatched.Count == 0)
                yield break;            
            using (var reader = new DelimitedDocumentReader(myContent.FilePath))
            {
                reader.ALIAS = myContent.Alias;                
                ulong counter = 0;
                ulong stop = unmatched.MaxFlagged ?? counter;
                foreach(var r in reader)
                {
                    if (unmatched[counter++]) //postfix
                    {
                        yield return DelimitedRecord.MergeEmpty(LeftSideColumns, r);
                    }
                    if (counter > stop)
                        yield break;                    
                }
            }            
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
