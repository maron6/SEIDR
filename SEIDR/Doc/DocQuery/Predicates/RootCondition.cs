using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery.Predicates
{
    public class RootCondition : UnaryCondition
    {        
        public RootCondition() : base(null) { }
        public RootCondition(iCondition child) : base(child) { }
        public MergedJoinCondition HashCondition
        {
            get
            {
                var c = Child as MergedJoinCondition;
                if (c.HashCondition)
                    return c;

                //Hash needs the condition to be simple(no basic transform - just a parse, no function call like adding a value)
                var c2 = Child as JoinCondition;
                if (c2 != null && c2.Simple && c2.Comparison.In(ConditionType.EQUAL, ConditionType.NOT_EQUAL))
                    return new MergedJoinCondition(c2.Comparison, c2.leftColumn, c2.rightColumn);

                return null;
            }
        }
        /// <summary>
        /// Checks if the child is a leaf condition - if so, it should be safe to merge the condition with other conditions, if the aliases are available.        
        /// </summary>
        public bool Simple => Child is LeafCondition;
        public bool CanUseHashCondition => HashCondition != null;
        /// <summary>
        /// Returns the condition type for the hash condition - equal or not equal
        /// </summary>
        public ConditionType? HashConditionType => HashCondition?.Comparison;
        /// <summary>
        /// Checks if the record passes Conditions
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public override bool? Evaluate(DocRecord record)
        {
            if (Child == null)
                return true;
            return Child.Evaluate(record);
        }
        /// <summary>
        /// Attempts to merge RootCondition b into this root Condition
        /// </summary>
        /// <param name="b"></param>
        /// <returns>True if b was successfully merged into this RootCondition</returns>
        public bool MergeWithRoot(RootCondition b)
        {
            if (!AliasList.IsSuperSet(b.AliasList))
                return false; //don't merge if b contains aliases not in use by this root
            if (Simple || b.Simple)
            {
                if (Simple == b.Simple)
                {
                    //Both have to be simple for HashCondition to be populated
                    var h = HashCondition;
                    var h2 = b.HashCondition;
                    if (h != null && h2 != null)
                    {
                        if (h.SetNext(h2))
                            return true;//Tries to merge the join conditions instead of using an 'AND' if possible
                    }
                    var j = Child as JoinCondition;
                    var mj = Child as MergedJoinCondition;
                    if( j != null)
                    {
                        if(b.Child is MergedJoinCondition)
                        {
                            if (b.Child.SetNext(j))
                            {
                                Child = b.Child;
                                return true;
                            }
                        }
                        if(b.Child is JoinCondition)
                        {
                            mj = new MergedJoinCondition(j.Comparison, j.leftColumn, j.rightColumn);
                            if (mj.SetNext(b.Child))
                            {
                                Child = mj;
                                return true;
                            }
                        }
                    }
                    else if(mj != null)
                    {
                        if (mj.SetNext(b.Child))
                            return true;
                    }
                }
                Child = new AndCondition(Child, b);                
                return true;
            }
            return false;
        }
    }
}
