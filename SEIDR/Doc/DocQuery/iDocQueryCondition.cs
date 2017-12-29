using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery
{
    interface iDocQueryCondition
    {
        List<Tuple<DocRecordColumnInfo>> GetColumns();
        //Include condition columns?
        bool? Test(DocRecord record);
        void SetUp(DocRecordColumnInfo left, DocRecordColumnInfo right, ConditionType conType);
        T Merge<T>(T left, T Right) where T : iDocQueryCondition;
    }
}
