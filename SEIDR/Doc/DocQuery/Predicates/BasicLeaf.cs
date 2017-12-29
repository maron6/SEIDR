using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery.Predicates
{
    public class BasicLeaf : LeafCondition
    {
        public BasicLeaf(TransformedColumnMetaData content)
        {
            ContentInformation = content;            
        }
        public override bool? Evaluate(DocRecord record)
        {
            throw new NotImplementedException();
        }
    }
}
