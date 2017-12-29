using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery
{
    internal class Condition
    {
        List<DocRecordColumnInfo> leftHashColumns  = new List<DocRecordColumnInfo>();
        List<DocRecordColumnInfo> rightHashColumns = new List<DocRecordColumnInfo>();
        public void AddHashColumnPair(DocRecordColumnInfo left, DocRecordColumnInfo right)
        {
            leftHashColumns.Add(left);
            rightHashColumns.Add(right);
        }
        public void CopyHashColumns(out DocRecordColumnInfo[] left, out DocRecordColumnInfo[] right)
        {
            if( !IsHashJoin
                || leftHashColumns.Count == 0 
                || rightHashColumns.Count != leftHashColumns.Count )
            {
                left = null;
                right = null;
                return;
            }
            left = leftHashColumns.ToArray();
            right = rightHashColumns.ToArray();
        }
        TransformedColumnMetaData LeftColumnMetaData;
        TransformedColumnMetaData RightColumnMetaData;
        TransformedData RightConstantData;
        TransformedData LeftConstantData;
        ConditionType join;
        /// <summary>
        /// Condition information for checking if columns match a condition for a passed record
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="join">Determines logic to be used in matches. <para>
        /// Note: For IS NULL and IS NOT NULL, <paramref name="right"/> will be ignored.</para>
        /// <para>Also, it's probably better to use the static GetNULLCondition methodthen.</para></param>
        public Condition(TransformedColumnMetaData left, TransformedColumnMetaData right, ConditionType join)
        {
            LeftColumnMetaData = left;
            RightColumnMetaData = right;
            this.join = join;
            if(join.In(ConditionType.EQUAL, ConditionType.HASH_EQUAL))
            {
                AddHashColumnPair(left, right);
            }
        }
        public static Condition GetNULLCondition(TransformedColumnMetaData metaData, bool isNull)
            => new Condition(
                    metaData, 
                    TCNull.Value, 
                    isNull ? ConditionType.IS_NULL : ConditionType.IS_NOT_NULL);        
        public Condition(TransformedColumnMetaData md, TransformedData constant, ConditionType join)
        {
            LeftColumnMetaData = md;
            RightConstantData = constant;
            this.join = join;            
        }
        public Condition(TransformedData left, TransformedData right, ConditionType join)
        {
            LeftConstantData = left;
            RightConstantData = right;
            this.join = join;
            ConstantValue = Match(left, right);
        }
        public readonly bool? ConstantValue = null;
        public bool IsConstant
        {
            get { return ConstantValue.HasValue; }
        }
        bool? Match(TransformedData left, TransformedData right)
        {            
            switch (join)
            {
                case ConditionType.IS_NULL:
                    return left.IsNull;
                case ConditionType.IS_NOT_NULL:
                    return !left.IsNull;
                case ConditionType.EQUAL:
                    return left.Equal(right);
                case ConditionType.GREATER_OR_EQUAL:
                    return left.GreaterEqual(right);
                case ConditionType.GREATER_THAN:
                    return left.Greater(right);
                case ConditionType.NOT_EQUAL:
                    return left.NotEqual(right);
                case ConditionType.LESS_THAN:
                    return left.Less(right);
                case ConditionType.LESS_THAN_OR_EQUAL:
                    return left.LessEqual(right);
                default:
                    return null;
            }
        }

        /*
        /// <summary>
        /// Check if the content for the two columns in comparison is usable and returns a boolean based on the comparison
        /// </summary>
        /// <param name="Content"></param>
        /// <returns></returns>
        public bool? Matches(IEnumerable<DelimitedRecord> Content)
        {            
            var content = Content.Where(c => c.ALIAS == LeftColumnMetaData.OwnerAlias).FirstOrDefault();
            if (content == null)
                throw new InvalidOperationException("ALIAS '"+ LeftColumnMetaData.OwnerAlias + "' IS NOT DEFINED IN CONTENT");
            var LeftColumn = LeftColumnMetaData.GetColumn(content);            
            if (!LeftColumn.ValidContent)
                return null;
            if (RightConstantData != null)
                return Match(LeftColumn.Content, RightConstantData);                
            
            
            content = Content.Where(c=> c.ALIAS == RightColumnMetaData.OwnerAlias).FirstOrDefault();
            if (content == null)
                throw new InvalidOperationException("ALIAS '" + RightColumnMetaData.OwnerAlias + "' IS NOT DEFINED IN CONTENT");

            var RightColumn = RightColumnMetaData.GetColumn(content);
            if (!RightColumn.ValidContent)
                return null;
            return Match(LeftColumn.Content, RightColumn.Content);            
        }*/
        /// <summary>
        /// Works with a record that has already had all the content merged together into one delimited record by <see cref="DelimitedRecord.Merge(DelimitedRecord, DelimitedRecord)"/>
        /// </summary>
        /// <param name="merged"></param>
        /// <returns></returns>
        public bool? Matches(IRecord merged)
        {
            if (IsConstant)
                return ConstantValue;
            TransformedColumn RightColumn;
            if (LeftConstantData != null)
            {
                RightColumn = RightColumnMetaData.GetColumn(merged);
                if (!RightColumn.ValidContent)
                    return null;
                return Match(LeftConstantData, RightColumn.Content);
            }            
            var LeftColumn = LeftColumnMetaData.GetColumn(merged);
            if (!LeftColumn.ValidContent)
                return null;
            if (RightConstantData != null)
                return Match(LeftColumn.Content, RightConstantData);

            RightColumn = RightColumnMetaData.GetColumn(merged);
            if (!RightColumn.ValidContent)
                return null;
            return Match(LeftColumn.Content, RightColumn.Content);
        }
        public bool? Matches(IRecord left, IRecord right)
        {
            if (IsConstant)
                return ConstantValue;
            TransformedColumn RightColumn;
            if (LeftConstantData != null)
            {
                RightColumn = RightColumnMetaData.GetColumn(right);
                if (!RightColumn.ValidContent)
                    return null;
                return Match(LeftConstantData, RightColumn.Content);
            }
            //var content = left[LeftColumnMetaData.OwnerAlias, LeftColumnMetaData.ColumnName];
            //var content = Content.Where(c => c.ALIAS == LeftColumnMetaData.OwnerAlias).FirstOrDefault();
            //if (content == null)
            //    throw new InvalidOperationException("ALIAS '" + LeftColumnMetaData.OwnerAlias + "' IS NOT DEFINED IN CONTENT");
            var LeftColumn = LeftColumnMetaData.GetColumn(left);
            if (!LeftColumn.ValidContent)
                return null;
            if (RightConstantData != null)
                return Match(LeftColumn.Content, RightConstantData);


            //content = Content.Where(c => c.ALIAS == RightColumnMetaData.OwnerAlias).FirstOrDefault();
            //if (content == null)
            //    throw new InvalidOperationException("ALIAS '" + RightColumnMetaData.OwnerAlias + "' IS NOT DEFINED IN CONTENT");

            RightColumn = RightColumnMetaData.GetColumn(right);
            if (!RightColumn.ValidContent)
                return null;
            return Match(LeftColumn.Content, RightColumn.Content);
        }
        
        public bool? Matches(ulong? Left, ulong? Right, IRecord left, IRecord right, bool ExcludeEmpty)
        {
            if (join != ConditionType.HASH_EQUAL)
                throw new InvalidOperationException("Cannot do a hash match when join type is not a Hash join");
            
            if (Left == null)
                Left = left.GetPartialHash(true, ExcludeEmpty, true, leftHashColumns.ToArray());
            if (Left == null)
                return null;
            Right =  Right ?? right.GetPartialHash(true, ExcludeEmpty, true, rightHashColumns.ToArray());
            if (Right == null)
                return null;
            return Left == Right;
        }        
        public void ConvertToHashJoin()
        {            
            if (join.NotIn(ConditionType.EQUAL, ConditionType.HASH_EQUAL))
                throw new InvalidOperationException("Cannot Convert to hash join from current Join Type (" + join.ToString() + ")");
            if (leftHashColumns.Count < 2 || rightHashColumns.Count != leftHashColumns.Count)
            {
                System.Diagnostics.Debug.WriteLine("Hash columns is below two or doesn't match. Ignoring request to convert to hash join.");
                return;
            }
            join = ConditionType.HASH_EQUAL;
        }
        public bool IsHashJoin => join == ConditionType.HASH_EQUAL;
    }
    internal class ConditionNode
    {
        public Condition MyCondition;
        public List<ConditionNode> Children;
        /// <summary>
        /// Count of nodes with populated conditions
        /// </summary>
        public int ConditionCount
        {
            get
            {
                int c = MyCondition == null ? 0 : 1;
                foreach (var child in Children)
                    c += child.ConditionCount;
                return c;
            }
        }
        public ConditionNode this[int x]
        {
            get { return Children?[x]; }
        }
        /*
        public bool CheckConditions(IEnumerable<DelimitedRecord> content)
        {                        
            if (MyCondition != null)
            {
                bool x = MyCondition.Matches(content) ?? false;
                if (!x || Children.Count == 0)
                    return x;
            }
            foreach (var n in Children)
            {
                if (n.CheckConditions(content) )
                    return true;
            }
            return false;            
        }*/
        public bool CheckConditions(IRecord left, IRecord right, 
            ulong? LeftHash = null, ulong? RightHash = null, bool ExcludeEmpty = true)
        {
            if (MyCondition != null)
            {
                bool x = false;
                if(MyCondition.IsHashJoin)
                {
                    x = MyCondition.Matches(LeftHash, RightHash, left, right, ExcludeEmpty) ?? false;
                }
                else
                    x = MyCondition.Matches(left, right) ?? false;

                if (!x || Children.Count == 0)
                    return x;
            }
            foreach (var n in Children) //Children have an 'OR' relation to each other
            {
                if (n.CheckConditions(left, right, ExcludeEmpty: ExcludeEmpty)) //hashes don't propagate.
                    return true;
            }
            return false;
        }
        public bool CheckConditions(IRecord merged)
        {
            if (MyCondition != null)
            {
                bool x = MyCondition.Matches(merged) ?? false;
                if (!x || Children.Count == 0)
                    return x;
            }
            foreach (var n in Children)
            {
                if (n.CheckConditions(merged))
                    return true;
            }
            return false;
        }
        public ConditionNode Parent { get; private set; }
        public ConditionNode(Condition value)
        {
            MyCondition = value; //null value is ok.
            Parent = null;
            Children = new List<ConditionNode>();
        }
        /// <summary>
        /// Adds the condition as a sibling ('OR' condition)
        /// </summary>
        /// <param name="OR"></param>
        /// <returns></returns>
        public ConditionNode AddSiblingCondition(Condition OR)
            => Parent?.AddNode(OR);

        private void AddConditions(ConditionGroupType grouping, params Condition[] toAdd)
        {
            if (toAdd.Length == 0)
                return;
            if (grouping == ConditionGroupType.AND)
            {
                ConditionNode n = new ConditionNode(toAdd[0]);
                n.Parent = this;
                Children.Add(n);
                for (int i = 1; i < toAdd.Length; i++)
                {
                    var x = new ConditionNode(toAdd[i]);
                    x.Parent = n;
                    n.Children.Add(x);
                    n = x;
                }
                return;
            }
            foreach (var n in toAdd)
            {
                Children.Add(new ConditionNode(n) { Parent = this });
            }
        }
        public void AddSiblingConditions(params Condition[] toAdd)
        {
            toAdd.ForEach(c => AddSiblingCondition(c));
        }
        /// <summary>
        /// Note that this is adding ConditionNodes - i.e., they can already have their own children populated
        /// </summary>
        /// <param name="toAdd"></param>
        public void AddChainNodes(params ConditionNode[] toAdd)
        {
            if (toAdd.Length == 0)
                return;
            if(Children.Count == 0)
            {
                Children.AddRange(toAdd);
            }
            else
            {
                Children.ForEach(c => c.AddChainNodes(toAdd));
            }
        }
        public void AddChainConditions(params Condition[] toAdd)
        {
            if (toAdd.Length == 0)
                return;
            if(Children.Count == 0)
            {
                AddConditions(ConditionGroupType.AND, toAdd);
            }
            else
            {
                Children.ForEach(c => c.AddChainConditions(toAdd));
            }

        }
        /// <summary>
        /// Add a list of 'OR' grouped chains to the bottom of each child recursively
        /// </summary>
        /// <param name="toAdd"></param>
        public void AddFlatChainConditions(params Condition[] toAdd)
        {
            if (toAdd.Length == 0)
                return;
            if(Children.Count == 0)
            {
                AddConditions(ConditionGroupType.OR, toAdd);             
            }
            else
            {
                Children.ForEach(c => c.AddFlatChainConditions(toAdd));
            }
        }
        public ConditionNode AddNode(Condition n)
            => AddNode(new ConditionNode(n));
        /// <summary>
        /// If the new node would be a constant (false), it is ignored, and the current instance is returned instead
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public ConditionNode AddNode(ConditionNode n)
        {
            if(n.MyCondition.ConstantValue == false)
            {
                return this;
            }
            n.Parent = this;
            Children.Add(n);
            return n;
        }
        public IEnumerable<ConditionNode> Siblings
            => Parent?.Children.Where(c => c != this);
    }
    internal class ConditionTree
    {
        /// <summary>
        /// Columns for the left side. Should be populated by optimization
        /// </summary>
        public DocRecordColumnInfo[] LeftHashColumns= null;
        public DocRecordColumnInfo[] RightHashColumns = null;
        public void Optimize()
        {

            Root.MyCondition.CopyHashColumns(out LeftHashColumns, out RightHashColumns); //If can be propagated up to root, 
            //then this is only thing we need to check for the condition tree.
        }
        public ConditionTree(){
            Root = new ConditionNode(null);
        }
        public readonly ConditionNode Root;        
        internal ConditionTree(Condition root)
        {
            Root = new ConditionNode(root);
        }
        public ConditionNode AddCondition(Condition n)
            => Root.AddNode(new ConditionNode(n));
        /*
        public bool CheckConditions(IEnumerable<DelimitedRecord> records)
        {
            if (Root == null || Root.Children.Count == 0)
                return true;

             return Root.CheckConditions(records);
        }*/
        public bool CheckConditions(IRecord left, IRecord right, 
            /*ulong? hashLeft, ulong? hashRight,*/ bool ExcludeEmptyFromHash)
        {
            if (Root == null || Root.Children.Count == 0)
                return true;
            //If the root can't do a hash after optimization, don't pass the hash value
            return Root.CheckConditions(left, right, 
                LeftHash:null, RightHash: null, ExcludeEmpty: ExcludeEmptyFromHash);
        }
        public bool CheckJoinedConditions(IRecord merged)
        {
            if (Root == null || Root.Children.Count == 0)
                return true;
            return Root.CheckConditions(merged);
        }
        public int ConditionCount => Root.ConditionCount;
    }
    /*
    public class ConditionChain
    {        
        //Tree would probably be best.
        ChainLink Root;
        class ChainLink
        {
            public Condition LinkContent; 
            public ConditionGroupType LinkType;
            public ChainLink NextLink;
            public bool CheckLink()
            {
                if(!(LinkContent.Matches() ?? false))
                {
                    if (LinkType == ConditionGroupType.AND)
                        return false;
                    return NextLink?.CheckLink() ?? false;
                }
                if (LinkType == ConditionGroupType.OR)
                    return true;
                return NextLink?.CheckLink() ?? false;
            }
        }
        public bool Valid
        {
            get
            {
                return Root?.CheckLink() ?? false;
            }
        }
    }
    */

}
