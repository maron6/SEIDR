using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.META;

namespace SEIDR.Doc.DocQuery.Predicates
{
    public class ConditionParser
    {
        /// <summary>
        /// Note: Should be stopped before a where
        /// </summary>
        string[] KeyWords = new[]
        {
            "AND",
            "OR",
            "(",
            ")",
            "!", 
            //"=", "<>", "!=", ">", ">=", "<", "<="            
        };
        string[] JoinConditionKeys = new[]
        {
             "<>", "!=", ">", ">=", "<", "<=", "=",
        };
        static BasicLeaf ParsePartialExpression(string content)
        {            
            BasicLeaf b = null;

            return b;
        }
        /// <summary>
        /// Delegate for parsing expressions (functionality set in <see cref="LeafCondition.ContentInformation"/>).
        /// <para>Will be the content on either side of comparison operators (e.g., '&lt;&gt;', '=', '&lt;=', '&gt;', etc)</para>
        /// </summary>
        public static Func<string, BasicLeaf> ParseExpression { get; set; } = ParsePartialExpression;
        LeafCondition ParseLeafExpression(string content)
        {
            if (content.ContainsAnySubstring(JoinConditionKeys))
            {
                var s = content.SplitByKeyword(JoinConditionKeys, true).ToArray();
                JoinCondition jc = null;
                var l = ParseExpression(s[0]);
                ConditionType c;
                switch (s[1])
                {
                    case "=":
                        c = ConditionType.EQUAL;
                        break;
                    case "!=":
                    case "<>":
                        c = ConditionType.NOT_EQUAL;
                        break;
                    case ">":
                        c = ConditionType.GREATER_THAN;
                        break;
                    case ">=":
                        c = ConditionType.GREATER_OR_EQUAL;
                        break;
                    case "<":
                        c = ConditionType.LESS_THAN;
                        break;
                    case "<=":
                        c = ConditionType.LESS_THAN_OR_EQUAL;
                        break;
                    default:
                        throw new InvalidOperationException("Unrecognized condition: " + s[1]);
                }
                jc = new JoinCondition(c, l);
                jc.SetRightSide(ParseExpression(s[2]));
                return jc;
            }            
            return ParseExpression(content);                                        
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="content">If a join, should be the portion between ON but not including and either the next join or "WHERE", ";", "SELECT", "UNION"<para>
        /// If a where, should be the portion between but not including the next ";", "SELECT", "UNION" </para></param>
        /// <returns></returns>
        public RootCondition Parse(string content)
        {            
            RootCondition rc = new RootCondition();
            var conditionStack = new Stack<RootCondition>();
            iCondition work = null;
            Tokenizer tk = new Tokenizer(content, false, '(', '!', ')', ',');
            while (tk.HasMoreTokens)
            {
                string x;
                string current = tk.GetNextToken();
                switch (current.ToUpper())
                {
                    case "(":
                        conditionStack.Push(new RootCondition(work));                                                
                        break;
                    case ")":
                        var p = conditionStack.Peek();
                        if (work != null)
                        {
                            if(p.Child != null)
                            {
                                //Try to condense by flipping condition on a not..
                                if(p.Child is NotCondition)
                                {
                                    if(work is JoinCondition)
                                    {
                                        ((JoinCondition)work).FlipComparison();
                                        p.Child = work;
                                    }
                                    else if(work is MergedJoinCondition)
                                    {
                                        ((MergedJoinCondition)work).FlipComparison();
                                        p.Child = work;
                                    }
                                    else
                                    {
                                        p.Child.SetNext(work);
                                    }
                                }
                                else
                                {
                                    p.Child.SetNext(work);
                                }
                            }
                            else
                                p.Child = work;
                        }
                        work = p;
                        break;
                    case "!":        
                        if(work != null && work.Full)                        
                            throw new InvalidOperationException("Tried to parse a '!', no room in predicate tree. Check Syntax.");

                        NotCondition n = new NotCondition(null);                        
                        if(tk.Peek(out x))
                        {
                            if(x != "(")
                            throw new InvalidOperationException("Expected '(', found '" + x + "'");
                        }
                        else
                        {
                            throw new InvalidOperationException("Query content ended unexpectedly");
                        }
                        if (work == null || work.SetNext(n))
                            work = n;                        
                        break;
                    case "AND":
                        AndCondition a = new AndCondition(work, null);
                        work = a;
                        break;
                    case "OR":
                        OrCondition o = new OrCondition(work, null);
                        work = o;
                        break;
                    case "IS":
                        x = tk.GetNextToken().ToUpper();
                        string x2 = null;
                        IsNullCondition c;
                        if (x == "NOT" && (x2 = tk.GetNextToken().ToUpper()) == "NULL")
                        {
                            c = new IsNullCondition(work as BasicLeaf, false);
                        }
                        else if (x == "NOT" || (x2 ?? x) != "NULL")
                            throw new InvalidOperationException("Expected NULL, found " + x2 ?? x);                                                    
                        else
                            c = new IsNullCondition(work as BasicLeaf, true);
                        work = c;
                        break;
                    default:
                        if (work != null && work.Full)
                            throw new InvalidOperationException("Tried to parse expression, but no room for a leaf condition. Check syntax");
                        StringBuilder sb = new StringBuilder(current);
                        string w;
                        while(tk.Peek(out w))
                        {
                            if (w.In(KeyWords))
                                break;
                            sb.Append(" ");
                            sb.Append(w);
                        }
                        LeafCondition lc = ParseExpression(sb.ToString());                        
                        var bc = work as BinaryCondition;
                        var uc = work as UnaryCondition;
                        if (bc != null)
                        {
                            bc.SetNext(lc);
                            if (bc is AndCondition && bc.Full)
                            {
                                JoinCondition r = bc.Right as JoinCondition;
                                JoinCondition l = bc.Left as JoinCondition;
                                MergedJoinCondition mj = bc.Left as MergedJoinCondition;
                                if (mj != null && mj.Comparison == r.Comparison)
                                {
                                    if (mj.SetNext(r))
                                        work = mj; //replace the And Condition
                                }
                                if (r != null && l != null 
                                    && r.Comparison == l.Comparison 
                                    && r.Comparison.In(ConditionType.EQUAL, ConditionType.NOT_EQUAL, ConditionType.HASH_EQUAL))
                                {
                                    mj = new MergedJoinCondition(l.Comparison, l.leftColumn, l.rightColumn);
                                    if (mj.SetNext(r))
                                        work = mj;
                                }
                            }
                        }
                        else if (uc != null)
                        {
                            uc.SetNext(lc);
                            //Maybe only do flipping after ')'? 
                            //Actually, ! should probably only come before '(' and would then be flipped after ')'
                            /*
                            if (uc is NotCondition)
                            {
                                if(lc is JoinCondition)
                                {
                                    ((JoinCondition)lc).FlipComparison();
                                    work = lc;
                                }
                                else if(lc is MergedJoinCondition)
                                {

                                }
                            }*/
                        }
                        else
                            work = lc;                        
                        break;
                }
            }
            while(conditionStack.Count > 0)
            {
                conditionStack.Peek().Child = work;
                work = conditionStack.Pop();
            }
            rc.Child = work;
            return rc;
        }
    }
}
