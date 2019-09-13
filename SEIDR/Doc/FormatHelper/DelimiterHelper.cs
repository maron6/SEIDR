using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.FormatHelper
{
    public class DelimiterHelper
    {
        public static List<string> SplitString(string content, char delimiter, out int startByte, out int remainingBytes, bool checkTextQualifier, MetaDataBase metaData, bool End, bool SkipEmpty)
            => SplitString(content, new string[] { delimiter.ToString() }, out startByte, out remainingBytes, checkTextQualifier, metaData, End, SkipEmpty);
        public static List<string> SplitString(string content, string delimiter, out int startByte, out int remainingBytes, bool checkTextQualifier, MetaDataBase metaData, bool End, bool SkipEmpty)
            => SplitString(content, new string[] { delimiter }, out startByte, out remainingBytes, checkTextQualifier, metaData, End, SkipEmpty);
        public static List<string> SplitString(string content, string[] delimiterList, out int startByte, out int remainingBytes, bool CheckTextQualifier, MetaDataBase metaData, bool End, bool SkipEmpty)
        {
            //string delim = metaData.Delimiter.ToString();
            /*
            string[] rowDelim;
            if (metaData.ReadWithMultiLineEndDelimiter)
                rowDelim = metaData.MultiLineEndDelimiter;
            else
                rowDelim = new string[] { metaData.LineEndDelimiter };  */
            string textQual = metaData.TextQualifier;
            List<string> result = new List<string>();
            int Position = 0;
            int nextTextQual = -1;            
            if (CheckTextQualifier && !string.IsNullOrEmpty(textQual))
            {
                nextTextQual = content.IndexOf(textQual);
                if (metaData.AllowQuoteEscape)
                {
                    while (nextTextQual > 0 && content[nextTextQual - 1] == metaData.QuoteEscape)
                    {
                        nextTextQual = content.IndexOf(textQual, nextTextQual + textQual.Length);
                    }
                }
            } /*
            ToDo: if !CheckTextQualifier || nextTextQual < 0 (really just nextTextQual < 0) then do a string.Split
            Needs handling for skip lines to get the starting byte and number of remaining bytes
            */
            int textQualCount = 0;
            int lastPositionEnd = 0;
            //int maxDelim = delimiterList.Max(del => del.Length);
            int nextRowDelim = content.IndexOfAny(delimiterList);
            startByte = 0;
            while (true)
            {
                if (nextTextQual == -1 || nextRowDelim < nextTextQual && textQualCount % 2 == 0)
                {
                    textQualCount = 0;
                    int fromPosition = Position;
                    string val = content.Substring(Position, nextRowDelim - Position);                    
                    if (delimiterList.Length == 1)
                    {
                        Position = nextRowDelim + delimiterList[0].Length;
                    }
                    else
                    {
                        foreach (var le in delimiterList)
                        {
                            if (nextRowDelim + le.Length < content.Length //If nextRowDelim >= 0, then the entire delimiter must have been found
                            && le == content.Substring(nextRowDelim, le.Length) )
                            {
                                Position = nextRowDelim + le.Length;
                                //emptyEnd = metaData.FileEncoding.GetByteCount(le);
                                break;
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(val) || !SkipEmpty)
                    {
                        if (result.Count == 0)
                        {
                            startByte = metaData.FileEncoding.GetByteCount(content.ToCharArray(0, fromPosition));
                            //First record being added - set starting offset based on where this string was started.
                        }
                        result.Add(val);
                        lastPositionEnd = nextRowDelim;
                    }
                    nextRowDelim = content.IndexOfAny(delimiterList, Position);
                    if (nextRowDelim < 0 || nextRowDelim == content.Length - 1)
                    {
                        if (End)
                        {
                            if (nextRowDelim >= 0)
                            {
                                val = content.Substring(Position, nextRowDelim - Position);
                                if (!SkipEmpty || nextRowDelim > Position)
                                {
                                    result.Add(val);
                                    lastPositionEnd = nextRowDelim;
                                }
                                break;   
                            }
                            val = content.Substring(Position);
                            if (!SkipEmpty || !string.IsNullOrEmpty(val))
                            {
                                result.Add(val);
                                lastPositionEnd = content.Length;
                            }                            
                        }
                        break;
                    }
                    continue;
                }
                textQualCount++;
                int quotePosition = nextTextQual + textQual.Length;
                nextTextQual = content.IndexOf(textQual, quotePosition);
                if (metaData.AllowQuoteEscape)
                {
                    while (nextTextQual > 0 && content[nextTextQual - 1] == metaData.QuoteEscape)
                    {
                        nextTextQual = content.IndexOf(textQual, nextTextQual + textQual.Length);
                    }
                }
                nextRowDelim = content.IndexOfAny(delimiterList, quotePosition);
                if(nextRowDelim < 0)
                {
                    if (End)
                    {
                        bool matched = false;
                        string val = null;
                        if (nextTextQual > 0 && textQualCount % 2 == 1)
                        {
                            //If we have an upcoming quote, and it's going to be the last quote, return the remainder of the string. 
                            // e.g., '" something | bla bla | "end of line.' - rather than erroring here, because the text qualifier goes to the end, we return the last string.
                            if (!metaData.AllowQuoteEscape && content.IndexOf(textQual, nextTextQual + textQual.Length) < 0)
                            {
                                val = content.Substring(Position);
                                matched = true;
                            }
                            else if (metaData.AllowQuoteEscape)
                            {
                                while (nextTextQual > 0 && content[nextTextQual - 1] == metaData.QuoteEscape)
                                {
                                    nextTextQual = content.IndexOf(textQual, nextTextQual + textQual.Length);
                                }//check that next non-escaped text qual is the last one. If so, then we can just end the string.
                                if (content.IndexOf(textQual, nextTextQual + textQual.Length) < 0)
                                {
                                    val = content.Substring(Position);
                                    matched = true;
                                }
                            }
                        }
                        else if(nextTextQual < 0 && textQualCount %2 == 0)
                        {
                            val = content.Substring(Position);
                            matched = true;
                        }
                        if(!matched)
                        {
                            string message = $"Missing Text Qualifier({textQual}) {(metaData.AllowQuoteEscape ? ", TextQualifier escape (" + metaData.QuoteEscape + ")," : string.Empty)} or delimiter ({ string.Join(",", delimiterList)}).";
                            throw new Exception(message);
                        }                        
                        if (!SkipEmpty || !string.IsNullOrEmpty(val))
                        { 
                            result.Add(val);
                            //emptyEnd = 0;
                        }
                    }
                    break;
                }
            }
            if (End)
            {
                if (SkipEmpty && lastPositionEnd < content.Length)
                {
                    //remainingBytes;
                    remainingBytes = metaData.FileEncoding.GetByteCount(content.ToCharArray(lastPositionEnd, content.Length - lastPositionEnd));
                }
                else
                    remainingBytes = 0;                
            }
            else
                remainingBytes = metaData.FileEncoding.GetByteCount(content.ToCharArray(lastPositionEnd, content.Length - lastPositionEnd));
            return result;
        }
        public static IEnumerable<string> EnumerateSplits(string content, string delimiter, bool CheckTextQualifier, MetaDataBase metaData, bool SkipEmpty)
            => EnumerateSplits(content, new string[] { delimiter }, CheckTextQualifier, metaData, SkipEmpty);
        public static IEnumerable<string> EnumerateSplits(string content, char delimiter, bool checkTextQualifier, MetaDataBase metaData, bool SkipEmpty)
            => EnumerateSplits(content, new string[] { delimiter.ToString() }, checkTextQualifier, metaData, SkipEmpty);
        public static IEnumerable<string> EnumerateSplits(string content, string[] delimiterList, bool CheckTextQualifier, MetaDataBase metaData, bool SkipEmpty)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            if (metaData == null)
                throw new ArgumentNullException(nameof(metaData));
            if (delimiterList == null)
                throw new ArgumentNullException(nameof(delimiterList));
            if (delimiterList.Length == 0)
                throw new ArgumentException("Delimiter list is empty or null", nameof(delimiterList));
            string textQual = metaData.TextQualifier;
            int Position = 0;
            int nextTextQual = -1;
            if (CheckTextQualifier && !string.IsNullOrEmpty(textQual))
            {
                nextTextQual = content.IndexOf(textQual, StringComparison.Ordinal);
                if (metaData.AllowQuoteEscape)
                {
                    while (nextTextQual > 0 && content[nextTextQual - 1] == metaData.QuoteEscape)
                    {
                        nextTextQual = content.IndexOf(textQual, nextTextQual + textQual.Length, StringComparison.Ordinal);
                    }
                }
            }
            if (nextTextQual < 0)
            { 
                //if no text qualifier logic to go through, should be faster to just run string.split
                //Don't have to worry about starting or ending byte here either.
                foreach (string section in content.Split(delimiterList,
                    SkipEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None))
                    yield return section;
                yield break;
            }

            int textQualCount = 0;
            int nextRowDelim = content.IndexOfAny(delimiterList);
            if(nextRowDelim < 0)
            {
                yield return content;
                /*If we originally had something like 'testSomething something\r\n' that only had a single delim, then when we get to here, the end position may have already removed 
                 * the delimiter that we would check for here otherwise. So in that case, just return the original string.
                */
                yield break;
            }
            while (true)
            {
                if (nextTextQual == -1 || nextRowDelim < nextTextQual && textQualCount % 2 == 0)
                {
                    textQualCount = 0;
                    string val = content.Substring(Position, nextRowDelim - Position);
                    if (!SkipEmpty || !string.IsNullOrEmpty(val))
                        yield return val;
                    if (delimiterList.Length == 1)
                        Position = nextRowDelim + delimiterList[0].Length;
                    else
                    {
                        foreach (var le in delimiterList)
                        {
                            if (le == content.Substring(nextRowDelim, le.Length))
                            {
                                Position = nextRowDelim + le.Length;
                                break;
                            }
                        }
                    }
                    nextRowDelim = content.IndexOfAny(delimiterList, Position);
                    if (nextRowDelim < 0)
                    {
                        val = content.Substring(Position);
                        if (!SkipEmpty || !string.IsNullOrEmpty(val))
                            yield return val;
                        break;
                    }
                    continue;
                }
                textQualCount++;
                int quotePosition = nextTextQual + textQual.Length;
                nextTextQual = content.IndexOf(textQual, quotePosition, StringComparison.Ordinal);
                if (metaData.AllowQuoteEscape)
                {
                    while (nextTextQual > 0 && content[nextTextQual - 1] == metaData.QuoteEscape)
                    {
                        nextTextQual = content.IndexOf(textQual, nextTextQual + textQual.Length, StringComparison.Ordinal);
                    }
                }
                nextRowDelim = content.IndexOfAny(delimiterList, quotePosition);
                if (nextRowDelim < 0|| nextRowDelim == content.Length - 1)
                {
                    int len = -1;
                    if (nextRowDelim >= 0)
                        len = nextRowDelim - Position;
                    if (nextTextQual > 0 && textQualCount % 2 == 1 )
                    {
                        //If we have an upcoming quote, and it's going to be the last quote, return the remainder of the string. 
                        // e.g., '" something | bla bla | "end of line.' - rather than erroring here, because the text qualifier goes to the end, we return the last string.
                        if (!metaData.AllowQuoteEscape && content.IndexOf(textQual, nextTextQual + textQual.Length, StringComparison.Ordinal) < 0)
                        {
                            if (len < 0)
                                yield return content.Substring(Position);
                            else
                                yield return content.Substring(Position, len);
                            break;
                        }
                        else if (metaData.AllowQuoteEscape)
                        {
                            while(nextTextQual > 0 && content[nextTextQual -1] == metaData.QuoteEscape)
                            {
                                nextTextQual = content.IndexOf(textQual, nextTextQual + textQual.Length, StringComparison.Ordinal);
                            }//check that next non-escaped text qual is the last one. If so, then we can just end the string.
                            if (content.IndexOf(textQual, nextTextQual + textQual.Length, StringComparison.Ordinal) < 0)
                            {
                                if (len < 0)
                                    yield return content.Substring(Position);
                                else
                                    yield return content.Substring(Position, len);
                                break;
                            }
                        }
                    }
                    else if(nextTextQual < 0 &&  textQualCount % 2 == 0)
                    {
                        if (len < 0)
                            yield return content.Substring(Position);
                        else
                            yield return content.Substring(Position, len);
                        break; //we just got past the last quote. Ending line.
                    }
                    string message = $"Missing Text Qualifier({textQual}) {(metaData.AllowQuoteEscape ? ", TextQualifier escape (" + metaData.QuoteEscape + ")," : string.Empty)} or delimiter ({ string.Join(",", delimiterList)}).";                    
                    throw new Exception(message);
                    /*
                    if(!SkipEmpty || Position < content.Length)
                        yield return content.Substring(Position);
                    break;
                    */
                }
            }
            yield break;
        }
        /// <summary>
        /// Infer column information for a delimited file
        /// </summary>
        /// <param name="ContentLine"></param>
        /// <param name="metaData"></param>
        /// <param name="CheckTextQualifier"></param>
        /// <param name="startPosition"></param>
        /// <returns></returns>
        public static List<DocRecordColumnInfo> InferColumnList(string ContentLine, MetaDataBase metaData, bool CheckTextQualifier, ref int startPosition)
        {
            if (metaData?.Delimiter == null)
                throw new ArgumentException("Meta data null or missing delimiter.", nameof(metaData));
            var valSet = EnumerateSplits(ContentLine, metaData.Delimiter.Value, CheckTextQualifier, metaData, false);
            List<DocRecordColumnInfo> colSet = new List<DocRecordColumnInfo>();
            int idx = 0;                        
            foreach(var val in valSet)
            {                
                string colName;
                if (metaData.HasHeader)
                {
                    colName = val;
                    startPosition += metaData.FileEncoding.GetByteCount(val);
                }
                else
                    colName = "COLUMN # " + idx;
                colSet.Add(new DocRecordColumnInfo(colName, metaData.Alias, idx++));
                
            }
            if (metaData.HasHeader)
            {
                startPosition += (colSet.Count - 1) * metaData.FileEncoding.GetByteCount(metaData.Delimiter.ToString());
                //2 columns = Col1|Col2 -> delimiter count = colcount - 1.
            }
            return colSet;
        }

        /// <summary>
        /// Get starting byte position after skipping X lines.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="delimiterList"></param>
        /// <param name="CheckTextQualifier"></param>
        /// <param name="metaData"></param>
        /// <param name="skipLines"></param>
        /// <param name="skipEmpty"></param>
        /// <returns></returns>
        public static int GetSkipPosition(string content, string[] delimiterList, bool CheckTextQualifier, MetaDataBase metaData, int skipLines, bool skipEmpty = true)
        {
            if (skipLines == 0)
                return 0;
            Encoding encode = metaData.FileEncoding;
            int position = 0, lineCounter = 0;
            string textQual = metaData.TextQualifier;
            int nextTextQual = -1;
            if (CheckTextQualifier && !string.IsNullOrEmpty(textQual))
                nextTextQual = content.IndexOf(textQual, StringComparison.Ordinal);
            int textQualCount = 0;
            int nextRowDelim = content.IndexOfAny(delimiterList);

            while (lineCounter < skipLines)
            {
                if (nextTextQual == -1 || nextRowDelim < nextTextQual && textQualCount % 2 == 0)
                {
                    textQualCount = 0;               
                    if (!skipEmpty || nextRowDelim - position > 0)                    
                        lineCounter++;                     
                    if (delimiterList.Length == 1)
                        position = nextRowDelim + delimiterList[0].Length;
                    else
                    {
                        foreach (string le in delimiterList)
                        {
                            if (le == content.Substring(nextRowDelim, le.Length))
                            {
                                position = nextRowDelim + le.Length;
                                break;
                            }
                        }
                    }
                    nextRowDelim = content.IndexOfAny(delimiterList, position);                    
                    if (nextRowDelim < 0)
                    {
                        throw new Exception("Unexpected end while determining start position.");
                    }
                }
                else
                {
                    textQualCount++;
                    int quotePosition = nextTextQual + textQual.Length;
                    nextTextQual = content.IndexOf(textQual, quotePosition, StringComparison.Ordinal);
                    nextRowDelim = content.IndexOfAny(delimiterList, quotePosition);
                }
            }
            return encode.GetByteCount(content.ToCharArray(0, position)); //ending position is where we want to start up again.
        }
    }
}
