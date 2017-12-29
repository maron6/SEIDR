using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery.Predicates
{
    /// <summary>
    /// Single child condition. 
    /// </summary>
    class NotCondition : UnaryCondition
    {
        public NotCondition(iCondition single)
            :base(single)
        {            
        }
        public override bool? Evaluate(DocRecord record)
        {
            return !Child.Evaluate(record);
        }
    }
}
