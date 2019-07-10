using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.FormatHelper
{
    public static class FixWidthHelper
    {
        /// <summary>
        /// Attempt to infer a column set from the provided set of lines from a source file.
        /// <para>Could use as a starting point, export to a delimited file, manually correct, and then import the column information using <see cref="Doc.DocRecordColumnCollection.ParseFromIRecordSet(string, IEnumerable{IDataRecord})"/>
        /// </para><para>Note that a DocReader would fit the second parameter.</para>
        /// </summary>
        /// <param name="lineSource"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        public static List<DocRecordColumnInfo> InferColumnset(IList<string> lineSource, MetaDataBase metaData)
        {
            var ret = new List<DocRecordColumnInfo>();
            /*
            Need to check a few lines 
            Rules:
             if At least 3 spaces - Probably a new column.
             If starting point inconsistent - right justify and end at consistent spot.
             
             */
            //Initial version: very simple guessing - 3 space check for one line.
            string header = lineSource[0];
            int spaceCounter = 0, columnLen = 0;
            StringBuilder colBuild = new StringBuilder();
            foreach(char c in header)
            {
                if (char.IsWhiteSpace(c))
                {
                    spaceCounter++;
                    columnLen++;                    
                }
                else 
                {
                    if (spaceCounter >= 3)
                    {
                        var col = new DocRecordColumnInfo(colBuild.ToString());
                        col.MaxLength = columnLen;
                        ret.Add(col);
                        colBuild.Clear();
                        columnLen = 1;
                    }
                    else
                        columnLen++;

                    colBuild.Append(c);                    
                    spaceCounter = 0;
                }
            }
            return ret;
        }
        public static List<string> SplitStringByLength(string contentSource, MetaDataBase metaData, out int remainingBytes)
        {
            var columnInfos = metaData.GetRecordColumnInfos(contentSource);            
            int len = columnInfos.MaxLength;
            if (len < 0)
                throw new ArgumentOutOfRangeException(nameof(columnInfos), "Unable to split by length - column lengths not fully configured.");
            int Position = 0;
            var count = contentSource.Length / len;
            List<string> result = new List<string>();
            for(int i = 0; i < count; i++)
            {
                result.Add(contentSource.Substring(Position, len));
                Position += len;
            }            
            remainingBytes = metaData.FileEncoding.GetByteCount(contentSource.ToCharArray(), Position, contentSource.Length - Position);
            return result;
        }
        public static IEnumerable<string> SplitStringByLength(string contentSource, MetaDataBase metaData)
        {
            var columnInfos = metaData.GetRecordColumnInfos(contentSource);
            int len = columnInfos.MaxLength;
            if (len < 0)
                throw new ArgumentOutOfRangeException(nameof(columnInfos), "Unable to split by length - column lengths not fully configured.");
            int Position = 0;
            var count = contentSource.Length / len;
            List<string> result = new List<string>();
            for (int i = 0; i < count; i++)
            {
                yield return contentSource.Substring(Position, len);
                Position += len;
            }
            yield break;
        }
    }
}
