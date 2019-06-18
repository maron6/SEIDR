using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    public class BitCONHelper
    {
        const int BITS_PER_BYTE = 8;
        const byte OFFSET = 4;                
        const int STRING_CUTOFF = (byte.MaxValue >> OFFSET) << (BITS_PER_BYTE * 2);
        public static string GetKey(string record, Encoding encoding)
        {
            var byteSet = encoding.GetBytes(record);
            int pos = 0;
            return GetValue(byteSet, ref pos, out _, encoding);
        }
        public static string GetKey(string record, Encoding encoding, ref int position)
        {
            var byteSet = encoding.GetBytes(record);            
            return GetValue(byteSet, ref position, out _, encoding);
        }
        public static string GetValue(byte[] input, ref int Position, out DocRecordColumnType format, Encoding ReadEncoding)
        {
            byte type = input[Position++];            
            format = (DocRecordColumnType)(type >> OFFSET);
            int byteL = 0;
            string result;
            switch (format)
            {
                case DocRecordColumnType.Bigint:
                    result = BitConverter.ToInt64(input, Position).ToString();
                    byteL = sizeof(long);                    
                    break;
                case DocRecordColumnType.Int:
                    byteL = sizeof(int);
                    result = BitConverter.ToInt32(input, Position).ToString();
                    break;
                case DocRecordColumnType.Smallint:
                    byteL = sizeof(short);
                    result = BitConverter.ToInt16(input, Position).ToString();
                    break;
                case DocRecordColumnType.Tinyint:
                    byteL = sizeof(byte);
                    result = input[Position].ToString();
                    break;
                case DocRecordColumnType.Bool:
                    byteL = 0;
                    result = ((type & 1) == 1) ? "TRUE": "FALSE";
                    break;                
                case DocRecordColumnType.DateTime:
                case DocRecordColumnType.Date:
                case DocRecordColumnType.Double:
                    byteL = sizeof(double);
                    var dbl = BitConverter.ToDouble(input, Position);
                    if (format == DocRecordColumnType.Date)                    
                        result = DateTime.FromOADate(dbl).ToString("yyyyMMdd");                    
                    else if (format == DocRecordColumnType.DateTime)                    
                        result = DateTime.FromOADate(dbl).ToString("yyyyMMddHHmmss");                    
                    else
                        result = dbl.ToString();
                    break;
                case DocRecordColumnType.Unknown:
                case DocRecordColumnType.Varchar:
                case DocRecordColumnType.NVarchar:
                case DocRecordColumnType.Decimal:
                case DocRecordColumnType.Money:    
                    byteL =  (int)format ^ type;
                    byteL <<= BITS_PER_BYTE * 2;
                    byteL = byteL & input[Position++];
                    byteL = byteL & input[Position++];
                    result = ReadEncoding.GetString(input, Position, byteL);
                    break;
                default:
                case DocRecordColumnType.NUL:
                    byteL = 0;
                    result = null;
                    break;
            }       
            Position += byteL;
            return result;
        }
        public static string SetResult(DocRecordColumnInfo column, object value, System.Text.Encoding WriteEncoding)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
            {
                return WriteEncoding.GetString(new byte[] { (int)DocRecordColumnType.NUL << OFFSET });
            }
            List<byte> result = new List<byte>();
            byte type = (byte)((int)column.DataType << OFFSET);                
            if(column.DataType.In(DocRecordColumnType.Unknown, 
                DocRecordColumnType.Varchar, DocRecordColumnType.NVarchar, 
                DocRecordColumnType.Money, DocRecordColumnType.Decimal)) //Treat decimal/Money as string to prevent loss of precision from converting to floating point
            {
                var data = WriteEncoding.GetBytes(value.ToString());
                var len = data.Length;                
                result.Add((byte)(type & ((len & STRING_CUTOFF) >> (BITS_PER_BYTE * 2)))); //Need to redo math for this...
                result.Add((byte)(len & (byte.MaxValue << BITS_PER_BYTE) >> BITS_PER_BYTE));
                result.Add((byte)(len & byte.MaxValue));

            }
            else if(column.DataType == DocRecordColumnType.Bool)
            {
                if ((bool)value)
                    type = (byte)(type | 1);
                result.Add(type);
            }
            else
            {                
                result.Add(type); 
                /*
                 * Note: if for some reason, DataType is set to NUL, it's not going to hit either of the  IF conditions above.
                 * Also would not hit any of the cases below, so would just have the nul type, as expected above.
                */
                switch (column.DataType)
                {
                    case DocRecordColumnType.Tinyint:
                        result.Add((byte)value);
                        break;
                    case DocRecordColumnType.Int:
                        result.AddRange(BitConverter.GetBytes((int)value));
                        break;
                    case DocRecordColumnType.Smallint:
                        result.AddRange(BitConverter.GetBytes((short)value));
                        break;
                    case DocRecordColumnType.Bigint:
                        result.AddRange(BitConverter.GetBytes((long)value));
                        break;
                    case DocRecordColumnType.Double:                       
                        result.AddRange(BitConverter.GetBytes((double)value));                        
                        break;                        
                    case DocRecordColumnType.Date:
                    case DocRecordColumnType.DateTime:
                        double d = ((DateTime)value).ToOADate();
                        result.AddRange(BitConverter.GetBytes(d));
                        break;                        
                }
            }
            return WriteEncoding.GetString(result.ToArray());
        }
        public static bool CheckValue(byte[] input, int Position)
        {
            int x = Position;
            return CheckValue(input, ref x);
        }
        /// <summary>
        /// Check that the input has the full value of the next item based on starting position.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="Position"></param>
        /// <returns></returns>
        public static bool CheckValue(byte[] input, ref int Position)
        {
            byte type = input[Position++];
            var format = (DocRecordColumnType)(type >> OFFSET);
            int byteL;
            switch (format)
            {
                case DocRecordColumnType.Bigint:
                    byteL = sizeof(long);
                    break;
                case DocRecordColumnType.Int:
                    byteL = sizeof(int);
                    break;
                case DocRecordColumnType.Smallint:
                    byteL = sizeof(short);
                    break;
                case DocRecordColumnType.Tinyint:
                    byteL = sizeof(byte);
                    break;
                case DocRecordColumnType.Bool:
                    byteL = 0;
                    break;
                case DocRecordColumnType.DateTime:
                case DocRecordColumnType.Date:
                case DocRecordColumnType.Double:
                    byteL = sizeof(double);
                    break;
                case DocRecordColumnType.Unknown:
                case DocRecordColumnType.Varchar:
                case DocRecordColumnType.NVarchar:
                case DocRecordColumnType.Decimal:
                case DocRecordColumnType.Money:
                    byteL = (int)format ^ type;
                    byteL <<= BITS_PER_BYTE * 2;
                    byteL = byteL & input[Position++];
                    byteL = byteL & input[Position++];                    
                    break;
                default:
                case DocRecordColumnType.NUL:
                    byteL = 0;
                    break;
            }
            Position += byteL;
            return Position > input.Length; //Note: equal should be okay
        }
        /// <summary>
        /// Indicates how many bytes were left are going through the values in content in the last instance method call.
        /// </summary>
        public int RemainingBytesFromSplit { get; private set; } = 0;
        /// <summary>
        /// Takes a string and splits it into a set of strings.
        /// <para>NOTE: because of metadata, we'll know if the last line is finished, and will simply not include the last line if it's not finished.</para>
        /// <para>Check <see cref="RemainingBytesFromSplit"/> after calling this method to see if anything was left off the end</para>
        /// </summary>
        /// <param name="content"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        public IEnumerable<string> SplitString(string content, MetaDataBase metaData)
        {
            RemainingBytesFromSplit = 0;
            int position = 0;
            int positionFrom = 0;
            Encoding fileEncoding = metaData.FileEncoding;
            var byteSet = fileEncoding.GetBytes(content);
            var k = //GetKey(content, fileEncoding, ref position);
                GetValue(byteSet, ref position, out _, fileEncoding);
            var colSet = metaData.GetRecordColumnInfos(k);
            while (true)
            {
                for(int i = 1; i < colSet.Columns.Count; i++)
                {                    
                    if(!CheckValue(byteSet, ref position))
                    {
                        RemainingBytesFromSplit = positionFrom;
                        yield break;
                    }                    
                }
                yield return fileEncoding.GetString(byteSet, positionFrom, position - positionFrom);
                positionFrom = position;
                if (CheckValue(byteSet, position))
                {
                    k = GetValue(byteSet, ref position, out _, fileEncoding);
                    colSet = metaData.GetRecordColumnInfos(k);
                }
                else yield break;
            }
        }
    }
}
