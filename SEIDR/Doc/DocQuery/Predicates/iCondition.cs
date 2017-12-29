using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery.Predicates
{
    public interface iCondition
    {
        IList<string> AliasList { get; }
        int NestedCount { get; }
        bool Full { get; }
        bool? Evaluate(DocRecord record);
        bool SetNext(iCondition next);        
    }
    public abstract class LeafCondition: iCondition
    {
        public IList<string> AliasList
        {
            get
            {
                return new string[] { ContentInformation.OwnerAlias };
            }
        }
        public TransformedColumnMetaData ContentInformation { get; protected set; } = null;
        public int NestedCount => 1;
        public bool Full { get { return true; } }
        public abstract bool? Evaluate(DocRecord record);
        public virtual bool SetNext(iCondition next) { return false; }
    }
    public abstract class UnaryCondition: iCondition
    {
        public IList<string> AliasList
        {
            get
            {
                return Child.AliasList;
            }
        }
        public int NestedCount => Child.NestedCount + 1;
        public iCondition Child { get; set; }
        public bool Full { get { return Child != null; } }
        public UnaryCondition(iCondition child)
        {
            Child = child;
        }        
        public abstract bool? Evaluate(DocRecord record);
        public bool SetNext(iCondition next)
        {
            if (Full)
                return false;
            Child = next;
            return true;
        }
    }
    public abstract class BinaryCondition: iCondition
    {
        public IList<string> AliasList
        {
            get
            {
                return Left.AliasList.Union(Right.AliasList, true);
            }
        }
        public int NestedCount => Left.NestedCount + Right.NestedCount + 1;
        public iCondition Left { get; set; }
        public iCondition Right { get; set; }
        public BinaryCondition(iCondition left, iCondition right)
        {
            Left = left;
            Right = right;
        }
        public bool Full { get { return Left != null && Right != null; } }
        public abstract bool? Evaluate(DocRecord record);
        public bool SetNext(iCondition next)
        {
            if (Full)
                return false;
            if (Left == null)
                Left = next;
            else
                Right = next;
            return true;
        }
    }
    /*
     * Process: 
     * add parenthesis to allow parsing one token at a time.
     * ! a and b -> ((!a) and b)
     
     * and
     * |    \
     * a    b

     * ! a and b or c -> ( ( (!a) and b) or c)
     * or
     * |    \
     * and   c
     * |    \
     * not  b
     * |
     * a     
     * 
     * !a and b or c and d ->       (((!a) and b) or (c and d))... Need to be able to parse this stuff into conditions...
     * 
     * or: Start parenthesis at next item. ends after an item? Shift starting point for parenthesis?
     * e.g. a or b and c -> a or (b and c)
     * a or b and (c and d) -> a or (b and (c and d))
     * a or b and 
     * 
     * a or b and c -> starts at 0, then reach or, parenthesis start at 2. next or without parenthesis moves the parenthesis start point forward again.
     * Or should also surround 0 through start with parenthesis? Should have already been surrounded?
     * (a) or b and c
     * (a) or (b and c)
     * if there were an or after, maybe surround 0 -> previous? 
     * a and b or b and c and d or e
     * (a and b)
     * (a and b) or (b and c) 
     * (a and b) or ((b and c) and d)
     * ((a and b) or ((b and c) and d) or e
     
    
      start parsing..create a root, on start parenthesis, create a left, if reach a new left, add to a stack... 
      on right parenthesis, go back up the stack and create a right side, link with with the most recent left? 
      
    * --
    * a     <- pointer
    * --
    * a and b  <-
    * -> right, go up stack
    * 
    * -- <-
    * a and b    
    * 
    * left parent, add stack.
    * --
    * --    <-
    * a and b
    * 
    * left paren
    * 
    * --    
    * OR    <-
    * a and b
    * 
    * --
    * OR *
    * a and b  -- <-
    * 
    * --
    * OR *
    * a and b   b <-
    * 
    * --
    * OR *
    * a and b   b and c<-
    * 
    * --
    * OR    *   <-
    * a and b   b and c
        
      I think a stack is the way to go, but will need to think it over a bit more...
     * then nest parse token...
     * a    b   e
     * b    c
     *      d
     * convert to binary expression tree then. create combine leaves with parent..
     * 
     * a and b
     * 
     *     or   b
     * a and b
     * 
     * go depth first and create connections?
     * 
     * Note: bool operator not valid as direct child/follower from another bool operator
     * ! -> parenthesis around following item (unless already parenthesis). 
     * and ->  parenthesis around the parent and the child. 
     * or -> parenthesis around the child. Start parenthesis, non bool operator ends parenthesis?
     */
}
