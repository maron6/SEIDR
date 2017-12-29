using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery.Predicates
{
    /// <summary>
    /// Join condition- joins delimited record columns. left and right should be ignroed
    /// </summary>
    class JoinCondition : LeafCondition
    {        
        public void FlipComparison()
        {
            switch (Comparison)
            {
                case ConditionType.EQUAL:
                case ConditionType.HASH_EQUAL:
                    Comparison = ConditionType.NOT_EQUAL;
                    return;
                case ConditionType.NOT_EQUAL:
                    Comparison = ConditionType.EQUAL;
                    return;
                case ConditionType.GREATER_OR_EQUAL:
                    Comparison = ConditionType.LESS_THAN;
                    return;
                case ConditionType.GREATER_THAN:
                    Comparison = ConditionType.LESS_THAN_OR_EQUAL;
                    return;
                case ConditionType.LESS_THAN_OR_EQUAL:
                    Comparison = ConditionType.GREATER_THAN;
                    return;
                case ConditionType.LESS_THAN:
                    Comparison = ConditionType.GREATER_OR_EQUAL;
                    return;                    
            }
        }
        internal TransformedColumnMetaData leftColumn;
        internal TransformedColumnMetaData rightColumn;
        public ConditionType Comparison { get; private set; }
        public bool Simple
            => leftColumn.Transform.In(TransformedVarchar.BasicTransform, TransformedNum.BasicTransform, TransformedMoney.BasicTransform, TransformedDate.BasicTransform) 
            && rightColumn.Transform.In(TransformedVarchar.BasicTransform, TransformedNum.BasicTransform, TransformedMoney.BasicTransform, TransformedDate.BasicTransform);
        public JoinCondition(ConditionType cond, LeafCondition start)
        {
            Comparison = cond;
            leftColumn = start.ContentInformation;
        }
        public void SetRightSide(LeafCondition next)
        {
            rightColumn = next.ContentInformation;
        }        
        public JoinCondition(ConditionType condType, TransformedColumnMetaData left, TransformedColumnMetaData right)
        {
            Comparison = condType;
            leftColumn = left;
            rightColumn = right;
        }
        public override bool? Evaluate(DocRecord record)
            => TransformedColumn.Evaluate(leftColumn.GetColumn(record), rightColumn.GetColumn(record), Comparison);

        public override bool SetNext(iCondition next)
        {
            if (rightColumn == null && next is LeafCondition)
            {
                rightColumn = (next as LeafCondition).ContentInformation;
                return true;
            }
            return false;
        }
        public static bool Merge(JoinCondition a, JoinCondition b, ref MergedJoinCondition result)
        {
            if(a.Comparison == b.Comparison)
            {
                result.AddPair(a.leftColumn, a.rightColumn);
                result.AddPair(b.leftColumn, b.rightColumn);
                return true;
            }
            return false;
        }
    }
    public class MergedJoinCondition: LeafCondition
    {
        TransformedColumnMetaDataCollection leftSide;
        TransformedColumnMetaDataCollection rightSide;
        public void FlipComparison()
        {
            switch (Comparison)
            {
                case ConditionType.EQUAL:
                case ConditionType.HASH_EQUAL:
                    Comparison = ConditionType.NOT_EQUAL;
                    return;
                case ConditionType.NOT_EQUAL:
                    Comparison = ConditionType.EQUAL;
                    return;
                case ConditionType.GREATER_OR_EQUAL:
                    Comparison = ConditionType.LESS_THAN;
                    return;
                case ConditionType.GREATER_THAN:
                    Comparison = ConditionType.LESS_THAN_OR_EQUAL;
                    return;
                case ConditionType.LESS_THAN_OR_EQUAL:
                    Comparison = ConditionType.GREATER_THAN;
                    return;
                case ConditionType.LESS_THAN:
                    Comparison = ConditionType.GREATER_OR_EQUAL;
                    return;
            }
        }
        public ConditionType Comparison { get; private set; }
        bool Simple = true;
        public bool HashCondition => Simple && Comparison.In(ConditionType.EQUAL, ConditionType.HASH_EQUAL, ConditionType.NOT_EQUAL);
        public MergedJoinCondition(ConditionType comparison, 
            TransformedColumnMetaData initLeft, TransformedColumnMetaData initRight)
        {
            Comparison = comparison;
            leftSide = new TransformedColumnMetaDataCollection(initLeft);
            rightSide = new TransformedColumnMetaDataCollection(initRight);
            if (initLeft.Transform != TransformedVarchar.BasicTransform 
                || initRight.Transform != TransformedVarchar.BasicTransform)
                Simple = false;
        }
        public void AddPair(TransformedColumnMetaData leftNew, TransformedColumnMetaData rightNew)
        {
            if (leftNew.Transform != TransformedVarchar.BasicTransform 
                || rightNew.Transform != TransformedVarchar.BasicTransform)
                Simple = false;
            leftSide.AddMetaData(leftNew);
            rightSide.AddMetaData(rightNew);
        }

        public override bool SetNext(iCondition next)
        {
            var j = next as JoinCondition;
            var mj = next as MergedJoinCondition;
            if(j != null && j.Comparison == Comparison)
            {
                if (!j.Simple)
                    Simple = false;
                AddPair(j.leftColumn, j.rightColumn);
                return true;
            }
            if(mj != null && mj.Comparison == Comparison)
            {
                if (!mj.Simple)
                    Simple = false;
                for (int i = 0; i < mj.leftSide.Count; i++)
                {                    
                    AddPair(mj.leftSide[i], mj.rightSide[i]);
                }
                return true;
            }
            return false;
        }
        public override bool? Evaluate(DocRecord record)
        {
            for(int i = 0; i < leftSide.Count; i++)
            {
                if (!TransformedColumn
                    .Evaluate(
                        leftSide[i].GetColumn(record),
                        rightSide[i].GetColumn(record),
                        Comparison) ?? false)
                    return false;
            }
            return true;
        }
    }
}
