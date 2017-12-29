using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery.Predicates
{
    /// <summary>
    /// Leaf condition, check if a column (or constant from meta data) is null or not null
    /// </summary>
    class IsNullCondition:LeafCondition
    {        
        ConditionType cond;
        public IsNullCondition(TransformedColumnMetaData col, bool not)            
        {
            ContentInformation = col;
            cond = not ? ConditionType.IS_NOT_NULL : ConditionType.IS_NULL;
        }        
        public IsNullCondition(LeafCondition child, bool not)
        {
            ContentInformation = child.ContentInformation;
            cond = not ? ConditionType.IS_NOT_NULL : ConditionType.IS_NULL;
        }
        public override bool? Evaluate(DocRecord record)
        {
            if (ContentInformation == null)
                throw new InvalidOperationException("Expected single expression information, none found. Check syntax");
            var content = ContentInformation.GetColumn(record);
            if (cond == ConditionType.IS_NULL)
                return content.IsNull;
            return !content.IsNull;            
        }
    }
}
