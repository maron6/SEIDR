using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery
{
    public enum ConditionType
    {
        EQUAL,
        NOT_EQUAL,
        GREATER_THAN,
        GREATER_OR_EQUAL,
        LESS_THAN,
        LESS_THAN_OR_EQUAL,
        IS_NULL,
        IS_NOT_NULL,
        HASH_EQUAL
    }
    internal enum ConditionGroupType
    {
        AND,
        OR
    }
    public enum DataType
    {
        VARCHAR,
        DATE,
        NUMBER,
        MONEY,
        DBNULL
    }       
    public enum JoinType
    {
        NOT_A_JOIN = 0,
        INNER = 1,        
        INNER_EXPLICIT = 2,
        LEFT = 3,

        //Maybe todo? Could just rearrange into left joins to simplify though...
        RIGHT = 4,
        //TODO: would need to be done with a working file I think.. I.e., join two files at a time, 
        //then put into a working file to use in the next join. I don't see it working well with 
        //the enumerable approach though
        FULL = 5,
    }
}
