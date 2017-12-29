using SEIDR.META;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery
{
   
    /// <summary>
    /// Experiment for querying (select, inner join, left join, where clause filter) records from a delimited file.    
    /// <para>Syntax:</para><br />
    /// <para>ALIAS ______:(filepath)</para>
    /// <para>...</para>
    /// <para>SELECT [__ALIAS__].[ColumnName], [__ALIAS__].[ColumnName], ...</para>
    /// <para>FROM [__ALIAS__]</para>
    /// <para>{JOIN|LEFT JOIN} [__ALIAS__] ON [__ALIAS__].[ColumnName] {==|&lt;=|&gt;|&lt;|&gt;=|!=} {[__ALIAS__].[ColumnName] | __LITERAL__} { AND | OR __remaining conditions__ }</para>
    /// <para>{WHERE [__ALIAS__].[ColumnName] { IS {NOT} NULL | {{==|&lt;=|&gt;|&lt;|&gt;=|!=} {[__ALIAS__].[ColumnName] | __LITERAL__}  } } {AND | OR __Remainin filters__}(</para>
    /// <para>Note: ands are grouped before Or, unless in parenthesis..</para>
    /// <br /> On Data types - to specify non varchar, do the following: {DATE|MONEY|NUM}([__ALIAS__].[ColumnName])
    /// <br />On Literals- specify date with {DATE|MONEY|NUM|}( __Literal___)
    /// </summary>
    class DelimitedQuery //: IEnumerable<DelimitedRecord>
    {
        DocMetaData[] Docs;
        DocRecordColumnInfo[] Output;
        ConditionTree Filter;
        //Dictionary<string, ConditionTree> Joins;
        Dictionary<string, DelimitedJoin> Joins;
        private DelimitedQuery()
        {

        }        
        string[] Header => Output.Select(o => o.ColumnName).ToArray();
        /// <summary>
        /// Execute query, writes output to destination file.
        /// </summary>
        /// <returns></returns>
        public void Execute(string DestinationFile, char delimiter = DelimitedDocumentWriter.DefaultDelimiter)
        {
            using (DelimitedDocumentWriter dw
                = new DelimitedDocumentWriter(DestinationFile, delimiter, false, Header))
            {
                foreach (var joined in this)
                {
                    /*
                    var q = (from o in Output
                             select joined[o.OwnerAlias][o.ColumnName]
                             ).ToArray();
                    dw.AddRecord(new DelimitedRecord(q));
                    dw.AddRecord(joined);
                    */
                }
            }
        }        
        /// <summary>
        /// Safe call to the getEnumerator (implicit foreach on instance)
        /// <para>If Query is not in valid state, skips any records</para>
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IRecord> Execute()
        {
            if (!IsValid)
                yield break;
            foreach (var joined in this)
                yield return joined;
        }/*
        DelimitedRecord doSelect(JoinedDelimitedRecords work)
        {
            var q = (from o in Output
                     select work[o.OwnerAlias][o.ColumnName]
                             ).ToArray();
            return new DelimitedRecord(Header, q, Header.Length);
        }*/
        DelimitedRecord doSelect(DelimitedRecord work)
        {
            return DelimitedRecord.GetSubset(work, Output);
        }
        /*
        IEnumerable<JoinedDelimitedRecords> doJoins(JoinedDelimitedRecords work, int index)
        {
            DelimitedJoin j = Joins[Docs[index].Alias];
            index++; //Increment index for the recursive call to the next join.
            //j.SetJoinRecord(work);
            j.SetJoinRecord(work);
            foreach(var jrecord in j)
            {
                if (index < Docs.Length)
                {
                    foreach(var njrecord in doJoins(jrecord, index))
                    {
                        yield return njrecord;
                    }
                }
                else
                {
                    yield return jrecord;
                }
            }
        }
        */
        IEnumerable<DelimitedRecord> doJoin(DelimitedRecord work, int index)
        {
            DelimitedJoin j = Joins[Docs[index].Alias];
            index++; //Increment index for the recursive call to the next join.
            j.SetJoinRecord(work);
            //Enumerate through the matches from this delimited join
            foreach (var jrecord in j)
            {
                //If there are more joins to do, merge pass this record to the next join 
                //and yield return the result of merging with it
                if (index < Docs.Length)
                {                    
                    foreach (var njrecord in doJoin(jrecord, index))
                    {
                        yield return DelimitedRecord.Merge(work, jrecord);
                    }
                }
                else //End of merging, 
                {
                    yield return jrecord;
                }
            }
        }

        /// <summary>
        /// Executes query, yields Delimited records instead of creating an output file
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IRecord> GetEnumerator()
        {
            if (!IsValid)
                throw new InvalidOperationException("Query State is invalid! Check Aliases and joins");                        
            using (var reader = new DelimitedDocumentReader(Docs[0].FilePath))
            {
                foreach(var record in reader)
                {
                    /*
                    //JoinedDelimitedRecords r = new JoinedDelimitedRecords(record);
                    if(Docs.Length > 1)
                    {
                        //foreach(JoinedDelimitedRecords r2 in doJoins(r, 1))
                        foreach(IRecord r2 in doJoin(record, 1))
                        {
                            if(Filter.CheckJoinedConditions(r2))
                                yield return doSelect(r2);
                        }                        
                    }
                    else
                    {
                        //if(Filter.CheckConditions(r.Content))
                        //    yield return doSelect(r);
                        if (Filter.CheckJoinedConditions(record))
                            yield return doSelect(record);
                    }
                    */
                }
            }
            yield break;

        }        
        /// <summary>
        /// Check if the query is valid before running.
        /// <para>An invalid operation exception will be thrown if this returns false when executing.</para>
        /// </summary>
        public bool IsValid
        {
            get
            {
                //Note: There should be a join for each alias, even though the first alias will not be used...
                if (Filter == null || Joins == null || Joins.Count != Docs.Length)
                    return false;
                if (Output == null || Output.Length == 0)
                    return false;
                return true;
            }
        }
        /// <summary>
        /// Parse a script to create a DelimitedQuery to run
        /// </summary>
        /// <param name="QueryContent"></param>
        /// <returns></returns>
        public static DelimitedQuery Parse(string QueryContent)
        {
            Action<string, string> badToken = (expected, found) => {
                throw new FormatException("Expected '" + expected + "', but found '" + found + "'.");
            };
            Tokenizer t = new Tokenizer(QueryContent.Trim());
            DelimitedQuery q = new DelimitedQuery();
            q.Joins = new Dictionary<string, DelimitedJoin>();
            q.Filter = new ConditionTree();
            var aliases = new Dictionary<string, DocMetaData>();
            DocMetaData[] aliasList = null;
            List<DocRecordColumnInfo> columns = new List<DocRecordColumnInfo>();
            while (t.HasMoreTokens)
            {
                string x = t.GetNextToken().ToUpper();
                if(x == "ALIAS")
                {
                    string alias = t.GetNextToken();
                    x = t.GetNextToken();
                    if (x != ":")
                        badToken(":", x);                        
                    x = t.GetNextToken();
                    string file = "";
                    if (x == "\"")
                        t.MergeUntil(ref file, @"""");
                    else
                        file = x;
                    aliases.Add(alias, new DocMetaData(file, alias));
                    continue;
                }
                if(x == "WHERE")
                {
                    //ToDo: add conditions to the "Filter" condition tree
                }
                if(x == "FROM")
                {
                    aliasList = new DocMetaData[aliases.Count];
                    //Done with setting aliases...
                    //Doc[0]...
                }                                                    
                else if(x.In("LEFT", "INNER", "JOIN", "RIGHT", "FULL"))
                {
                    JoinType jt = JoinType.INNER;
                    if (x.In("LEFT", "INNER"))
                    {
                        jt = x == "LEFT" ? JoinType.LEFT : JoinType.INNER_EXPLICIT;
                        x = t.GetNextToken();
                        if (x.ToUpper() != "JOIN")
                            throw new FormatException("Expected 'JOIN', found '" + x + "'");
                    }
                    x = t.GetNextToken();
                    DocMetaData i;
                    if (!aliases.TryGetValue(x, out i))
                        throw new FormatException("Unknown alias found: '" + x + "'");
                    x = t.GetNextToken().ToUpper();
                    if (x != "ON")
                    {
                        if (jt.In(JoinType.LEFT, JoinType.INNER_EXPLICIT))                        
                            throw new FormatException("Explicit left or inner join, expected 'ON' token, found '" + x + "'");                        
                        else
                            jt = JoinType.NOT_A_JOIN;
                    }
                    ConditionTree ct = new ConditionTree();
                    if(jt > 0)
                    {
                        ConditionNode working = null;
                        ConditionGroupType cgt = ConditionGroupType.AND;
                        int pCount = 0;
                        x = t.GetNextToken();
                        List<Condition> andChain = new List<Condition>();
                        Stack<ConditionNode> parentScope = new Stack<ConditionNode>(); 
                        //On Entering a '(' after an 'AND' or 'OR', put working on the stack and add a null condition
                        
                        //Idea: nest conditions, then after finishing a ')', go to parent?...
                        /*
                         * (a and b) or (b and c and d or a) and (e or f and g) or a and g
                         * should become...
                         * ROOT
                         * a    e   e   a
                         * b    g   g   g
                         *      a   b
                         *          c
                         *          d
                         *          
                         *          
                         * ((a and b) and ( c or d) and (e or f) or a and f) and g
                         * ROOT
                         * a >              a                         
                         * b >     >   >    f
                         * c    c   d   d   g
                         * e    f   e   f
                         * g    g   g   g
                         * Get full join segment, get smallest segments - i.e., search for any '('. If none, basic chaining
                         * If find any, start a conditionNode, stick on a stack, then search for any '(' or ')'. 
                         * '(' adds more to stack, ')' processes as basic chain, then pops stack and adds as child to stack?
                         * Need to look at condition type between the entries within a group, though..
                         * Chain a, b, 
                         */
                        //Initial join, then loop while next token is AND/OR
                        do
                        {
                            //ToDo: Parse into condition tree, evaluate after setting up the tree (from raw condition)
                            //Also: need to expand conditions. E.g., (a or b) & c => a & c or b & c
                            if (x.ToUpper().In("AND", "OR"))
                            {
                                if (working == null)
                                    throw new FormatException("Expected alias or function, found '" + x + "'.");
                                if (x.ToUpper() == "AND")
                                {
                                    cgt = ConditionGroupType.AND; //Current group type..
                                }
                                else
                                    cgt = ConditionGroupType.OR;
                                x = t.GetNextToken();
                            }                            
                            if (x == "(")
                            {
                                pCount++;
                                x = t.GetNextToken();                                
                                if(working!= null)                                    
                                {
                                    parentScope.Push(working);
                                    if (cgt == ConditionGroupType.OR)
                                        working = working.AddSiblingCondition(null);
                                    else
                                        working = working.AddNode(null as Condition);
                                }
                                continue;
                            }            
                            else if(x == ")")
                            {
                                pCount--;
                                if (pCount < 0)
                                    throw new FormatException("Unmatched ')' found!");
                                x = t.GetNextToken();
                                working = parentScope.Pop();
                                continue;
                            }

                            string x2 = null;
                            string aliasLeft = null;
                            string aliasRight = null;
                            string constRight = null;
                            DataType dt = DataType.VARCHAR;
                            Func<string, TransformedData> tsfm = null;
                            if (x.ToUpper().In("DATE", "NUM", "MONEY"))
                            {
                                switch (x.ToUpper())
                                {
                                    case "DATE":
                                        dt = DataType.DATE;
                                        break;
                                    case "NUM":
                                        dt = DataType.NUMBER;
                                        break;
                                    case "MONEY":
                                        dt = DataType.MONEY;
                                        break;
                                }
                                x = t.GetNextToken();
                                if (x != "(")
                                    badToken("(", x);
                            }
                            if( x == "[")
                            {
                                x = "";
                                t.MergeUntil(ref x, "]");                                
                            }
                            aliasLeft = x;
                            x = t.GetNextToken();

                            if (x != ".")
                                badToken(".", x);                                
                            x = t.GetNextToken();
                            if(x == "[")
                            {
                                x = "";
                                t.MergeUntil(ref x, "]");
                            }
                            Condition cond;
                            ConditionType condt = ConditionType.EQUAL;
                            TransformedColumnMetaData tmcd = new TransformedColumnMetaData()
                            {
                                ColumnName = x,
                                OwnerAlias = aliasLeft,
                                Type = dt,
                                Transform = tsfm
                            };
                            //... getright or const..
                            TransformedColumnMetaData right = new TransformedColumnMetaData
                            {
                                ColumnName = x,
                                OwnerAlias = aliasRight,
                                Type = dt, //Share type?
                                Transform = tsfm //?
                            };
                            {
                                //toDo: Update condt, parse to constant..
                                x = t.GetNextToken();
                                TransformedData constantData;
                                switch (dt)
                                {

                                    default:
                                        constantData = new TransformedVarchar(x);
                                        break;
                                }
                                cond = new Condition(tmcd, constantData, condt);                                                   
                            }

                            if (working == null)
                                working = ct.AddCondition(cond);
                            else
                            {
                                if (cgt == ConditionGroupType.AND)
                                {
                                    andChain.Add(cond);
                                }
                                else
                                {
                                    if (andChain.Count > 0)
                                    {
                                        //working.AddConditions(ConditionGroupType.AND, andChain.ToArray());
                                        working.AddChainConditions(andChain.ToArray());
                                        andChain.Clear();
                                    }
                                    working = working.AddSiblingCondition(cond);
                                }
                            }

                        } while (x.ToUpper().NotIn(";", "WHERE"));
                        if (andChain.Count > 0)
                        {
                            working = ct.AddCondition(andChain[0]);
                            if (andChain.Count > 1)
                            {
                                andChain.RemoveAt(0);
                                working.AddChainConditions(andChain.ToArray());
                            }
                            andChain.Clear();
                        }                            
                        if(x == "(")
                        {
                            pCount++;
                        }
                        switch (x)
                        {
                            case "(":
                                pCount++;
                                break;
                        }                        
                    }
                    q.Joins[i.Alias] = new DelimitedJoin(i, ct, jt);
                }
            }
            if (aliasList == null || aliasList.Length == 0)
            {
                if (aliases.Count > 0)
                    throw new Exception("Aliases are not used!");
                throw new FormatException("No aliases found");
            }
            q.Docs = aliasList; //Ordered
            q.Output = columns.ToArray();
            
            /* ToDo:              
             * 
             * tokenize
             * fill column selection
             * file alias info for documents
             * populate joins for each alias
             * populate filter
             */
            //ToDo: fill the Condition trees...
            //Ensure aliases are unique...(will throw an exceptino in Joins if not)..
            return q;
        }
        /// <summary>TODO:
        /// Removes extra join conditions, moves filters to earlier joins if possible, mark conditions as hash joins where possible
        /// </summary>
        public void Optimize()
        {

        } 
    }

    
}
