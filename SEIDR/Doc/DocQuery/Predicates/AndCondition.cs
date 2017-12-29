using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery.Predicates
{
    class AndCondition: BinaryCondition
    {
        public AndCondition(iCondition left, iCondition right): base(left, right) { }
        public override bool? Evaluate(DocRecord record)
        {
            if(Left.NestedCount >= Right.NestedCount)
                return (Left.Evaluate(record) ?? false) && (Right.Evaluate(record) ?? false);
            return (Right.Evaluate(record) ?? false) && (Left.Evaluate(record) ?? false);
        }
    }
}
