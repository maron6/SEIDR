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
