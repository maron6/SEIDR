using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SEIDR.META
{
    /// <summary>
    /// Separates records into tokens. 
    /// <para>Note(Special Tokens): colon, commas, semicolon, period, brackets, parenthesis are considered their own tokens by default.
    /// </para>
    /// <para>Note that this is an IEnumerable so you can either use the peek/GetNext, or you can simply use the tokenizer in a foreach loop.</para>
    /// </summary>
    public class Tokenizer : IEnumerable<string>
    {
        /// <summary>
        /// Merges the following tokens until reaching a 'tokenUntil' token.
        /// </summary>
        /// <param name="combineString"></param>
        /// <param name="tokenUntil"></param>
        /// <param name="appendToken">Adds the combine until token to the CombineString</param>
        public void MergeUntil(ref string combineString, string tokenUntil, bool appendToken = false)
        {
            string x = GetNextToken();
            while(x != tokenUntil)
            {
                combineString += x;
                x = GetNextToken();
            }
            if (appendToken)
                combineString += x;
        }        
        int Counter;
        string[] TokenList;
        /// <summary>
        /// Creates an instance ofa Tokenizer that separates the string into tokens based on spaces. Also separates special characters into their own token
        /// </summary>
        /// <param name="content"></param>
        public Tokenizer(string content)
        {
            TokenList = GetTokens(content);
            Counter = 0;
        }
        /// <summary>
        /// Creates an instance of a Tokenizer but overrides the list of default characters to treat as tokens.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="includeDefaultSpecialTokens">If true, will include the default special tokens (,.[];</param>
        /// <param name="specialTokenList"></param>
        public Tokenizer(string content, bool includeDefaultSpecialTokens, params char[] specialTokenList)
        {
            TokenList = GetTokens(content, specialTokenList, includeDefaultSpecialTokens);
            Counter = 0;
        }
        /// <summary>
        /// Surrounds default special token characters with spaces so that they are separated into their own spot when splitting by space
        /// </summary>
        /// <param name="temp"></param>
        /// <returns></returns>
        private static string HandleDefaultSpecialTokens(string temp)
        {
            return temp.Replace(";", " ; ").Replace(".", " . ").Replace(",", " , ").Replace("[", " [ ").Replace("]", " ] ")
                .Replace(")", " ) ").Replace("(", " ( ").Replace(":", " : "); //Surround these with spaces so they become their own tokens
        }
        private static string CleanNewlines(string temp)
        {
            return temp.Replace("\f", "").Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", " \n ").Replace("\n", Environment.NewLine);
        }
        /// <summary>
        /// Takes the passed string and extracts a list of tokens from it
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string[] GetTokens(string s)
        { 
            s = HandleDefaultSpecialTokens(s);
            s = CleanNewlines(s);
            s = Regex.Replace(s, @"\s+", @"\s");
            return s.Split(' ');
        }
        /// <summary>
        /// Takes teh passed content string and extracts a lsit of tokens from it.
        /// <para>Also treats the passed list of characters as special tokens.</para>
        /// </summary>
        /// <param name="s"></param>
        /// <param name="specialTokens"></param>
        /// <param name="includeDefaultSpecialTokens">If true, will continue to treat the default special tokens as special tokens (see class description)       
        /// </param>
        /// <returns></returns>
        public static string[] GetTokens(string s, char[] specialTokens, bool includeDefaultSpecialTokens = false)
        {
            if (includeDefaultSpecialTokens)            
                s = HandleDefaultSpecialTokens(s);            
            foreach (char x in specialTokens)
            {
                s = s.Replace(x.ToString(), $" {x} "); //Surround with spaces to tokenize
            }
            s = CleanNewlines(s);
            s = Regex.Replace(s, @"\s+", @"\s"); //Combine multiple spaces together
            return s.Split(' ');
        }
        /// <summary>
        /// Gets the next token
        /// </summary>
        /// <returns></returns>
        public string GetNextToken()
        {           
            return TokenList[Counter++];
        }
        /// <summary>
        /// Check if there are more tokens to look at
        /// </summary>
        public bool HasMoreTokens => Counter < TokenList.Length;
        public bool Peek(out string Token)
        {
            Token = null;
            if(Counter < TokenList.Length)
            {
                Token = TokenList[Counter];
                return true;
            }
            return false;
        }
        public IEnumerator<string> GetEnumerator()
        {
            return ((IEnumerable<string>)TokenList).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<string>)TokenList).GetEnumerator();
        }
    }
}
