using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// Column information
    /// </summary>
    public interface IRecordColumnInfo
    {
        /// <summary>
        /// Column position in a record.
        /// </summary>
        int Position { get; }
        /// <summary>
        /// If sort is ascending order.
        /// </summary>
        bool SortASC { get; set; }
      
    }
    /// <summary>
    /// Column information for Doc Reader/Writer
    /// </summary>
    public sealed class DocRecordColumnInfo : IRecordColumnInfo
    {
        /// <summary>
        /// Check whether or not a value indicates that the column should be text qualified.
        /// </summary>
        /// <param name="delimiter"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool CheckNeedTextQualifier(string delimiter, string value)
        {
            if (string.IsNullOrEmpty(delimiter) || string.IsNullOrEmpty(value))
                return false;
            if (!DataType.In(DocRecordColumnType.Varchar, DocRecordColumnType.NVarchar, DocRecordColumnType.Unknown))
                return false;
            return value.Contains(delimiter);
        }
        public bool CheckNeedTextQualifier(char? delimiter, string value)
        {
            if (!delimiter.HasValue)
                return false;
            return CheckNeedTextQualifier(delimiter.Value.ToString(), value);
        }
        /// <summary>
        /// For formatting - indicates that data is separated by a subdelimiter and can be returned as an array or list.
        /// </summary>
        public bool Array { get; private set; }
        /// <summary>
        /// Data is expected to be an array - delimiter for splitting data.
        /// </summary>
        public string ArrayDelimiter { get; private set; }
        /// <summary>
        /// Set to Array mode, and also set the ArrayDelimiter.
        /// </summary>
        /// <param name="arrDelim"></param>
        /// <returns></returns>
        public DocRecordColumnInfo SetArrayMode(string arrDelim)
        {
            ArrayDelimiter = arrDelim;
            Array = true;
            return this;
        }
        /// <summary>
        /// Sets <see cref="Array"/> to false, and clears the <see cref="ArrayDelimiter"/>
        /// </summary>
        /// <returns></returns>
        public DocRecordColumnInfo UnsetArrayMode()
        {
            Array = false;
            ArrayDelimiter = null;
            return this;
        }
        DocRecordColumnType _DataType = DocRecordColumnType.Unknown;
        /// <summary>
        /// Indicates type of data
        /// </summary>
        public DocRecordColumnType DataType
        {
            get => _DataType;
            set
            {
                _DataType = value;
                if (value.NotIn(DocRecordColumnType.Unknown, DocRecordColumnType.Varchar, DocRecordColumnType.NVarchar))
                    TextQualify = false; //by default, don't text qualify dates/numerics/etc
            }
        }
        /// <summary>
        /// Optional, additional meta Data to associate to a column.
        /// <para>Defaults to null.</para>
        /// </summary>
        public Dictionary<string, string> ExtraMetaData { get; set; } = null;
        /// <summary>
        /// Optional formatting for certain data types.
        /// <para>Currently only used for Date/DateTime</para>
        /// </summary>
        public string Format { get; set; } = null;
        /// <summary>
        /// Treats the column as an int by position.
        /// </summary>
        /// <param name="column"></param>
        public static implicit operator int(DocRecordColumnInfo column)
        {
            return column.Position;
        }
        /// <summary>
        /// Treats the column as a string by taking its Name (primary identifier)
        /// </summary>
        /// <param name="column"></param>
        public static implicit operator string(DocRecordColumnInfo column)
        {
            return column.ColumnName;
        }
        /// <summary>
        /// Sort order for <see cref="IRecordColumnInfo"/>
        /// </summary>
        bool IRecordColumnInfo.SortASC { get; set; } = true;
        /// <summary>
        /// If true, a value matching <see cref="string.Empty"/> will be treated as null.
        /// </summary>
        public bool NullIfEmpty { get; set; } = true;
        int ml = -1;
        /// <summary>
        /// Maximum width of the column when used with fixed width
        /// </summary>
        public int? MaxLength
        {
            get
            {
                if (ml > 0)
                    return ml;
                return null;
            }
            set
            {
                if (value.HasValue && value > 0)
                    ml = value.Value;
                else
                    ml = -1;
            }
        }
        #region setters
        
        public DocRecordColumnInfo SetNullIfEmpty(bool value)
        {
            NullIfEmpty = value;
            return this;
        }
        public DocRecordColumnInfo SetMaxLength(int? value)
        {
            MaxLength = value;
            return this;
        }
        public DocRecordColumnInfo SetTextQualify(bool value)
        {
            TextQualify = value;
            return this;
        }
        public DocRecordColumnInfo SetLeftJustify(bool value)
        {
            LeftJustify = value;
            return this;
        }
#endregion
        /*
        /// <summary>
        /// For use with variable width, cause column to end early. Will complicate parsing, DocReader paging, but should be doable, but will assume that 
        /// <para>E.g., final column ends as soon as it reaches a newline instead of after <see cref="MaxLength"/> characters</para>
        /// <para>Note: last column in fixed width should account for the space taken by NewLine</para>
        /// </summary>
        [Obsolete("Correct usage not fully implemented, not planned.")]
        public string EarlyTerminator { get; set; } = null;
        */
        /// <summary>
        /// Name of the column
        /// </summary>
        public string ColumnName { get; internal set; }
        /// <summary>
        /// <see cref="ColumnName"/>, but if the <see cref="OwnerAlias"/> is set, will be prepended by the OwnerAlias and a dot.
        /// <para>E.g. "a.ColName" or "fi .Name of Column"</para>
        /// </summary>
        public string FullColumnName => string.IsNullOrWhiteSpace(OwnerAlias) ? ColumnName : OwnerAlias + "." + ColumnName;
        /// <summary>
        /// Alias of the column's owner
        /// </summary>
        public string OwnerAlias { get; internal set; }
        /// <summary>
        /// Creates a new Doc Record Column info record, for getting information out of a Doc record
        /// </summary>
        /// <param name="Column"></param>
        /// <param name="Alias"></param>
        /// <param name="position">Column position</param>
        public DocRecordColumnInfo(string Column, string Alias, int position)
        {
            ColumnName = Column;
            OwnerAlias = Alias;
            Position = position;
        }
        /// <summary>
        /// Construct column info with unknown position. (Determine in collection)
        /// </summary>
        /// <param name="Column"></param>
        /// <param name="alias"></param>
        /// <param name="type"></param>
        public DocRecordColumnInfo(string Column, DocRecordColumnType type = DocRecordColumnType.Unknown, string alias = null)
        {
            ColumnName = Column;
            OwnerAlias = alias;
            Position = -1;
            DataType = type;
        }
        /// <summary>
        /// Creates a new Doc Record Column Info record, includes specifying type of data.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="Alias"></param>
        /// <param name="position"></param>
        /// <param name="type"></param>
        public DocRecordColumnInfo(string column, string Alias, int position, DocRecordColumnType type)
            :this(column, Alias, position)
        {
            DataType = type;
        }        

        /// <summary>
        /// Intended Position of the column in the raw file.
        /// </summary>
        public int Position { get; internal set; }
        /// <summary>
        /// Used when writing with <see cref="DocWriter"/>. Ignored in Fixed width mode.
        /// </summary>
        public bool TextQualify { get; set; } = false;
        /// <summary>
        /// Used when writing with <see cref="DocWriter"/>. Ignored in delimited mode
        /// </summary>
        public bool LeftJustify { get; set; } = true;
        /// <summary>
        /// Check that the columns are referencing the same data
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {            
            var other = obj as DocRecordColumnInfo;
            if (other == null)
                return false;
            if (other.ColumnName == ColumnName && other.OwnerAlias == OwnerAlias && other.Position == Position)
                return true;
            return false;            
        }
        /// <summary>
        /// Check that the alias and column name match
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(DocRecordColumnInfo a, DocRecordColumnInfo b)
        {
            return a?.ColumnName == b?.ColumnName && a?.OwnerAlias == b?.OwnerAlias && a?.Position == b?.Position;
        }
        /// <summary>
        /// Check that the alias or column name do not match
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(DocRecordColumnInfo a, DocRecordColumnInfo b)
        {
            return !(a == b);
        }
        /// <summary>
        /// Hash code is dependent on column name and alias only
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return ColumnName.GetHashCode() * (OwnerAlias?.GetHashCode() ?? -1);
        }
        /// <summary>
        /// ToString, just returns Column Name.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ColumnName;
        }
        /// <summary>
        /// Alternate version of toString.
        /// </summary>
        /// <param name="includeAlias">If true and alias is non empty, will be formatted as [Alias].[ColumnName]<para>
        /// If true and alias is empty/white space, will return [ColumnName]</para><para>
        /// Otherwise, will return <see cref="ToString()"/></para></param>
        /// <returns></returns>
        public string ToString(bool includeAlias)
        {
            if (includeAlias && !string.IsNullOrWhiteSpace(OwnerAlias))
                return $"[{OwnerAlias}].[{ColumnName}]";
            else if (includeAlias)
                return "[" + ColumnName + "]";
            return ToString();
        }
        /// <summary>
        /// Sets up the columns using the same alias and in the same order as the passed parameters
        /// </summary>
        /// <param name="Alias"></param>
        /// <param name="ColumnList"></param>
        /// <returns></returns>
        public static DocRecordColumnInfo[] CreateColumns(string Alias, params string[] ColumnList)
        {
            var cols = new DocRecordColumnInfo[ColumnList.Length];
            for(int i = 0; i < ColumnList.Length; i++)
            {
                cols[i] = new DocRecordColumnInfo(ColumnList[i], Alias, i);
            }
            return cols;
        }
        /// <summary>
        /// Uses <see cref="TryGet(string, out object)"/> to return an object of type <typeparamref name="T"/>.
        /// <para>If the data type does not match, will either return the default for the type or throw an error, depending on <paramref name="DefaultOnFailure"/>.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="DefaultOnFailure"></param>
        /// <returns></returns>
        public T Evaluate<T>(string value, bool DefaultOnFailure = false)
        {
            object o;
            if(TryGet(value, out o))
            {
                if (o is T)
                    return (T)o;
                if(o == null)
                {
                    if (default(T) == null || DefaultOnFailure)
                        return default;
                    throw new Exception("Value is null, but variable does not allow null.");
                }
                if (Nullable.GetUnderlyingType(typeof(T)) == o.GetType())
                    return (T)Convert.ChangeType(o, typeof(T));
                if (Nullable.GetUnderlyingType(o.GetType()) == typeof(T))
                {
                    if (o == null)
                        return default;
                    return (T)o;
                }
            }
            if (DefaultOnFailure)
                return default;
            throw new Exception("Unable to get value.");
        }
        public string FormatValue(object o)
        {            
            if (o == null)
                return null;
            string s;
            if (!Array)
            {
                if (Format != null)
                {
                    if (o is DateTime)
                    {
                        return ((DateTime)o).ToString(Format);
                    }
                    if(o is DateTime?)
                    {
                        return ((DateTime?)o).Value.ToString(Format);
                    }
                }
                s = o.ToString();
                if (NullIfEmpty && string.IsNullOrEmpty(s))
                    return null;
                return s;
            }
            switch (DataType)
            {
                case DocRecordColumnType.Date:
                case DocRecordColumnType.DateTime:
                    if(o is DateTime[])
                    {
                        var t = ((DateTime[])o).Select(dt => FormatValue(dt));
                        return string.Join(ArrayDelimiter, t);
                    }
                    if (o is DateTime?[])
                    {
                        var t = ((DateTime?[])o).Select(dt => FormatValue(dt));
                        return string.Join(ArrayDelimiter, t);
                    }
                    if (Format == null)
                        return o.ToString();
                    if (o is DateTime)
                        return ((DateTime)o).ToString(Format);                    
                    return (o as DateTime?).Value.ToString(Format);
                case DocRecordColumnType.Tinyint:
                    if (o is byte[])
                    {
                        return string.Join(ArrayDelimiter, ((byte[])o).Select(i => i.ToString()));
                    }
                    if (o is byte?[])
                    {
                        return string.Join(ArrayDelimiter, ((byte?[])o).Select(i => i.ToString()));
                    }
                    break;
                case DocRecordColumnType.Smallint:
                    if (o is short[])
                    {
                        return string.Join(ArrayDelimiter, ((short[])o).Select(i => i.ToString()));
                    }
                    if (o is short?[])
                    {
                        return string.Join(ArrayDelimiter, ((short?[])o).Select(i => i.ToString()));
                    }
                    break;
                case DocRecordColumnType.Int:
                    if(o is int[])
                    {
                        return string.Join(ArrayDelimiter, ((int[])o).Select(i => i.ToString()));
                    }
                    if (o is int?[])
                    {
                        return string.Join(ArrayDelimiter, ((int?[])o).Select(i => i.ToString()));
                    }
                    break;
                case DocRecordColumnType.Bigint:
                    if (o is long[])
                    {
                        return string.Join(ArrayDelimiter, ((long[])o).Select(i => i.ToString()));
                    }
                    if (o is long?[])
                    {
                        return string.Join(ArrayDelimiter, ((long?[])o).Select(i => i.ToString()));
                    }
                    break;
                case DocRecordColumnType.Bool:
                    if (o is bool[])
                    {
                        return string.Join(ArrayDelimiter, ((bool[])o).Select(i => i.ToString()));
                    }
                    if (o is bool?[])
                    {
                        return string.Join(ArrayDelimiter, ((bool?[])o).Select(i => i.ToString()));
                    }
                    break;
                default:
                    if (o is string[])
                    {
                        return string.Join(ArrayDelimiter, (string[])o);
                    }
                    break;

            }
            s = o.ToString();
            if (NullIfEmpty && string.IsNullOrEmpty(s))
                return null;
            return s;
        }
        /// <summary>
        /// Check if the object's type is compatible with the column meta data.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public bool CompareDataType(object o)
        {
            var tInfo = o.GetType();
            if (Array )
            {
                if(tInfo.IsArray || o is System.Collections.IList)
                    tInfo = tInfo.GetElementType();
                else
                {
                    return false;
                }
                
            }
            tInfo = Nullable.GetUnderlyingType(tInfo) ?? tInfo;
            var tt = Type.GetTypeCode(tInfo);
            
            switch (DataType)
            {
                case DocRecordColumnType.NUL:
                    if (o == null || o == DBNull.Value)
                        return true;
                    return false;
                case DocRecordColumnType.Int:
                case DocRecordColumnType.Smallint:
                case DocRecordColumnType.Bigint:
                case DocRecordColumnType.Tinyint:
                    return tt.In(TypeCode.Int32, TypeCode.Int16, TypeCode.Int64, TypeCode.Byte);
                case DocRecordColumnType.Money:
                case DocRecordColumnType.Decimal:
                    return tt == TypeCode.Decimal;
                case DocRecordColumnType.Date:
                case DocRecordColumnType.DateTime:
                    return tt == TypeCode.DateTime;
                case DocRecordColumnType.Double:
                    return tt == TypeCode.Double;
                case DocRecordColumnType.Bool:
                    return tt == TypeCode.Boolean;
                default:
                    return true;
            }
        }
        

        /// <summary>
        /// Tries to get the value as a variable of type <typeparamref name="T"/>.
        /// <para>Return value indicates whether record was successfully parsed.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="Result"></param>
        /// <returns></returns>
        public bool TryEvaluate<T>(string value, out T Result)
        {
            
            object o;
            if (TryGet(value, out o))
            {
                if (o is T)
                {
                    Result = (T)o;
                    return true;
                }
                if (o == null)
                {
                    Result = default;
                    if (default(T) != null)
                        return false;
                    return true;
                }
                if (Nullable.GetUnderlyingType(typeof(T)) == o.GetType())
                {
                    Result = (T)Convert.ChangeType(o, typeof(T));
                    return true;
                }
                if (Nullable.GetUnderlyingType(o.GetType()) == typeof(T))
                {
                    if (o == null)
                        Result = default;
                    else
                        Result = (T)o;
                    return true;
                }
            }
            Result = default;
            return false;
        }      
        public bool TryGetArray(string val, out object result)
        {
            var source = val.SplitByKeyword(ArrayDelimiter);
            var t = new List<object>();
            foreach(var s in source)
            {
                object o;
                if (TryGet(s, out o, true))
                    t.Add(o);
                else
                {
                    result = null;
                    return false;
                }
            }
            result = t.ToArray();
            return true;
        }
        /// <summary>
        /// Tries to parse out the data from the value, based on Column data type.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="result"></param>
        /// <param name="fromArray">Indicates that the caller is trying to get individual items from the array.</param>
        /// <returns></returns>
        public bool TryGet(string val, out object result, bool fromArray = false)
        {
            if(Array && !fromArray)
            {
                return TryGetArray(val, out result);
            }
            bool nulVal = false;
            if (val == null || NullIfEmpty && string.IsNullOrWhiteSpace(val))
                nulVal = true;
            else if (!DataType.In(DocRecordColumnType.Varchar, DocRecordColumnType.NVarchar, DocRecordColumnType.Unknown))
                val = val.Trim();
            bool success = false;
            switch (DataType)
            {
                case DocRecordColumnType.Unknown:
                case DocRecordColumnType.Varchar:
                case DocRecordColumnType.NVarchar:
                    result = val;
                    return true;
                case DocRecordColumnType.Tinyint:
                    {
                        if (nulVal)
                        {
                            result = null as byte?;
                            return true;
                        }
                        byte b;
                        success = byte.TryParse(val, out b);
                        result = b;
                        break;
                    }
                case DocRecordColumnType.Smallint:
                    if (nulVal)
                    {
                        result = null as short?;
                        return true;
                    }
                    short s;
                    success = short.TryParse(val, out s);
                    result = s;
                    break;
                case DocRecordColumnType.Bool:
                    if (nulVal)
                    {
                        result = null as bool?;
                        return true;
                    }
                    bool br;
                    success = bool.TryParse(val, out br);
                    if (!success)
                    {
                        if (val.ToUpper().In("YES", "Y"))
                        {
                            result = true;
                            return true;
                        }
                        if (val.ToUpper().In("NO", "N"))
                        {
                            result = false;
                            return true;
                        }
                    }

                    result = br;
                    break;
                case DocRecordColumnType.Int:
                    if (nulVal)
                    {
                        result = null as int?;
                        return true;
                    }
                    int i;
                    success = int.TryParse(val, out i);
                    result = i;
                    break;
                case DocRecordColumnType.Bigint:
                    if (nulVal)
                    {
                        result = null as long?;
                        return true;
                    }
                    long l;
                    success = long.TryParse(val, out l);
                    result = l;
                    break;
                case DocRecordColumnType.Double:
                    if (nulVal)
                    {
                        result = null as double?;
                        return true;
                    }
                    double dbl;
                    success = double.TryParse(val, out dbl);
                    result = dbl;
                    break;
                case DocRecordColumnType.Money:
                    if (nulVal)
                    {
                        result = null as decimal?;
                        return true;
                    }

                    var cInfo = System.Globalization.CultureInfo.CurrentCulture;
                    bool neg = val.Contains(cInfo.NumberFormat.NegativeSign);
                    var regex = new System.Text.RegularExpressions.Regex("[^0-9" + cInfo.NumberFormat.NumberDecimalSeparator + "]+");
                    string parse = regex.Replace(val, string.Empty);
                    decimal m;
                    success = decimal.TryParse(parse, out m);
                    if (success)
                    {
                        if (neg)
                            result = -1 * m;
                        else
                            result = m;
                    }
                    else
                        result = 0M;
                    break;
                case DocRecordColumnType.Decimal:
                    if (nulVal)
                    {
                        result = null as decimal?;
                        return true;
                    }
                    decimal d;
                    success = decimal.TryParse(val, out d);
                    result = d;
                    break;
                case DocRecordColumnType.Date:
                case DocRecordColumnType.DateTime:
                    if (nulVal)
                    {
                        result = null as DateTime?;
                        return true;
                    }
                    DateTime dt = default;
                    string format = Format;
                    if (string.IsNullOrWhiteSpace(format))
                    {
                        if (!DateConverter.GuessFormatDateTime(val, out format))
                        {
                            format = null;
                            success = DateTime.TryParse(val, out dt); //Try parse with base DateTime method to be safe.
                        }
                        else
                            Format = format;
                    }
                    if (format != null)
                    {
                        success = DateTime.TryParseExact(val, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AllowInnerWhite, out dt);
                    }
                    result = dt;
                    return success;
                default:
                    result = val;
                    return false;
            }
            return success;
        }
    }
}
