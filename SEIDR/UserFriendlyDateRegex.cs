using System;
using System.Text.RegularExpressions;

namespace SEIDR
{
    /// <summary>
    /// Date Regex logic for simple conversion of friendly date regex to replace the information with the provided date information
    /// </summary>
    public static class UserFriendlyDateRegex
    {
        /// <summary>
        /// Used for evaluating a style of user friendly date variable, where month, day, and/or year are represented by letters within ankle brackets.        
        /// <para>Might be useful somewhere. Does not handle offsets</para>
        /// </summary>
        /// <param name="s"></param>
        /// <param name="Evaluation">Use current date if not provided</param>
        /// <returns></returns>
        public static string Eval(string s, DateTime? Evaluation = null)
        {
            DateTime nu = Evaluation ?? DateTime.Now; //so that they all safely have the same datetime.
            s = Regex.Replace(s, "<[Mm]>", nu.Month.ToString());
            s = Regex.Replace(s, "<[Mm][Mm]>", nu.ToString("MM"));
            s = Regex.Replace(s, "<[Dd]>", nu.Day.ToString());
            s = Regex.Replace(s, "<[Dd][Dd]>", nu.ToString("dd"));
            s = Regex.Replace(s, "<[Yy][Yy]>", nu.ToString("yy"));
            s = Regex.Replace(s, "<[Cc][Cc]>", nu.Year.ToString().Substring(0, 2));
            s = Regex.Replace(s, "<[Yy][Yy][Yy][Yy]>", nu.Year.ToString());
            s = Regex.Replace(s, "<[Cc][Cc][Yy][Yy]>", nu.Year.ToString());
            
            return s;
        }
    }
}
