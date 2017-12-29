namespace SEIDR.Doc.FileQuery
{
    enum FileJoinConditionType
    {
        Equal,
        NotEqual,
        IsNULL,
        IsNotNULL,
        GreaterThan,
        LessThan,
        GreaterThanEqual,
        LessThanEqual
    }
    enum FileJoinConditionRelation
    {
        AND,
        OR
    }
    enum FileJoinType
    {
        inner,
        left,
        right,
        full
    }
    enum FileQueryColumnType
    {
        varchar,
        date,
        number
    }
}