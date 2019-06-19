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
        const byte OFFSET = 2;                
        //const int STRING_CUTOFF = (byte.MaxValue >> OFFSET) << (BITS_PER_BYTE * 2);
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
        public static string GetValue(byte[] input, ref int Position, out DocRecordColumnType format, Encoding ReadEncoding, string SubDelimiter = null)
        {
            byte type = input[Position++];
            bool arr = false;
            if ((type & 0b1000_0000) != 0)
            {
                arr = true;
                type &= 0b0111_1111; //remove array flag.
                if (SubDelimiter == null)
                    SubDelimiter = "*"; //Decode type
            }
            format = (DocRecordColumnType)(type >> OFFSET);
            if (arr)
                return string.Join(SubDelimiter, DecodeArray(input, ref Position, format, ReadEncoding));
            else
                return DecodeItem(input, ref Position, format, ReadEncoding, type);
            //int byteL = 0;
            /*
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
            return result;*/
        }

        static string[] DecodeArray(byte[] input, ref int Position, DocRecordColumnType format, Encoding ReadEncoding)
            //where V:class //Must be nullable. E.g., string, DateTime?, int?
        {
            //check length
            int l = BitConverter.ToInt32(input, Position);
            Position += sizeof(int);
            //V[] vs = new V[l];
            string[] vs = new string[l]; //results are strings for the 
            for(int i = 0; i < Position; i++)
            {

            }
            return vs;
        }
        static string DecodeItem(byte[] input, ref int Position, DocRecordColumnType format, Encoding ReadEncoding, byte TypeHeader) //where V: class
        {
            //V result = default;
            string result;
            int byteL;//number of records to move forward
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
                    result = ((TypeHeader & 1) == 1) ? "TRUE" : "FALSE";
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
                    byteL = (int)TypeHeader & 3; //Last two bytes - will be a value between 00 and 11 (0-3), indicating i + 1 bytes to int (number of characters to read from bytes as a string)
                    int L = input[Position++]; //0 - just a single byte.
                    for(int i = 1; i <= byteL; i++)
                    {
                        L |= input[Position++] << i;
                    }                    
                    result = ReadEncoding.GetString(input, Position, L);
                    byteL = L;
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
        static void EncodeArray<T>(DocRecordColumnType encodeType, object value, List<byte> work, Encoding WriteEncoding)
        {            
            if (value == null || string.IsNullOrEmpty(value.ToString()) || !(value is T))
            {
                work.Add((int)DocRecordColumnType.NUL << OFFSET);
                return;
            }

            byte type = (byte)((int)encodeType << OFFSET);            
            type = (byte)(0b1000_0000 | type);
            work.Add(type);
            IList<T> data = (IList<T>)value;
            work.AddRange(BitConverter.GetBytes(data.Count));
            foreach (T item in data)
            {
                EncodeType(encodeType, item, work, WriteEncoding);
            }
        }
        static void EncodeType(DocRecordColumnType encodeType, object value, List<byte> work, Encoding WriteEncoding)
        {

            if (value == null || string.IsNullOrEmpty(value.ToString()))
            {
                work.Add((int)DocRecordColumnType.NUL << OFFSET);
                return;
            }
            byte type = (byte)((int)encodeType << OFFSET);
            switch (encodeType)
            {
                case DocRecordColumnType.Unknown:
                case DocRecordColumnType.Varchar:
                case DocRecordColumnType.NVarchar:
                case DocRecordColumnType.Money:
                case DocRecordColumnType.Decimal:
                    {
                        var data = WriteEncoding.GetBytes(value.ToString());
                        var len = data.Length;
                        var bl = BitConverter.GetBytes(len);
                        if (bl[3] == 0)
                        {
                            if (bl[2] == 0)
                            {
                                if (bl[1] == 0)
                                {
                                    //Length should be at least one byte (bl[0] != 0), or else we would have been in the forced NUL above.
                                    work.AddRange(type, bl[0]);
                                }
                                else
                                {
                                    type = (byte)(type | 1);
                                    work.AddRange(type, bl[0], bl[1]);
                                }
                            }
                            else
                            {
                                type = (byte)(type | 2);
                                work.AddRange(type, bl[0], bl[1], bl[2]);
                            }
                        }
                        else
                        {
                            type = (byte)(type | 3);
                            work.Add(type);
                            work.AddRange(bl);
                        }
                        work.AddRange(data);
                        break;
                    }
                case DocRecordColumnType.Bool:                    
                        if ((bool)value)
                            type = (byte)(type | 1);
                        work.Add(type);
                    break;
                default:                    
                    {
                        work.Add(type);
                        /*
                         * Note: if for some reason, DataType is set to NUL, it's not going to hit either of the  IF conditions above.
                         * Also would not hit any of the cases below, so would just have the nul type, as expected above.
                        */
                        switch (encodeType)
                        {
                            case DocRecordColumnType.Tinyint:
                                work.Add((byte)value);
                                break;
                            case DocRecordColumnType.Int:
                                work.AddRange(BitConverter.GetBytes((int)value));
                                break;
                            case DocRecordColumnType.Smallint:
                                work.AddRange(BitConverter.GetBytes((short)value));
                                break;
                            case DocRecordColumnType.Bigint:
                                work.AddRange(BitConverter.GetBytes((long)value));
                                break;
                            case DocRecordColumnType.Double:
                                work.AddRange(BitConverter.GetBytes((double)value));
                                break;
                            case DocRecordColumnType.Date:
                            case DocRecordColumnType.DateTime:
                                double d = ((DateTime)value).ToOADate();
                                work.AddRange(BitConverter.GetBytes(d));
                                break;
                        }
                        break;
                    }
            }
        }
        public static string SetResult(DocRecordColumnInfo column, object value, Encoding WriteEncoding)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
            {
                return WriteEncoding.GetString(new byte[] { (int)DocRecordColumnType.NUL << OFFSET });
            }
            List<byte> result = new List<byte>();
            //byte type = (byte)((int)column.DataType << OFFSET);
            if (column.Array)
            {
                //type = (byte)(0b1000_0000 | type);
                //value should be an IList whose typ corresponds to this...need to get length and then individual values.
                //ToDo: Encode array - type to use depends on column data type, though.
                switch (column.DataType)
                {
                    case DocRecordColumnType.Unknown:
                    case DocRecordColumnType.Varchar:
                    case DocRecordColumnType.NVarchar:
                        EncodeArray<string>(column.DataType, value, result, WriteEncoding);
                        break;
                    case DocRecordColumnType.Money:
                    case DocRecordColumnType.Decimal:
                        EncodeArray<decimal?>(column.DataType, value, result, WriteEncoding);
                        break;
                    case DocRecordColumnType.Bool:
                        EncodeArray<bool?>(column.DataType, value, result, WriteEncoding);
                        break;
                    case DocRecordColumnType.Int:
                        EncodeArray<int?>(column.DataType, value, result, WriteEncoding);
                        break;
                }
            }
            else
            {
                EncodeType(column.DataType, value, result, WriteEncoding);
            }
            return WriteEncoding.GetString(result.ToArray());
            /*
            if(column.DataType.In(DocRecordColumnType.Unknown, 
                DocRecordColumnType.Varchar, DocRecordColumnType.NVarchar, 
                DocRecordColumnType.Money, DocRecordColumnType.Decimal)) //Treat decimal/Money as string to prevent loss of precision from converting to floating point
            {
                var data = WriteEncoding.GetBytes(value.ToString());
                var len = data.Length;
                var bl = BitConverter.GetBytes(len);
                if (bl[3] == 0)
                {
                    if (bl[2] == 0)
                    {
                        if (bl[1] == 0)
                        {
                            //Length should be at least one byte (bl[0] != 0), or else we would have been in the forced NUL above.
                            result.AddRange(type, bl[0]);
                        }
                        else
                        {
                            type = (byte)(type | 1);
                            result.AddRange(type, bl[0], bl[1]);
                        }
                    }
                    else
                    {
                        type = (byte)(type | 2);
                        result.AddRange(type, bl[0], bl[1], bl[2]);
                    }
                }
                else
                {
                    type = (byte)(type | 3);
                    result.Add(type);
                    result.AddRange(bl);
                }
                                
            }
            else if(column.DataType == DocRecordColumnType.Bool)
            {
                if ((bool)value)
                    type = (byte)(type | 1);
                result.Add(type);
            }
            else
            {                               
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
        
            return WriteEncoding.GetString(result.ToArray());*/
        }
        public static bool CheckValue(byte[] input, int Position)
        {
            int x = Position;
            return CheckValue(input, ref x);
        }
        static bool CheckItem(byte[] input, ref int Position, DocRecordColumnType format, byte TypeHeader)
        {
            int byteL;//number of records to move forward
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
                    byteL = TypeHeader & 3;
                    if (Position + 1 >= input.Length)
                        return false;
                    int L = input[Position++]; //0 - just a single byte.
                    for (int i = 1; i <= byteL; i++)
                    {
                        if (Position + 1 >= input.Length)
                            return false;
                        L |= input[Position++] << i;
                    }
                    byteL = L;
                    break;
                default:
                case DocRecordColumnType.NUL:
                    byteL = 0;
                    break;
            }
            Position += byteL;
            return Position < input.Length;
        }
        /// <summary>
        /// Check that the input has the full value of the next item based on starting position.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="Position"></param>
        /// <returns></returns>
        public static bool CheckValue(byte[] input, ref int Position)
        {            
            if (Position + 1 > input.Length)
                return false;
            byte type = input[Position++];
            bool arr = false;
            if ((type & 0b1000_0000) != 0)
            {
                arr = true;
                type &= 0b0111_1111; //remove array flag.
            }
            var format = (DocRecordColumnType)(type >> OFFSET);
            if (format == DocRecordColumnType.NUL || format == DocRecordColumnType.Bool) //no more bytes for this value.
                return true;
            //int byteL;
            if (arr)
            {
                if (Position + sizeof(int) >= input.Length)
                    return false;
                int colCount = BitConverter.ToInt32(input, Position);
                Position += sizeof(int);
                if (Position + 1 >= input.Length)
                    return false;
                for(int subcol = 0; subcol < colCount; subcol++)
                {
                    var subformat = input[Position++];
                    if (subformat == 0) //null
                        continue;                        
                    if (Position == input.Length)
                        return false;
                    if (!CheckItem(input, ref Position, format, subformat))
                        return false;
                }
                return true;
            }
            return CheckItem(input, ref Position, format, type);
            /*
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
            */
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
        public static List<string> SplitString(string content, MetaDataBase metaData, out int RemainingBytesFromSplit)
        {
            List<string> result = new List<string>();
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
                for (int i = 1; i < colSet.Columns.Count; i++)
                {
#if DEBUG
                    int test = position;
                    string v = GetValue(byteSet, ref test, out _, fileEncoding);
                    System.Diagnostics.Debug.WriteLine(v);
#endif
                    if (!CheckValue(byteSet, ref position))
                    {
                        RemainingBytesFromSplit = positionFrom;
                        return result;
                    }
                }
                result.Add( fileEncoding.GetString(byteSet, positionFrom, position - positionFrom));
                positionFrom = position;
                if (CheckValue(byteSet, position))
                {
                    k = GetValue(byteSet, ref position, out _, fileEncoding);
                    colSet = metaData.GetRecordColumnInfos(k);
                }
                else
                    return result;
            }
        }
    }
}
