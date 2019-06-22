using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.FormatHelper
{
    public class DelimiterHelper
    {
        public static List<string> SplitString(string content, char delimiter, out int remainingBytes, bool checkTextQualifier, MetaDataBase metaData, bool End)
            => SplitString(content, new string[] { delimiter.ToString() }, out remainingBytes, checkTextQualifier, metaData, End);
        public static List<string> SplitString(string content, string delimiter, out int remainingBytes, bool checkTextQualifier, MetaDataBase metaData, bool End)
            => SplitString(content, new string[] { delimiter }, out remainingBytes, checkTextQualifier, metaData, End);
        public static List<string> SplitString(string content, string[] delimiterList, out int remainingBytes, bool CheckTextQualifier, MetaDataBase metaData, bool End)
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
            if (CheckTextQualifier)
                nextTextQual = content.IndexOf(textQual);
            int textQualCount = 0;
            int nextRowDelim = content.IndexOfAny(delimiterList);
            while (true)
            {
                if (nextTextQual == -1 || nextRowDelim < nextTextQual && textQualCount % 2 == 0)
                {
                    textQualCount = 0;
                    string val = content.Substring(Position, nextRowDelim - Position);
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
                    result.Add(val);
                    nextRowDelim = content.IndexOfAny(delimiterList, Position);
                    if (nextRowDelim < 0)
                    {
                        if (End)
                            result.Add(content.Substring(Position));
                        break;
                    }
                    continue;
                }
                textQualCount++;
                int quotePosition = nextTextQual + textQual.Length;
                nextTextQual = content.IndexOf(textQual, quotePosition);
                nextRowDelim = content.IndexOfAny(delimiterList, quotePosition);
                if(nextRowDelim < 0)
                {
                    if (End)
                        result.Add(content.Substring(Position));
                    break;
                }
            }
            if (End)
                remainingBytes = 0;
            else
                remainingBytes = metaData.FileEncoding.GetByteCount(content.ToCharArray(Position, content.Length - Position));
            return result;
        }
        public static IEnumerable<string> EnumerateLines(string content, string delimiterList, bool CheckTextQualifier, MetaDataBase metaData)
            => EnumerateLines(content, new string[] { delimiterList }, CheckTextQualifier, metaData);
        public static IEnumerable<string> EnumerateLines(string content, char delimiter, bool checkTextQualifier, MetaDataBase metaData)
            => EnumerateLines(content, new string[] { delimiter.ToString() }, checkTextQualifier, metaData);
        public static IEnumerable<string> EnumerateLines(string content, string[] delimiterList, bool CheckTextQualifier, MetaDataBase metaData)
        {
            string textQual = metaData.TextQualifier;
            int Position = 0;
            int nextTextQual = -1;
            if (CheckTextQualifier)
                nextTextQual = content.IndexOf(textQual);
            int textQualCount = 0;
            int nextRowDelim = content.IndexOfAny(delimiterList);
            while (true)
            {
                if (nextTextQual == -1 || nextRowDelim < nextTextQual && textQualCount % 2 == 0)
                {
                    textQualCount = 0;
                    yield return content.Substring(Position, nextRowDelim - Position);
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
                        yield return content.Substring(Position);
                        break;
                    }
                    continue;
                }
                textQualCount++;
                int quotePosition = nextTextQual + textQual.Length;
                nextTextQual = content.IndexOf(textQual, quotePosition);
                nextRowDelim = content.IndexOfAny(delimiterList, quotePosition);
                if (nextRowDelim < 0)
                {
                    yield return content.Substring(Position);
                    break;
                }
            }
            yield break;
        }
    }
}
