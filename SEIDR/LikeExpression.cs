
using System.Collections.Generic;
using System.Linq;

namespace SEIDR
{
    /// <summary>
    /// Used for comparing strings using a sql WildCard '%' and '_'. Also for:
    /// <para>* Finding the first or last occurance of such a string in an array of strings</para>
    /// <para>* Finding the first substring occurrence of a condition within a single string. (Other methods match condition against ENTIRE string)</para>
    /// </summary>
    /// <remarks>For Reference, the Original Intended use is to use this to find column names in the header of a file that match a pattern like 'ac%t' and use that index in a Processor in order to make a generic report generator for a given type of file.</remarks>
    public class LikeExpressions
    {
        /// <summary>
        /// If true, will not escape regex special characters, allowing you to take advantage of regex in your calling method.
        /// </summary>
        public bool AllowRegex = false;
        #region not working as intended. To Remove or review
        
        /// <summary>
        /// Checks for the substring within the full line that matches the condition of a like Statement. Requires matching case
        /// </summary>
        /// <param name="fullLine">Full string to check</param>
        /// <param name="condition">LIKE Condition</param>        
        /// <returns>First occurrence of a substring of fullLine that matches condition, or null.</returns>
        /*public*/ string SearchStringWithCase(string fullLine, string condition)
        {
            condition = condition.Replace("_", ".{1}?");
            condition = condition.Replace("%", ".+");
            System.Text.RegularExpressions.Match result = System.Text.RegularExpressions.Regex.Match(fullLine, condition);
            if (result.Success)
            {
                return result.Value;
            }
            return null;
        }
        /// <summary>
        /// Checks for the substring within the full line that matches the condition of a like Statement. Ignores case.
        /// </summary>
        /// <param name="fullLine">Full string to check</param>
        /// <param name="condition">LIKE Condition</param>        
        /// <returns>First occurrence of a substring of fullLine that matches condition, or null.</returns>
        /*public*/ string SearchString(string fullLine, string condition)
        {
            condition = condition.Replace("_", ".{1}?");
            condition = condition.Replace("%", ".+");
            System.Text.RegularExpressions.Match result = System.Text.RegularExpressions.Regex.Match(fullLine, condition, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (result.Success)
            {
                return result.Value;
            }
            return null;
        }
        
        #endregion
        /// <summary>
        /// Checks if a string fits a comparison check using the SQL WildCards '%' and '_'. Case is ignored.
        /// </summary>
        /// <param name="line">Line to check.</param>
        /// <param name="comparison">What would go in the LIKE expression</param>
        /// <returns>True if it passes the comparison, false otherwise</returns>
        public bool Compare(string line, string comparison)
        {
            if (string.IsNullOrEmpty(comparison) || string.IsNullOrEmpty(line))
                return line == comparison;
            /*
             * a%b -> ^a.+b&
             * a_b -> ^a.{1}?b&
             * %a_b -> a.{1}?b&
             * _a%b -> ^.{1}?a.+b&: From start, one character then a then 0 or more other characters, then b then end.
             * 
             */
            if (!AllowRegex)
            {
                foreach (char s in @"\.$^{[(|)*+?]}")
                {
                    comparison = comparison.Replace("" + s, @"\" + s);
                }
            }
            if (comparison[0] == '%')
                comparison = comparison.Substring(1);
            else if (comparison[0] == '_')
            {
                comparison = "^.{1}?" + comparison.Substring(1);
            }
            else
                comparison = "^" + comparison;

            if (comparison[comparison.Length - 1] == '%')
                comparison = comparison.Substring(0, comparison.Length - 1);
            else if (comparison[comparison.Length - 1] == '_')
                comparison = comparison.Substring(0, comparison.Length - 1) + @".{1}?\Z";
            else
                comparison = comparison + @"\Z";
            comparison = comparison.Replace("%", ".*");
            comparison = comparison.Replace("_", ".{1}?");
            // System.Console.WriteLine(comparison);
            if (System.Text.RegularExpressions.Regex.IsMatch(line, comparison, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return true;
            return false;
        }
        /// <summary>
        /// Checks if line is LIKE comparison, case sensitive.
        /// </summary>
        /// <param name="line">String we want to find a match for comparison in</param>
        /// <param name="comparison">Value we want to find inside line</param>
        /// <returns>True if line is LIKE comparison,  false otherwise</returns>
        public bool CompareWithCase(string line, string comparison)
        {
            if (string.IsNullOrEmpty(comparison) || string.IsNullOrEmpty(line))
                return line == comparison;
            if (!AllowRegex)
            {
                foreach (char s in @"\.$^{[(|)*+?]}")
                {
                    comparison = comparison.Replace("" + s, @"\" + s);
                }
            }
            if (comparison[0] == '%')
                comparison = comparison.Substring(1);
            else if (comparison[0] == '_')
                comparison = "^.{1}?" + comparison.Substring(1);
            else
                comparison = "^" + comparison;

            if (comparison[comparison.Length - 1] == '%')
                comparison = comparison.Substring(0, comparison.Length - 1);
            else if (comparison[comparison.Length - 1] == '_')
                comparison = comparison.Substring(0, comparison.Length - 1) + @".{1}?\Z";
            else
                comparison = comparison + @"\Z";
            comparison = comparison.Replace("%", ".*");
            comparison = comparison.Replace("_", ".{1}?");
            // System.Console.WriteLine(comparison);
            if (System.Text.RegularExpressions.Regex.IsMatch(line, comparison))
                return true;
            return false;

        }
        #region deprecated

        /*
        private static bool dCompare(string line, string comparison)
        {           
            
            line = line.ToLower();            
            string[] c = comparison.ToLower().Split('%');
            if (!line.StartsWith(c[0]) || !line.EndsWith(c[c.Length - 1]))
                return false;
            int last = 0;
            for (int i = 1; i < c.Length - 2; i++)
            {
                if (line.IndexOf(c[i]) < last)
                    return false;
                last = line.IndexOf(c[i]);
            }
            return true;
        }*/
        /*private static int internalCompare(string line, string comparison)
        {
            
            if(line =="")
                return -1;
            string[] cc = comparison.Split('_');
            if(cc.Length == 1)
                return line.IndexOf(cc[0]);
            int offset = 0;
            do{
                
                int update = -1;
                update = line.IndexOf(cc[0]);
                if(update < 0)
                    return update;
                offset++; //int offset = 1;
                line = line.Substring(1);                
                string check = ""+line;
                int i = 1;
                for(; i < cc.Length; i++)
                {
                    if (check.IndexOf(cc[i]) == update + cc[i - 1].Length)
                    {
                        update = check.IndexOf(cc[i]);
                        //offset++;
                        check = check.Substring(1); //accounts for wild cards and slowly moves forward with checking.
                        continue;
                    }
                    else if (cc[i] == "")
                    {
                        // offset++;
                        //update++;
                        check = check.Substring(1);
                        continue;
                    }
                    break;
                }
                if (i == cc.Length)
                {
                    //return (cc[i-1] == ""? 0 :check.IndexOf(cc[i - 1])) + offset + cc[i-1].Length;
                    return update + offset + cc[i - 1].Length;
                }
            }while(line.Length > 1);
            return -1;
             * 
        }*/
        #endregion

        /// <summary>
        /// Goes through each string in lines to find the first string that matches comparison.
        /// </summary>
        /// <param name="lines">Array of Lines to check for a match with comparison</param>
        /// <param name="comparison">What would go in a LIKE statement</param>
        /// <returns>Index of First string in lines that matches comparison. Returns -1 if no match is found.</returns>
        public int Compare(string[] lines, string comparison)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                string s = lines[i];
                if (Compare(s, comparison))
                    return i;
            }
            return -1;
        }
        /// <summary>
        /// Performs Compare in the opposite order on the line.
        /// </summary>
        /// <param name="lines">Array of Lines to check for a match with comparison</param>
        /// <param name="comparison">What would go in a LIKE statement</param>
        /// <returns>Index of Last string in lines that matches comparison. Returns -1 if no match is found.</returns>
        public int ReverseCompare(string[] lines, string comparison)
        {
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string s = lines[i];
                if (Compare(s, comparison))
                    return i;
            }
            return -1;
        }
        public string Compare(IEnumerable<string> lines, string Comparison)
        {
            foreach(string l in lines)
            {
                if (Compare(l, Comparison))
                    return l;
            }
            return null;
        }
        /// <summary>
        /// Performs compare in the opposite order, returns the first string from the Enumerable that matches
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="Comparison"></param>
        /// <returns></returns>
        public string ReverseCompare(IEnumerable<string> lines, string Comparison)
        {
            foreach(string s in lines.Reverse())
            {
                if (Compare(s, Comparison))
                    return s;
            }
            return null;
        }
        /// <summary>
        /// Searches an array of strings to find all strings that match comparison
        /// </summary>
        /// <param name="lines">Array of strings to search</param>
        /// <param name="comparison">Comparison that can include the % wildcard</param>
        /// <returns>Array containing all strings that match comparison</returns>
        public string[] GetMatches(IEnumerable<string> lines, string comparison)
        {
            int valids = 0;
            string[] temp = new string[lines.Count()];
            foreach (string s in lines)
            {
                if (Compare(s, comparison))
                {
                    temp[valids] = s;
                    valids++;
                }

            }
            string[] result = new string[valids];
            for (int i = 0; i < valids; i++)
            {
                result[i] = temp[i];
            }
            return result;
        }

    }
}
