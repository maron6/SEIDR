using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.META
{
    public class NestedTokenNode
    {
        public NestedTokenCondition Source { get; private set; } = null;
        public string Value { get; private set; } = null;
        public NestedTokenNode(string value, NestedTokenNode parent, NestedTokenCondition source)
        {
            Value = value;
            Parent = parent;
            Source = source;
        }
        private NestedTokenNode()
        {

        }
        /// <summary>
        /// Depth first loop through Children
        /// </summary>
        /// <returns></returns>
        public IEnumerable<NestedTokenNode> LoopNodes()
        {
            if (Children == null || Children.Count == 0)
                yield return this;
            foreach(var Child in Children)
            {
                foreach(var c in Child.LoopNodes())
                {
                    yield return c;
                }
            }
            yield return this;
        }
        public NestedTokenNode CheckTokenConditions(NestedTokenCondition PickedToken, string token)
        {         
            if(PickedToken == null)
            {
                if (Parent == null)
                    return AddChild(token, null); //Root cannot have siblings, and cannot match, so add as child?
                //No nesting to do, so add as a sibling.
                return AddSibling(token, Source);
            }   
            var c = PickedToken.Match(token);
            switch (c)
            {
                case NestedTokenCondition.NestDirection.Nest:
                    return AddChild(token, PickedToken);
                case NestedTokenCondition.NestDirection.None:
                    return AddSibling(token, PickedToken);
                case NestedTokenCondition.NestDirection.UnNest:
                    if (PickedToken.UnNestToken != Parent.Source?.UnNestToken)
                        throw new InvalidOperationException("Unexpected Token for Un-Nesting: " + PickedToken.UnNestToken + ". Expected: " + Parent.Source?.UnNestToken ?? string.Empty);
                    return Parent;
            }
            return null;
        }
        public NestedTokenCondition CheckConditions(IList<NestedTokenCondition> Conditions, string Token)
        {            
            return (from cond in Conditions
                         where cond.UnNestToken == Source?.UnNestToken
                         || cond.UnNestToken != Token
                         select cond).OrderBy(c => c.UnNestToken == Token? 0 : 1).FirstOrDefault();
        }
        public NestedTokenNode AddSibling(string tokenValue, NestedTokenCondition source)
        {
            return Parent.AddChild(tokenValue, source);
        }
        public NestedTokenNode AddChild(string TokenValue, NestedTokenCondition source)
        {
            var c = new NestedTokenNode(TokenValue, this, source);            
            return c;
        }
        public IEnumerable<NestedTokenNode> GetChildren()
        {
            return new List<NestedTokenNode>( Children);
        }
        public NestedTokenNode Parent { get; private set; } = null;
        List<NestedTokenNode> Children { get; set; } = new List<NestedTokenNode>();
        public static NestedTokenNode GetRoot()
        {
            return new NestedTokenNode();
        }
    }
}
