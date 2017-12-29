using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.META
{
    public class NestedTokenizer
    {
        public List<NestedTokenCondition> NestingConditions { get; private set; } = new List<NestedTokenCondition>();        
        public NestedTokenNode TokenTree { get; private set; } = NestedTokenNode.GetRoot();
        NestedTokenNode Working;
        
        /*
        Going down, should use a new token condition... Going up, need to match the un-nest condition on a parent.
        UnNest tokens should really only be paired with a single nesting token, though, so that shouldn't be an issue,
        and just means that we should leave parent's scope before leaving a parent's parent's scope 

        Root will add children immediately btw

        Tokens: Check parent for being able to unnest. 
        If Successful, parent unnests/unwinds the working node to itself
        If not successful:
            If any other conditions can cause nesting down/adding as a child to current node    
            Otherwise, add as sibling to current node. If current node does not have a parent and its children is 
            empty(root), add as a child instead
        */

    }
}
