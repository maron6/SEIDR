using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.META;
using SEIDR.Doc.DocQuery.Predicates;

namespace SEIDR.Doc.DocQuery
{
    public class DocQueryParser
    {
        string[] KeyWords = new[]
        {
            "JOIN", "INNER", "LEFT", "RIGHT", "FULL"
            , "WHERE", "UNION", "UNION ALL", "SELECT", "FROM",
            "ON", ";", "LET", "SET"
        };
        DocQuerySettings s;
        public DocQueryParser(DocQuerySettings settings)
        {
            s = settings;            
        }
        public DocQuery Parse(string QueryText)
        {
            DocQuery q = new DocQuery(s);
            ConditionParser p = new ConditionParser();
            Tokenizer tk = new Tokenizer(QueryText);
            string LastKeyWord = null;
            //IEnumerable<string> content = QueryText.SplitByKeyword(KeyWords, true, true);
            //foreach (string c in content){} //note: will mess things up in cases like SettingsFile -> "", "Set", "tingsFile"
            while (tk.HasMoreTokens)
            {
                string keyword = tk.GetNextToken().ToUpper();
                if (keyword.NotIn(KeyWords))
                    throw new InvalidOperationException("Invalid syntax: Expected Keyword, found '" + keyword + "'");
                switch (keyword)
                {

                }
            }
            return q;
        }
    }
}
