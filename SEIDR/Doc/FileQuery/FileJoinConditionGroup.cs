using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.FileQuery
{
    class FileJoinConditionGroup
    {        
        IEnumerable<FileJoinCondition> Conditions; //And's
        IEnumerable<FileJoinConditionGroup> ConditionGroups; //Or's?
        FileJoinConditionRelation relation;
        public static FileJoinConditionGroup And(FileJoinConditionGroup a, FileJoinConditionGroup b)
        {
            if (a.relation == FileJoinConditionRelation.AND && b.relation == FileJoinConditionRelation.AND)
            {
                return new FileJoinConditionGroup
                {
                    Conditions = a.Conditions.Union(b.Conditions)
                    ,relation = FileJoinConditionRelation.AND
                };
            }
            return new FileJoinConditionGroup();
        }
        public static FileJoinConditionGroup Or(FileJoinConditionGroup a, FileJoinConditionGroup b)
        {
            return new FileJoinConditionGroup { relation = FileJoinConditionRelation.OR };
        }
    }
}
