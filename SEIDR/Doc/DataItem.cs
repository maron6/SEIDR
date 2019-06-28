using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    public class DataItem
    {
        /// <summary>
        /// If true, will return default instead of throwing an exception if the data type does not match when casting.
        /// </summary>
        public static bool DEFAULT_NO_MATCH = false;
        /// <summary>
        /// Underlying object.
        /// </summary>
        public object Value { get; private set; } = null;

        /// <summary>
        /// Data Type associated with value.
        /// </summary>
        public DocRecordColumnType DataType { get; private set; }
        public DataItem()
        {
            Value = null;
            DataType = DocRecordColumnType.NUL;
        }
        public DataItem(object val, DocRecordColumnType type)
        {
            if(val == null 
                || val == DBNull.Value 
                //|| val is string && (string)val == string.Empty //Allow container to control.
                )
            {
                Value = null;
                DataType = DocRecordColumnType.NUL;
            }
            else
            {
                Value = val; //Trust caller to validate. (See column info CompareDataType and related methods.)
                DataType = type;
            }
        }
        #region math operators
        public static DataItem operator +(DataItem item, double val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem(Convert.ToDecimal(val) + (decimal)item.Value, item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem(val + (double)item.Value, item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value + (long)val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value + (int)val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value + (short)val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value + (byte)val, item.DataType);
                case DocRecordColumnType.Unknown:
                case DocRecordColumnType.NVarchar:
                case DocRecordColumnType.Varchar:
                    return new DataItem(item.Value.ToString() + val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support + operator.");
            }
        }
        public static DataItem operator +(DataItem item, decimal val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem(val + (decimal)item.Value, item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem(Convert.ToDouble(val) + (double)item.Value, item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value + (long)val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value + (int)val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value + (short)val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value + (byte)val, item.DataType);
                case DocRecordColumnType.Unknown:
                case DocRecordColumnType.NVarchar:
                case DocRecordColumnType.Varchar:
                    return new DataItem(item.Value.ToString() + val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support + operator.");
            }
        }
        public static DataItem operator +(DataItem item, long val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem(val + (decimal)item.Value, item.DataType);
                case DocRecordColumnType.DateTime:
                case DocRecordColumnType.Date:
                    return new DataItem(((DateTime)item.Value).AddDays(val), item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem(Convert.ToDouble(val) + (double)item.Value, item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value + val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value + val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value + (short)val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value + (byte)val, item.DataType);
                case DocRecordColumnType.Unknown:
                case DocRecordColumnType.NVarchar:
                case DocRecordColumnType.Varchar:
                    return new DataItem(item.Value.ToString() + val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support + operator.");
            }
        }
        public static DataItem operator +(DataItem item, int val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem(val + (decimal)item.Value, item.DataType);
                case DocRecordColumnType.DateTime:
                case DocRecordColumnType.Date:
                    return new DataItem(((DateTime)item.Value).AddDays(val), item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem(Convert.ToDouble(val) + (double)item.Value, item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value + val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value + val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value + (short)val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value + (byte)val, item.DataType);
                case DocRecordColumnType.Unknown:
                case DocRecordColumnType.NVarchar:
                case DocRecordColumnType.Varchar:
                    return new DataItem(item.Value.ToString() + val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support + operator.");
            }
        }
        public static DataItem operator +(DataItem item, short val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem(val + (decimal)item.Value, item.DataType);
                case DocRecordColumnType.DateTime:
                case DocRecordColumnType.Date:
                    return new DataItem(((DateTime)item.Value).AddDays(val), item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem(Convert.ToDouble(val) + (double)item.Value, item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value + val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value + val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value + val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value + (byte)val, item.DataType);
                case DocRecordColumnType.Unknown:
                case DocRecordColumnType.NVarchar:
                case DocRecordColumnType.Varchar:
                    return new DataItem(item.Value.ToString() + val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support + operator.");
            }
        }

        public static DataItem operator +(DataItem item, byte val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem(val + (decimal)item.Value, item.DataType);
                case DocRecordColumnType.DateTime:
                case DocRecordColumnType.Date:
                    return new DataItem(((DateTime)item.Value).AddDays(val), item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem(Convert.ToDouble(val) + (double)item.Value, item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value + val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value + val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value + val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value + val, item.DataType);
                case DocRecordColumnType.Unknown:
                case DocRecordColumnType.NVarchar:
                case DocRecordColumnType.Varchar:
                    return new DataItem(item.Value.ToString() + val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support + operator.");
            }
        }
        public static DataItem operator -(DataItem item, double val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value - Convert.ToDecimal(val), item.DataType);                
                case DocRecordColumnType.Double:
                    return new DataItem((double)item.Value - Convert.ToDouble(val), item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value - val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value - val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value - val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value - val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support - operator.");
            }
        }

        public static DataItem operator -(DataItem item, decimal val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value - val, item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem((double)item.Value - Convert.ToDouble(val), item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value - val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value - val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value - val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value - val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support - operator.");
            }
        }
        public static DataItem operator -(DataItem item, long val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value - val, item.DataType);
                case DocRecordColumnType.DateTime:
                case DocRecordColumnType.Date:
                    return new DataItem(((DateTime)item.Value).AddDays(-val), item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem((double)item.Value - Convert.ToDouble(val), item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value - val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value - val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value - val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value - val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support - operator.");
            }
        }
        public static DataItem operator -(DataItem item, int val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value - val, item.DataType);
                case DocRecordColumnType.DateTime:
                case DocRecordColumnType.Date:
                    return new DataItem(((DateTime)item.Value).AddDays(-val), item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem((double)item.Value - Convert.ToDouble(val), item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value - val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value - val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value - (short)val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value - val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support - operator.");
            }
        }

        public static DataItem operator -(DataItem item, short val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value - val, item.DataType);
                case DocRecordColumnType.DateTime:
                case DocRecordColumnType.Date:
                    return new DataItem(((DateTime)item.Value).AddDays(-val), item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem((double)item.Value - Convert.ToDouble(val), item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value - val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value - val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value - val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value - val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support - operator.");
            }
        }
        public static DataItem operator -(DataItem item, byte val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value - val, item.DataType);
                case DocRecordColumnType.DateTime:
                case DocRecordColumnType.Date:
                    return new DataItem(((DateTime)item.Value).AddDays(-val), item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem( (double)item.Value-Convert.ToDouble(val) , item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value - val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value - val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value - val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value - val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support - operator.");
            }
        }



        public static DataItem operator *(DataItem item, double val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value * Convert.ToDecimal(val), item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem((double)item.Value * Convert.ToDouble(val), item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value * val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value * val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value * val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value * val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support * operator.");
            }
        }

        public static DataItem operator *(DataItem item, decimal val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value * val, item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem((double)item.Value * Convert.ToDouble(val), item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value * val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value * val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value * val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value * val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support * operator.");
            }
        }
        public static DataItem operator *(DataItem item, long val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value * val, item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem((double)item.Value * Convert.ToDouble(val), item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value * val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value * val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value * val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value * val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support * operator.");
            }
        }
        public static DataItem operator *(DataItem item, int val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value * val, item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem((double)item.Value * Convert.ToDouble(val), item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value * val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value * val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value * (short)val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value * val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support * operator.");
            }
        }

        public static DataItem operator *(DataItem item, short val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value * val, item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem((double)item.Value * Convert.ToDouble(val), item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value * val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value * val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value * val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value * val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support * operator.");
            }
        }
        public static DataItem operator *(DataItem item, byte val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value * val, item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem((double)item.Value * Convert.ToDouble(val), item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value * val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value * val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value * val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value * val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support * operator.");
            }
        }
        public static DataItem operator /(DataItem item, double val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value / Convert.ToDecimal(val), item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem((double)item.Value / Convert.ToDouble(val), item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value / val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value / val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value / val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value / val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support / operator.");
            }
        }

        public static DataItem operator /(DataItem item, decimal val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value / val, item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem((double)item.Value / Convert.ToDouble(val), item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value / val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value / val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value / val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value / val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support / operator.");
            }
        }
        public static DataItem operator /(DataItem item, long val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value / val, item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem((double)item.Value / Convert.ToDouble(val), item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value / val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value / val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value / val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value / val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support / operator.");
            }
        }
        public static DataItem operator /(DataItem item, int val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value / val, item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem((double)item.Value / Convert.ToDouble(val), item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value / val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value / val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value / (short)val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value / val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support / operator.");
            }
        }

        public static DataItem operator /(DataItem item, short val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value / val, item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem((double)item.Value / Convert.ToDouble(val), item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value / val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value / val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value / val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value / val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support / operator.");
            }
        }
        public static DataItem operator /(DataItem item, byte val)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                throw new ArgumentNullException(nameof(item));
            switch (item.DataType)
            {
                case DocRecordColumnType.Decimal:
                    return new DataItem((decimal)item.Value / val, item.DataType);
                case DocRecordColumnType.Double:
                    return new DataItem((double)item.Value / Convert.ToDouble(val), item.DataType);
                case DocRecordColumnType.Bigint:
                    return new DataItem((long)item.Value / val, item.DataType);
                case DocRecordColumnType.Int:
                    return new DataItem((int)item.Value / val, item.DataType);
                case DocRecordColumnType.Smallint:
                    return new DataItem((short)item.Value / val, item.DataType);
                case DocRecordColumnType.Tinyint:
                    return new DataItem((byte)item.Value / val, item.DataType);
                default:
                    throw new ArgumentException("DataItem's data type does not support / operator.");
            }
        }

        #endregion
        #region implicit Reverse - Compare DataItem's type versus TypedRecord column before adding
        public static implicit operator DataItem(int item)
        {
            var o = new DataItem(item, DocRecordColumnType.Int);
            return o;
        }
        public static implicit operator DataItem(int? item)
        {
            DataItem o;
            if (item == null)
                o = new DataItem(item, DocRecordColumnType.NUL);
            else
                o = new DataItem(item.Value, DocRecordColumnType.Int);
            return o;
        }
        public static implicit operator DataItem(int[] item)
        {
            if (item == null)
                return new DataItem(item, DocRecordColumnType.NUL);
            var o = new DataItem(item, DocRecordColumnType.Int);
            return o;
        }
        public static implicit operator DataItem(int?[] item)
        {
            DataItem o;
            if (item == null)
                o = new DataItem();
            else
                o = new DataItem(item, DocRecordColumnType.Int);
            return o;
        }

        public static implicit operator DataItem(long item)
        {
            var o = new DataItem(item, DocRecordColumnType.Bigint);
            return o;
        }
        public static implicit operator DataItem(long? item)
        {
            DataItem o;
            if (item == null)
                o = new DataItem();
            else
                o = new DataItem(item.Value, DocRecordColumnType.Bigint);
            return o;
        }
        public static implicit operator DataItem(long[] item)
        {
            if (item == null)
                return new DataItem();
            var o = new DataItem(item, DocRecordColumnType.Bigint);
            return o;
        }
        public static implicit operator DataItem(long?[] item)
        {
            //Null check in constructor
            return new DataItem(item, DocRecordColumnType.Bigint);            
        }
        public static implicit operator DataItem(short item)
        {
            return new DataItem(item, DocRecordColumnType.Smallint);
            
        }
        public static implicit operator DataItem(short? item)
        {
            DataItem o;
            if (item == null)
                o = new DataItem();
            else
                o = new DataItem(item.Value, DocRecordColumnType.Smallint);
            return o;
        }

        public static implicit operator DataItem(short[] item)
        {            
            return new DataItem(item, DocRecordColumnType.Smallint);            
        }
        public static implicit operator DataItem(short?[] item)
        {
            return new DataItem(item, DocRecordColumnType.Smallint);
            
        }
        public static implicit operator DataItem(byte item)
        {
            return new DataItem(item, DocRecordColumnType.Tinyint);            
        }
        public static implicit operator DataItem(byte? item)
        {
            DataItem o;
            if (item == null)
                o = new DataItem();
            else
                o = new DataItem(item.Value, DocRecordColumnType.Tinyint);
            return o;
        }
        public static implicit operator DataItem(byte[] item)
        {
            if (item == null)
                return new DataItem();
            var o = new DataItem(item, DocRecordColumnType.Tinyint);
            return o;
        }
        public static implicit operator DataItem(byte?[] item)
        {
            DataItem o;
            if (item == null)
                o = new DataItem();
            else
                o = new DataItem(item, DocRecordColumnType.Tinyint);
            return o;
        }
        public static implicit operator DataItem(bool item)
        {
            var o = new DataItem(item, DocRecordColumnType.Bool);
            return o;
        }
        public static implicit operator DataItem(bool? item)
        {           
            if (item == null)
                return new DataItem(item, DocRecordColumnType.NUL);
            return new DataItem(item.Value, DocRecordColumnType.Bool);            
        }
        public static implicit operator DataItem(bool[] item)
        {
            if (item == null)
                return new DataItem();
            var o = new DataItem(item, DocRecordColumnType.Bool);
            return o;
        }
        public static implicit operator DataItem(bool?[] item)
        {
            DataItem o;
            if (item == null)
                o = new DataItem(item, DocRecordColumnType.NUL);
            else
                o = new DataItem(item, DocRecordColumnType.Bool);
            return o;
        }
        public static implicit operator DataItem(decimal item)
        {
            return new DataItem(item, DocRecordColumnType.Decimal);
        }
        public static implicit operator DataItem(decimal? item)
        {
            if (item.HasValue)
                return new DataItem(item.Value, DocRecordColumnType.Decimal);
            return new DataItem();
        }
        public static implicit operator DataItem(decimal[] item)
        {
            if (item == null)
                return new DataItem(null, DocRecordColumnType.NUL);
            return new DataItem(item, DocRecordColumnType.Decimal);
        }
        public static implicit operator DataItem(decimal?[] item)
        {
            if (item == null)
                return new DataItem(null, DocRecordColumnType.NUL);
            return new DataItem(item, DocRecordColumnType.Decimal);
        }
        public static implicit operator DataItem(DateTime item)
        {
            var o = new DataItem(item, DocRecordColumnType.DateTime);
            return o;
        }
        public static implicit operator DataItem(DateTime? item)
        {            
            if (item == null)
                return new DataItem(item, DocRecordColumnType.NUL);
            return new DataItem(item.Value, DocRecordColumnType.DateTime);            
        }
        public static implicit operator DataItem(DateTime[] item)
        {
            if (item == null)
                return new DataItem(null, DocRecordColumnType.NUL);
            return new DataItem(item, DocRecordColumnType.DateTime);            
        }
        public static implicit operator DataItem(DateTime?[] item)
        {
            
            if (item == null)
                return new DataItem(item, DocRecordColumnType.NUL);
            return new DataItem(item, DocRecordColumnType.DateTime);
        }
        public static implicit operator DataItem(string item)
        {            
            if (item == null)
                return new DataItem(item, DocRecordColumnType.NUL);
            return new DataItem(item, DocRecordColumnType.Varchar);            
        }
        public static implicit operator DataItem(string[] item)
        {            
            if (item == null)
                return new DataItem(item, DocRecordColumnType.NUL);
            return new DataItem(item, DocRecordColumnType.Varchar);            
        }
        #endregion
        #region IMPLICIT  
        public static implicit operator int(DataItem item)
        {
            switch (item.DataType)
            {
                case DocRecordColumnType.Smallint:
                case DocRecordColumnType.Tinyint:
                case DocRecordColumnType.Int:
                case DocRecordColumnType.Bigint:
                case DocRecordColumnType.Decimal:
                case DocRecordColumnType.Money:
                    return (int)item.Value;
                case DocRecordColumnType.Double:
                    return Convert.ToInt32((double)item.Value);
                case DocRecordColumnType.Date:
                case DocRecordColumnType.DateTime:
                    return ((DateTime)item.Value).GetDateSerial();
            }
            if (DEFAULT_NO_MATCH)
                return default;
            throw new Exception("DataType does not match int.");
        }
        
        public static implicit operator int?(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                return null;
            return (int)item;
        }
        public static implicit operator int[](DataItem item)
        {
            if (item.Value is int[])
                return (int[])item.Value;
            else if (item.Value is IList<int>)
            {
                var i = item.Value as IList<int>;
                return i.ToArray();
            }
            throw new Exception("Data Type does not match int[]");
        }
        public static implicit operator int?[](DataItem item)
        {
            if (item.Value is int?[])
                return (int?[])item.Value;
            else if (item.Value is IList<int?>)
            {
                var i = item.Value as IList<int?>;
                return i.ToArray();
            }
            throw new Exception("Data Type does not match int[]");
        }
        public static implicit operator bool(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.Bool)
                return (bool)item.Value;
            if (DEFAULT_NO_MATCH)
                return default;
            throw new Exception("DataType does not match bool.");
        }
        public static implicit operator bool?(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                return null;
            return (bool)item;
        }
        public static implicit operator bool[](DataItem item)
        {
            if (item.Value is bool[])
                return (bool[])item.Value;            
            else if (item.Value is IList<bool>)
            {
                var i = item.Value as IList<bool>;
                return i.ToArray();
            }
            throw new Exception("Data Type does not match bool[]");
        }
        public static implicit operator bool?[](DataItem item)
        {
            if (item.Value is bool?[])
                return (bool?[])item.Value;            
            else if (item.Value is IList<bool?>)
            {
                var i = item.Value as IList<bool?>;
                return i.ToArray();
            }
            throw new Exception("Data Type does not match bool?[]");
        }
        public static implicit operator byte(DataItem item)
        {
            switch (item.DataType)
            {
                case DocRecordColumnType.Smallint:
                case DocRecordColumnType.Tinyint:
                case DocRecordColumnType.Int:
                case DocRecordColumnType.Bigint:
                case DocRecordColumnType.Decimal:
                case DocRecordColumnType.Money:
                    return (byte)item.Value;
                case DocRecordColumnType.Double:
                    return Convert.ToByte((double)item.Value);
                case DocRecordColumnType.Date:
                case DocRecordColumnType.DateTime:
                    return (byte)((DateTime)item.Value).GetDateSerial();
            }
            if (DEFAULT_NO_MATCH)
                return default;
            throw new Exception("DataType does not match Expected.");
        }
        public static implicit operator byte?(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                return null;
            return (byte)item;
        }

        public static implicit operator byte[](DataItem item)
        {
            if (item.Value is byte[])
                return (byte[])item.Value;
            else if (item.Value is IList<byte>)
            {
                var i = item.Value as IList<byte>;
                return i.ToArray();
            }
            throw new Exception("Data Type does not match byte[]");
        }
        public static implicit operator byte?[](DataItem item)
        {
            if (item.Value is byte?[])
                return (byte?[])item.Value;
            else if (item.Value is IList<byte?>)
            {
                var i = item.Value as IList<byte?>;
                return i.ToArray();
            }
            throw new Exception("Data Type does not match byte?[]");
        }
        public static implicit operator short(DataItem item)
        {
            switch (item.DataType)
            {
                case DocRecordColumnType.Smallint:
                case DocRecordColumnType.Tinyint:
                case DocRecordColumnType.Int:
                case DocRecordColumnType.Bigint:
                case DocRecordColumnType.Decimal:
                case DocRecordColumnType.Money:
                    return (short)item.Value;
                case DocRecordColumnType.Double:
                    return Convert.ToInt16((double)item.Value);
                case DocRecordColumnType.Date:
                case DocRecordColumnType.DateTime:
                    {
                        return (short)((DateTime)item.Value).GetDateSerial();
                    }
            }
            if (DEFAULT_NO_MATCH)
                return default;
            throw new Exception("DataType does not match Expected.");
        }
        public static implicit operator short?(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                return null;
            return (short)item;
        }

        public static implicit operator short[](DataItem item)
        {
            if (item.Value is short[])
                return (short[])item.Value;
            else if (item.Value is IList<short>)
            {
                var i = item.Value as IList<short>;
                return i.ToArray();
            }
            throw new Exception("Data Type does not match short[]");
        }
        public static implicit operator short?[](DataItem item)
        {
            if (item.Value is short?[])
                return (short?[])item.Value;
            else if (item.Value is IList<short?>)
            {
                var i = item.Value as IList<short?>;
                return i.ToArray();
            }
            throw new Exception("Data Type does not match short?[]");
        }
        public static implicit operator decimal(DataItem item)
        {
            switch (item.DataType)
            {
                case DocRecordColumnType.Smallint:
                case DocRecordColumnType.Tinyint:
                case DocRecordColumnType.Int:
                case DocRecordColumnType.Bigint:
                case DocRecordColumnType.Decimal:
                case DocRecordColumnType.Money:
                    return (decimal)item.Value;
                case DocRecordColumnType.Double:
                    return Convert.ToInt64((double)item.Value);
                case DocRecordColumnType.Date:
                case DocRecordColumnType.DateTime:
                    {
                        return ((DateTime)item.Value).GetDateSerial();
                    }
            }
            if (DEFAULT_NO_MATCH)
                return default;
            throw new Exception("DataType does not match Expected.");
        }
        public static implicit operator decimal?(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                return null;
            return (decimal)item;
        }

        public static implicit operator decimal[](DataItem item)
        {
            if (item.Value is decimal[])
                return (decimal[])item.Value;
            else if (item.Value is IList<decimal>)
            {
                var i = item.Value as IList<decimal>;
                return i.ToArray();
            }
            throw new Exception("Data Type does not match decimal[]");
        }
        public static implicit operator decimal?[](DataItem item)
        {
            if (item.Value is decimal?[])
                return (decimal?[])item.Value;
            else if (item.Value is IList<decimal?>)
            {
                var i = item.Value as IList<decimal?>;
                return i.ToArray();
            }
            throw new Exception("Data Type does not match decimal?[]");
        }

        public static implicit operator long(DataItem item)
        {
            switch (item.DataType)
            {
                case DocRecordColumnType.Smallint:
                case DocRecordColumnType.Tinyint:
                case DocRecordColumnType.Int:
                case DocRecordColumnType.Bigint:
                case DocRecordColumnType.Decimal:
                case DocRecordColumnType.Money:
                    return (long)item.Value;
                case DocRecordColumnType.Double:
                    return Convert.ToInt64((double)item.Value);
                case DocRecordColumnType.Date:
                case DocRecordColumnType.DateTime:
                    {
                        return ((DateTime)item.Value).GetDateSerial();                        
                    }
            }
            if (DEFAULT_NO_MATCH)
                return default;
            throw new Exception("DataType does not match Expected.");
        }   
        public static implicit operator long?(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                return null;
            return (long)item;
        }

        public static implicit operator long[](DataItem item)
        {
            if (item.Value is long[])
                return (long[])item.Value;
            else if (item.Value is IList<long>)
            {
                var i = item.Value as IList<long>;
                return i.ToArray();
            }
            throw new Exception("Data Type does not match long[]");
        }
        public static implicit operator long?[](DataItem item)
        {
            if (item.Value is long?[])
                return (long?[])item.Value;
            else if (item.Value is IList<long?>)
            {
                var i = item.Value as IList<long?>;
                return i.ToArray();
            }
            throw new Exception("Data Type does not match long?[]");
        }
        public static implicit operator DateTime(DataItem item)
        {
            switch (item.DataType)
            {
                case DocRecordColumnType.Smallint:
                case DocRecordColumnType.Tinyint:
                case DocRecordColumnType.Int:
                case DocRecordColumnType.Bigint:
                case DocRecordColumnType.Decimal:
                case DocRecordColumnType.Money:
                    return ((int)item).GetDateFromSerial();
                case DocRecordColumnType.Double:
                    return DateTime.FromOADate(item);
                case DocRecordColumnType.Date:
                case DocRecordColumnType.DateTime:
                    {
                        return (DateTime)item.Value;
                    }
            }
            if (DEFAULT_NO_MATCH)
                return default;
            throw new Exception("DataType does not match Expected");
        }
        public static implicit operator DateTime?(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                return null;
            return (DateTime)item;
        }
        
        public static implicit operator DateTime[](DataItem item)
        {
            if (item.Value is DateTime[])
                return (DateTime[])item.Value;
            else if (item.Value is IList<DateTime>)
            {
                var i = item.Value as IList<DateTime>;
                return i.ToArray();
            }
            throw new Exception("Data Type does not match DateTime[]");
        }
        public static implicit operator DateTime?[](DataItem item)
        {
            if (item.Value is DateTime?[])
                return (DateTime?[])item.Value;
            else if (item.Value is IList<DateTime?>)
            {
                var i = item.Value as IList<DateTime?>;
                return i.ToArray();
            }
            throw new Exception("Data Type does not match DateTime?[]");
        }
        public static implicit operator string(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                return null;
            if(item.DataType == DocRecordColumnType.Money)
            {
                decimal d = item;
                return d.ToString("C");
            }
            if(item.DataType.In(DocRecordColumnType.Date, DocRecordColumnType.DateTime))
            {
                DateTime d = item;
                if (item.DataType == DocRecordColumnType.Date)
                    return d.ToShortDateString();
                return d.ToString();
            }
            return item.Value.ToString();
        }

        public static implicit operator string[](DataItem item)
        {
            if (item.Value is string[])
                return (string[])item.Value;
            else if (item.Value is IList<string>)
            {
                var i = item.Value as IList<string>;
                return i.ToArray();
            }
            throw new Exception("Data Type does not match string[]");
        }
        public override int GetHashCode()
        {
            if (Value == null)
                return base.GetHashCode();
            return Value.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (Value == null && obj == null)
                return true;
            else if (Value == null)
                return false;
            return Value.Equals(obj);
        }
        /// <summary>
        /// Underlying value's ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Value?.ToString();
        }
        #endregion
        /// <summary>
        /// If object type is IList rather than a plain array, convert to array.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public static void CheckArrayObject(ref object o, DocRecordColumnType dataType)
        {
            var tInfo = o.GetType();
            if (tInfo.IsArray)
                return;
            switch (dataType)
            {
                case DocRecordColumnType.Int:
                    if (!CheckType<int>(ref o))
                        CheckType<int?>(ref o);
                    break;
                case DocRecordColumnType.Bigint:
                    if(!CheckType<long>(ref o))
                        CheckType<long?>(ref o);
                    break;
                case DocRecordColumnType.Smallint:
                    if(!CheckType<short>(ref o))
                        CheckType<short?>(ref o);
                    break;
                case DocRecordColumnType.Date:
                case DocRecordColumnType.DateTime:
                    if (!CheckType<DateTime>(ref o))
                        CheckType<DateTime?>(ref o);
                    break;
                case DocRecordColumnType.Tinyint:
                    if (!CheckType<byte>(ref o))
                        CheckType<byte?>(ref o);
                    break;
                case DocRecordColumnType.Decimal:
                case DocRecordColumnType.Money:
                    if (!CheckType<decimal>(ref o))
                        CheckType<decimal?>(ref o);
                    break;
                default:
                    CheckType<string>(ref o);
                    break;

            }
        }
        static bool CheckType<T>(ref object o)
        {
            if(o is IList<T>)
            {
                o = ((IList<T>)o).ToArray();
                return true;
            }
            return false;
        }

        /*
        #region explicit
        public static explicit operator int(DataItem item)
        {
            switch (item.DataType)
            {
                case DocRecordColumnType.Smallint:
                case DocRecordColumnType.Tinyint:
                case DocRecordColumnType.Int:
                case DocRecordColumnType.Bigint:
                case DocRecordColumnType.Decimal:
                case DocRecordColumnType.Money:
                    return (int)item.Value;
                case DocRecordColumnType.Double:
                    return Convert.ToInt32((double)item.Value);
                case DocRecordColumnType.Date:
                case DocRecordColumnType.DateTime:
                    return ((DateTime)item.Value).GetDateSerial();
            }
            if (DEFAULT_NO_MATCH)
                return default;
            throw new Exception("DataType does not match int.");
        }
        public static explicit operator int?(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                return null;
            return (int)item;
        }
        public static explicit operator bool(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.Bool)
                return (bool)item.Value;
            if (DEFAULT_NO_MATCH)
                return default;
            throw new Exception("DataType does not match bool.");
        }
        public static explicit operator bool?(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                return null;
            return (bool)item;
        }
        public static explicit operator byte(DataItem item)
        {
            switch (item.DataType)
            {
                case DocRecordColumnType.Smallint:
                case DocRecordColumnType.Tinyint:
                case DocRecordColumnType.Int:
                case DocRecordColumnType.Bigint:
                case DocRecordColumnType.Decimal:
                case DocRecordColumnType.Money:
                    return (byte)item.Value;
                case DocRecordColumnType.Double:
                    return Convert.ToByte((double)item.Value);
                case DocRecordColumnType.Date:
                case DocRecordColumnType.DateTime:
                    return (byte)((DateTime)item.Value).GetDateSerial();
            }
            if (DEFAULT_NO_MATCH)
                return default;
            throw new Exception("DataType does not match Expected.");
        }
        public static explicit operator byte?(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                return null;
            return (byte)item;
        }
        public static explicit operator short(DataItem item)
        {
            switch (item.DataType)
            {
                case DocRecordColumnType.Smallint:
                case DocRecordColumnType.Tinyint:
                case DocRecordColumnType.Int:
                case DocRecordColumnType.Bigint:
                case DocRecordColumnType.Decimal:
                case DocRecordColumnType.Money:
                    return (short)item.Value;
                case DocRecordColumnType.Double:
                    return Convert.ToInt16((double)item.Value);
                case DocRecordColumnType.Date:
                case DocRecordColumnType.DateTime:
                    {
                        return (short)((DateTime)item.Value).GetDateSerial();
                    }
            }
            if (DEFAULT_NO_MATCH)
                return default;
            throw new Exception("DataType does not match Expected.");
        }
        public static explicit operator short?(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                return null;
            return (short)item;
        }
        public static explicit operator decimal(DataItem item)
        {
            switch (item.DataType)
            {
                case DocRecordColumnType.Smallint:
                case DocRecordColumnType.Tinyint:
                case DocRecordColumnType.Int:
                case DocRecordColumnType.Bigint:
                case DocRecordColumnType.Decimal:
                case DocRecordColumnType.Money:
                    return (decimal)item.Value;
                case DocRecordColumnType.Double:
                    return Convert.ToInt64((double)item.Value);
                case DocRecordColumnType.Date:
                case DocRecordColumnType.DateTime:
                    {
                        return ((DateTime)item.Value).GetDateSerial();
                    }
            }
            if (DEFAULT_NO_MATCH)
                return default;
            throw new Exception("DataType does not match Expected.");
        }
        public static explicit operator decimal?(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                return null;
            return (decimal)item;
        }

        public static explicit operator long(DataItem item)
        {
            switch (item.DataType)
            {
                case DocRecordColumnType.Smallint:
                case DocRecordColumnType.Tinyint:
                case DocRecordColumnType.Int:
                case DocRecordColumnType.Bigint:
                case DocRecordColumnType.Decimal:
                case DocRecordColumnType.Money:
                    return (long)item.Value;
                case DocRecordColumnType.Double:
                    return Convert.ToInt64((double)item.Value);
                case DocRecordColumnType.Date:
                case DocRecordColumnType.DateTime:
                    {
                        return ((DateTime)item.Value).GetDateSerial();
                    }
            }
            if (DEFAULT_NO_MATCH)
                return default;
            throw new Exception("DataType does not match Expected.");
        }
        public static explicit operator long?(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                return null;
            return (long)item;
        }
        public static explicit operator DateTime(DataItem item)
        {
            switch (item.DataType)
            {
                case DocRecordColumnType.Smallint:
                case DocRecordColumnType.Tinyint:
                case DocRecordColumnType.Int:
                case DocRecordColumnType.Bigint:
                case DocRecordColumnType.Decimal:
                case DocRecordColumnType.Money:
                    return ((int)item).GetDateFromSerial();
                case DocRecordColumnType.Double:
                    return DateTime.FromOADate(item);
                case DocRecordColumnType.Date:
                case DocRecordColumnType.DateTime:
                    {
                        return (DateTime)item.Value;
                    }
            }
            if (DEFAULT_NO_MATCH)
                return default;
            throw new Exception("DataType does not match Expected");
        }
        public static explicit operator DateTime?(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                return null;
            return (DateTime)item;
        }
        public static explicit operator string(DataItem item)
        {
            if (item.DataType == DocRecordColumnType.NUL)
                return null;
            if (item.DataType == DocRecordColumnType.Money)
            {
                decimal d = (decimal)item.Value;
                return d.ToString("C");
            }
            if (item.DataType.In(DocRecordColumnType.Date, DocRecordColumnType.DateTime))
            {
                DateTime d = (DateTime)item.Value;
                if (item.DataType == DocRecordColumnType.Date)
                    return d.ToShortDateString();
                return d.ToString();
            }
            return item.Value.ToString();
        }

        #endregion

        // */
    }
}
