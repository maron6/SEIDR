using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// Document column information for doc reader/writer
    /// </summary>
    public class DocRecordColumnCollection : IEnumerable<DocRecordColumnInfo>
    {
        #region static Class mapping utility methods
        /// <summary>
        /// Creates a column collection from a class's properties.
        /// </summary>
        /// <param name="toParse"></param>
        /// <returns></returns>
        public static DocRecordColumnCollection ParseFromType(Type toParse)
        {            
            DocRecordColumnCollection ret = new DocRecordColumnCollection(toParse.Name);
            foreach(var prop in toParse.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                DocRecordColumnType t = DocRecordColumnType.Unknown;
                Type propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                switch (propType.Name.ToUpper())
                {
                    case "STRING":
                        t = DocRecordColumnType.Varchar;
                        break;
                    case "DATETIME":
                        t = DocRecordColumnType.DateTime;
                        break;
                    case "LONG":
                    case "INT64":
                        t = DocRecordColumnType.Bigint;
                        break;
                    case "INT":
                    case "INT32":
                        t = DocRecordColumnType.Int;
                        break;
                    case "SHORT":
                    case "INT16":
                        t = DocRecordColumnType.Smallint;
                        break;
                    case "BYTE":
                        t = DocRecordColumnType.Tinyint;
                        break;
                    case "BOOL":
                    case "BOOLEAN":
                        t = DocRecordColumnType.Bool;
                        break;
                    case "DECIMAL":
                        t = DocRecordColumnType.Decimal;
                        break;
                    default:
                        t = DocRecordColumnType.Unknown;
                        break;
                }                
                ret.AddColumn(prop.Name, dataType:t);
            }
            return ret;
        }
        #endregion
        string textQualifier = null;
        [Obsolete("Use TextQualifier in MetaDataBase", true)]
        /// <summary>
        /// Text qualifier
        /// </summary>
        public string TextQualifier
        {
            get { return textQualifier; }
            set
            {
                textQualifier = value ;
                SetFormat();
            }
        } 

        /// <summary>
        /// Merges the two column collections and returns a new collection with the specified alias
        /// </summary>
        /// <param name="newAlias"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static DocRecordColumnCollection Merge(string newAlias, DocRecordColumnCollection left, DocRecordColumnCollection right)
        {
            var ret = Merge(left, right);
            if (newAlias != null)
                ret.Alias = newAlias;
            return ret;
        }
        /// <summary>
        /// Default value for new columns' <see cref="DocRecordColumnInfo.NullIfEmpty"/>. Default is true. Ignored when writing.
        /// </summary>
        public bool DefaultNullIfEmpty { get; set; } = true;
        /// <summary>
        /// Merges the two column collections
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>        
        /// <returns></returns>
        public static DocRecordColumnCollection Merge(DocRecordColumnCollection left, DocRecordColumnCollection right)
        {
            var ret = new DocRecordColumnCollection();/*
            {
                fixedWidthMode = left.FixedWidthMode,
                _Delimiter = left._Delimiter
            };*/
            var rCols = (from col in left.Columns
                         select col
                         ).OrderBy(c => c.Position);
            foreach (var col in rCols)
            {
                ret.CopyColumnIntoCollection(col);                
            }
            rCols = (from col in right.Columns
                         select col
                         ).OrderBy(c => c.Position);                        
            foreach (var col in rCols)
            {
                ret.CopyColumnIntoCollection(col);
            } 
            return ret;
        }
        /// <summary>
        /// Number of columns in the collection
        /// </summary>
        public int Count => Columns.Count;
        /// <summary>
        /// If in fixed width mode, returns the max size of a record, based on the widths of columns.
        /// <para>May return -1 if one of the columns is not valid for fixed width mode (<see cref="Valid"/> will be false)</para>
        /// </summary>
        public int MaxLength
        {
            get
            {
                CheckForFixedWidthValid();
                if (!CanUseAsFixedWidth || !Valid) //CanuseAsFixWidth -> All columns need a length.
                    return -1;
                int x = 0;
                foreach (var col in Columns)
                {
                    x += col.MaxLength.Value;
                }
                return x;
            }
        }
        #region Parsing
      
        /// <summary>
        /// Parses a DocRecord out of the string. Assumes that the string does not contain a line ending delimiter(E.g., \r)
        /// </summary>
        /// <param name="writeMode"></param>
        /// <param name="record"></param>
        /// <param name="FixWidth"></param>
        /// <param name="VariableWidth"></param>
        /// <param name="Delimiter"></param>
        /// <param name="TextQualifier"></param>
        /// <param name="AllowMissingColumns"></param>
        /// <param name="ThrowExceptionColumnCountMismatch"></param>
        /// <returns></returns>
        public DocRecord ParseRecord(bool writeMode, string record, 
            bool FixWidth = false, bool VariableWidth = false, char? Delimiter = '|', string TextQualifier = "\"",
            bool AllowMissingColumns = false, bool ThrowExceptionColumnCountMismatch = true)
        {
            if (string.IsNullOrEmpty(record))
                return null;
            if (!Valid)
                throw new InvalidOperationException("Collection state is not valid.");
            if (!FixWidth && Delimiter.HasValue == false)
                throw new ArgumentNullException(nameof(Delimiter), "Delimiter must be populated if not using FixWidth");
            string[] split = new string[LastPosition];
            int position = 0;
            for (int i = 0; i < Columns.Count; i++)
            {
                if (position >= record.Length)
                {
                    //have gone beyond length of record
                    if (ThrowExceptionColumnCountMismatch)
                        throw new MissingColumnException(i, Columns.Count - 1);
                    break;
                }
                if (FixWidth)
                {
                    int x = Columns[i].MaxLength.Value;
                    if (x + position > record.Length)
                        x = record.Length - position; //Number of characters to read                                        
                    split[i] = record.Substring(position, x);
                    position += x;
                    if (ThrowExceptionColumnCountMismatch && i == Columns.Count - 1 && position < record.Length)
                        throw new ColumnOverflowException(record.Length - position, Columns.Count, record.Length);
                }
                else if (VariableWidth) //Almost like delimited mode, but columns have a max length..
                {
                    int x = record.IndexOf(Delimiter.Value, position) - position;
                    int y = Columns[i].MaxLength ?? x;
                    if (y < x || x < 0)
                    {
                        x = y;
                        y = 0;
                    }
                    else
                        y = 1; //Skip delimiter          
                    if (y < 0)
                    {
                        if (i == Columns.Count - 1)
                        {
                            split[i] = record.Substring(position);
                            break;
                        }
                        throw new VariableColumnNotFoundException(record.Length - position, i, Columns.Count, record.Length, Delimiter.Value);
                    }
                    if (x + position > record.Length)
                        x = record.Length - position; //Number of characters to read                                                                
                    split[i] = record.Substring(position, x);
                    position += x + y;
                    if (ThrowExceptionColumnCountMismatch && i == Columns.Count - 1 && position < record.Length)
                        throw new ColumnOverflowException(record.Length - position, Columns.Count, record.Length);
                }
                else
                {
                    split = record.SplitOutsideQuotes(Delimiter.Value, TextQualifier);
                    if (ThrowExceptionColumnCountMismatch)
                    {
                        if (split.Length < Columns.Count)
                        {
                            if (!AllowMissingColumns)
                                throw new MissingColumnException(split.Length, Columns.Count);
                        }
                        else if (split.Length > Columns.Count)
                            throw new ColumnOverflowException(split.Length, Columns.Count);
                    }
                    break;
                }

            }
            DocRecord r = new DocRecord(this, writeMode, split);
            return r;
        }
       
        #endregion
        /// <summary>
        /// Changes the <see cref="DocRecordColumnInfo.ColumnName"/> of the associated column.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="NewName"></param>
        /// <returns></returns>
        public DocRecordColumnCollection RenameColumn(int position, string NewName)
        {
            Columns[position].ColumnName = NewName;
            return this;
        }

        /// <summary>
        /// Creates a basic set up
        /// </summary>
        public DocRecordColumnCollection(string Alias)
        {
            Columns = new List<DocRecordColumnInfo>();
            //_Delimiter = '|';
            //fixedWidthMode = false;
            this.Alias = Alias;
        }
        /// <summary>
        /// Checks for the index of the column
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public int IndexOf(DocRecordColumnInfo column)
            => Columns.IndexOf(column);


        /// <summary>
        /// Attempts to set up for fixed width columns with prepopulated list
        /// </summary>
        /// <param name="columns"></param>
        public DocRecordColumnCollection(List<DocRecordColumnInfo> columns)
        {
            Columns = new List<DocRecordColumnInfo>(columns);
            CheckForFixedWidthValid();
            //LastPosition = columns.Max(c => c.Position);
            //SetFormat();
        }
        /// <summary>
        /// Create Collection from list of columns
        /// </summary>
        /// <param name="cols"></param>
        public DocRecordColumnCollection(params DocRecordColumnInfo[] cols) : this(new List<DocRecordColumnInfo>(cols)) { }

        /// <summary>
        /// True if the collection is valid for use as FixedWidth
        /// </summary>
        public bool CanUseAsFixedWidth { get; private set; } = true;
        internal List<DocRecordColumnInfo> Columns { get; private set; }
   
        [Obsolete("Formatting to MetaData", true)]
        internal string format;
        [Obsolete("Move Formatting to MetaData that will be used for reading or writing instead of column collection.", true)]
        internal void SetFormat()
        {
            if (!Valid)
                return;

            Columns.Sort((a, b) =>
            {
                return a.Position.CompareTo(b.Position);
            });
            int last = LastPosition;
            StringBuilder fmt = new StringBuilder();
            for (int i = 0; i <= last; i++)
            {
                var col = this[i];
                if (col == null)
                {
                    //fmt.Append(_Delimiter);
                    continue;
                }
                if (false)// fixedWidthMode)
                {
                    int justify = col.LeftJustify ? -col.MaxLength.Value : col.MaxLength.Value;
                    fmt.Append("{" + i + "," + justify + "}");
                }
                else
                {
                    if (col.TextQualify)
                        fmt.Append(TextQualifier);

                    fmt.Append("{" + i + "}");

                    if (col.TextQualify)
                        fmt.Append(TextQualifier);
                    //if (i < last)
                    //    fmt.Append(_Delimiter);
                }
            }
            //fmt.Append(LineEndDelimiter);
            format = fmt.ToString();
        }
        internal void CheckSort()
        {
            Columns.Sort((a, b) =>
            {
                return a.Position.CompareTo(b.Position);
            });
        }
   
    
        /// <summary>
        /// Usable for doc writing/reading
        /// </summary>
        public bool Valid
        {
            get
            {
                return Columns.Count > 0
                    && LastPosition >= 0;
            }
        }
        /// <summary>
        /// For use with Fix-width mode. Requires a line end delimiter.
        /// </summary>
        public bool RaggedRight { get; set; } = false;
        /// <summary>
        /// Default alias for new columns
        /// </summary>
        public string Alias { get; set; }
        /// <summary>
        /// Grabs the first column whose column name matches, and whose alias matches <see cref="Alias"/>
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <returns></returns>
        public DocRecordColumnInfo this[string ColumnName]
            => this[Alias, ColumnName];
        /// <summary>
        /// Access Column info for specific column/alias
        /// </summary>
        /// <param name="SpecificAlias"></param>
        /// <param name="ColumnName"></param>
        /// <param name="Position">Physical position</param>
        /// <returns></returns>
        public DocRecordColumnInfo this[string SpecificAlias, string ColumnName, int Position = -1]
        {
            get
            {
                return Columns.First(c => c.OwnerAlias == SpecificAlias && c.ColumnName == ColumnName && (Position < 0 || c.Position == Position) );
            }            
        }
        /// <summary>
        /// Attempt to get the best match for the column.
        /// </summary>
        /// <param name="Column"></param>
        /// <param name="alias"></param>
        /// <param name="position">Optional position filter</param>
        /// <returns></returns>
        public DocRecordColumnInfo GetBestMatch(string Column, string alias = null, int position = -1)
        {
            return Columns.FirstOrDefault(c => (c.OwnerAlias == alias || alias == null) && c.ColumnName == Column && (position < 0 || c.Position == position)) 
                ?? Columns.FirstOrDefault(c => c.ColumnName == Column && (position < 0 || c.Position == position));
        }
        /// <summary>
        /// Validates that the column is considered a member of this collection.
        /// </summary>
        /// <param name="columnInfo"></param>
        /// <returns></returns>
        public bool HasColumn(DocRecordColumnInfo columnInfo)
        {
            return Columns.Contains(columnInfo);
        }
        /// <summary>
        /// Check if a column exists that matches
        /// </summary>
        /// <param name="SpecificAlias"></param>
        /// <param name="ColumnName"></param>
        /// <param name="Position"></param>
        /// <returns></returns>
        public bool HasColumn(string SpecificAlias, string ColumnName, int Position = -1)
        {

            return Columns.Exists(c => (c.OwnerAlias == SpecificAlias || SpecificAlias == null) && c.ColumnName == ColumnName && (Position < 0 || c.Position == Position));
        }
        /// <summary>
        /// Access columns by position. 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public DocRecordColumnInfo this[int index]
        {
            get
            {
                return Columns.FirstOrDefault(c => c.Position == index);
            }/*
            set
            {
                Columns[index] = value;
            }*/
        }
        /// <summary>
        /// Adds a new column and returns its index
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="MaxSize"></param>        
        /// <param name="leftJustify">Indicates if column should be left justified in fix width mode</param>
        /// <param name="textQualify">Indicates whether the column should be text qualified when writing.</param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public DocRecordColumnInfo AddColumn(string ColumnName, int? MaxSize = null, bool leftJustify = true, bool textQualify = false, 
            DocRecordColumnType dataType = DocRecordColumnType.Unknown)
        {
            var col = new DocRecordColumnInfo(ColumnName, Alias, LastPosition + 1)
            {
                MaxLength = MaxSize,                
                NullIfEmpty = DefaultNullIfEmpty,
                LeftJustify = leftJustify,
                TextQualify = textQualify,
                DataType = dataType
            };
            Columns.Add(col);
            if (MaxSize == null)            
                CanUseAsFixedWidth = false;

            Columns.Sort((a, b) =>
            {
                return a.Position.CompareTo(b.Position);
            });
            //SetFormat();
            return col;
        }

        internal void RemoveColumn(string alias, string ColumnName, int position = -1)
        {
            if (!HasColumn(alias, ColumnName, position))
                return;
            var c = GetBestMatch(ColumnName, alias, position);

            int p = Columns.IndexOf(c) + 1;
            int l = Columns.Count;
            for (int i = p; i < l; i++)
            {                
                Columns[i].Position -= 1; //move position down by one for removed column.
            }
            Columns.Remove(c);
            CheckForFixedWidthValid();
            //SetFormat();
        }
        internal void ClearColumns()
        {
            Columns.Clear();
            CanUseAsFixedWidth = true;
        }
        internal void RemoveColumn(DocRecordColumnInfo toRemove)
        {
            if (!HasColumn(toRemove))
                return;            
            int p = Columns.IndexOf(toRemove) + 1;
            int l = Columns.Count;
            for (int i = p; i < l; i++)
            {                                
                Columns[i].Position -= 1; //move position down by one for removed column.
            }
            Columns.Remove(toRemove);
            CheckForFixedWidthValid();
            //SetFormat();
        }
        public DocRecordColumnInfo CopyColumnIntoCollection(DocRecordColumnInfo toCopy)
        {
            var col = new DocRecordColumnInfo(toCopy.ColumnName, toCopy.OwnerAlias, LastPosition + 1)
            {
                MaxLength = toCopy.MaxLength,                
                NullIfEmpty = toCopy.NullIfEmpty,
                LeftJustify = toCopy.LeftJustify,
                TextQualify = toCopy.TextQualify,         
                DataType = toCopy.DataType
            };
            Columns.Add(col);

            if (toCopy.MaxLength == null)
                CanUseAsFixedWidth = false;
            
            //SetFormat();
            return col;
        }
        /// <summary>
        /// Updates the column information specified by column name, under this collection's <see cref="Alias"/>
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="newName"></param>
        /// <param name="MaxSize"></param>        
        /// <param name="nullIfEmpty">If set, overrides the column value. If null, leaves the column's value alone</param>
        public void UpdateColumn(string ColumnName, string newName, int? MaxSize, bool? nullIfEmpty = null)
            => UpdateColumn(Alias, ColumnName, newName, MaxSize, DefaultNullIfEmpty);
        /// <summary>
        /// Updates the column information specified by the Alias/column Name
        /// </summary>
        /// <param name="Alias"></param>
        /// <param name="ColumnName"></param>
        /// <param name="newName"></param>
        /// <param name="MaxSize"></param>        
        /// <param name="NullIfEmpty">If set, overrides the column value. If null, leaves the column's value alone</param>
        /// <param name="dataType">Data Type of column</param>
        public void UpdateColumn(string Alias, string ColumnName, string newName, int? MaxSize, bool? NullIfEmpty = null, DocRecordColumnType? dataType = null)
        {
            var col = this[Alias, ColumnName];
            col.ColumnName = newName;
            col.MaxLength = MaxSize;
            col.NullIfEmpty = NullIfEmpty ?? col.NullIfEmpty;
            col.DataType = dataType ?? col.DataType;
            if (MaxSize != null)
                CheckForFixedWidthValid();
            else
                CanUseAsFixedWidth = false;
            //SetFormat();
        }
        /// <summary>
        /// Last position value in the underlying columns, for looping.
        /// </summary>
        public int LastPosition => Columns.Count == 0? -1 : Columns.Max(c => c.Position);
        /// <summary>
        /// Adds the set up column to the collection
        /// </summary>
        /// <param name="column"></param>
        /// <returns>Position of the column.</returns>
        public int AddColumn(DocRecordColumnInfo column)
        {
            if(column.Position >= 0)
            {
                int last = LastPosition;
                int np = column.Position;
                for(int i = np; i <= last; i++)
                {
                    if (i > Columns.Count)
                        break;
                    if (Columns[i].Position > i)
                        break;
                    Columns[i].Position = i + 1;
                }
                if (np >= Columns.Count)
                {
                    Columns.Add(column);
                    Columns.Sort((a, b) =>
                    {
                        return a.Position.CompareTo(b.Position);
                    });
                }
                else
                    Columns.Insert(np, column);
            }
            else
            {
                column.Position = LastPosition + 1;
                Columns.Add(column);
            }
            if (column.MaxLength == null)
                CanUseAsFixedWidth = false;
            //SetFormat();
            return column.Position;
        }
        /// <summary>
        /// Adds a mapping of a source column to a column in this collection (Best Match)
        /// <para>Returns 'this' for method chaining.</para>
        /// </summary>
        /// <param name="map"></param>
        /// <param name="source"></param>
        /// <param name="ColumnName"></param>
        /// <param name="alias"></param>
        /// <returns></returns>
        public DocRecordColumnCollection AddMapping(Dictionary<int, DocRecordColumnInfo>  map, DocRecordColumnInfo source,string ColumnName, string alias = null)
        {
            DocRecordColumnInfo temp = GetBestMatch(ColumnName, alias);
            map[temp.Position] = source;
            return this;
        }
        /// <summary>
        /// Checks if the object can be used for fixed width. 
        /// <para>Should be used if setting column information via indexers</para>
        /// </summary>
        public void CheckForFixedWidthValid()
        {
            foreach(var col in Columns)
            {
                if(col.MaxLength == null)
                {
                    CanUseAsFixedWidth = false;
                    return;
                }
            }
            CanUseAsFixedWidth = true;
        }        
        public IEnumerator<DocRecordColumnInfo> GetEnumerator()
        {            
            return Columns.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Columns.GetEnumerator();
        }
    }
}
