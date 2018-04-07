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

        /// <summary>
        /// Text qualifier
        /// </summary>
        public char TextQualifier = '"';
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
            if(newAlias != null)
                ret.Alias = newAlias;            
            return ret;            
        }
        /// <summary>
        /// Default value for new columns' <see cref="DocRecordColumnInfo.NullIfEmpty"/>. Default is true
        /// </summary>
        public bool NullIfEmpty { get; set; } = true;
        /// <summary>
        /// Merges the two column collections
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static DocRecordColumnCollection Merge(DocRecordColumnCollection left, DocRecordColumnCollection right)
        {
            var ret = new DocRecordColumnCollection(left.Columns);
            foreach(var col in right)
            {
                ret.AddColumn(col);
            }
            ret._Delimiter = left._Delimiter;
            ret.fixedWidthMode = left.fixedWidthMode;
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
                if (!fixedWidthMode || !Valid)
                    return -1;
                int x = 0;
                foreach (var col in Columns)
                {
                    x += col.MaxLength.Value;
                }
                return x;
            }
        }
        /// <summary>
        /// Parses a DocRecord out of the string. The string should end at <see cref="LineEndDelimiter"/>, but not include it.
        /// </summary>
        /// <param name="writeMode"></param>
        /// <param name="record"></param>
        /// <returns></returns>
        public DocRecord ParseRecord(bool writeMode, string record)
        {
            if (string.IsNullOrEmpty(record))
                return null;
            if (!Valid)
                throw new InvalidOperationException("Collection state is not valid.");
            string[] split = new string[Columns.Count];            
            int position = 0;            
            for(int i= 0; i < Columns.Count; i++)
            {
                if (position >= record.Length)
                {
                    //have gone beyond length of record
                    if (ThrowExceptionColumnCountMismatch)                    
                        throw new MissingColumnException(i, Columns.Count - 1);                                                                
                    break;
                }
                if (fixedWidthMode)
                {
                    int x = Columns[i].MaxLength.Value;
                    if (x + position > record.Length)
                        x = record.Length - position; //Number of characters to read
                    int nextPosition = record.IndexOf(Columns[i].EarlyTerminator);
                    if (nextPosition < 0)
                    {

                        split[i] = record.Substring(position, x);
                        //break;
                    }
                    if (nextPosition - position < x)
                        x = nextPosition - position;
                    split[i] = record.Substring(position, x);
                    position += x;
                    if(ThrowExceptionColumnCountMismatch && i == Columns.Count - 1 && position < record.Length)                   
                        throw new ColumnOverflowException(record.Length - position, Columns.Count, record.Length);                    
                }
                else
                {
                    split = record.SplitOutsideQuotes(Delimiter.Value, TextQualifier);
                    if (ThrowExceptionColumnCountMismatch)
                    {
                        if (split.Length < Columns.Count)
                            throw new MissingColumnException(split.Length, Columns.Count);
                        else if (split.Length > Columns.Count)
                            throw new ColumnOverflowException(split.Length, Columns.Count);
                    }
                    break;
                    /*
                    int nextPosition = record.IndexOf(Delimiter.Value, position);
                    int text = record.IndexOf(TextQualifier);
                    bool odd = true;
                    while(text > nextPosition)
                    {
                        /*
                         * If not odd, look for the next delimiter after the current quote.
                         * If odd, look for the next quote.
                         * If not odd compare the position of next delimiter against quote...
                         * ( when we get to even, the next delimiter should be before the next quote. 
                         *      If not, need to search more)
                         * 
                         * * /
                        if (!odd)
                            nextPosition = record.IndexOf(Delimiter.Value, text);
                        text = record.IndexOf(Delimiter.Value, text + 1);
                        //if(odd)
                        //    text = 
                    }
                    if (text >= 0 && text < nextPosition)
                    {
                        nextPosition = record.IndexOf(TextQualifier, TextQualifier);

                    }
                    if (nextPosition < 0)
                    {
                        split[i] = record.Substring(position);
                        break;
                    }
                    split[i] = record.Substring(position, nextPosition - position);
                    position = nextPosition + 1;//Skip the delimiter
                    */
                }
                
            }
            DocRecord r = new DocRecord(this, writeMode, split);
            return r;
        }

        /// <summary>
        /// If true, throws an exception if the size of a record is too big or too small, based on number of records.
        /// <para>If false, ignores extra columns, and missing columns are treated as null</para>
        /// </summary>
        public bool ThrowExceptionColumnCountMismatch { get; set; } = true;        
        /// <summary>
        /// Creates a basic set up
        /// </summary>
        public DocRecordColumnCollection(string Alias)
        {
            Columns = new List<DocRecordColumnInfo>();
            //_Delimiter = '|';
            fixedWidthMode = false;
            this.Alias = Alias;
        }
        /// <summary>
        /// Checks for the index of the column
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public int IndexOf(DocRecordColumnInfo column)
            =>Columns.IndexOf(column);
        
        /// <summary>
        /// Sets up for specified delimiter and pre-populated column list
        /// </summary>
        /// <param name="Delimiter"></param>
        /// <param name="columns"></param>
        public DocRecordColumnCollection(char Delimiter, IList<DocRecordColumnInfo> columns)
        {
            Columns = columns;
            CheckForFixedWidthValid();
            _Delimiter = Delimiter;
            fixedWidthMode = false;
        }
        /// <summary>
        /// Attempts to set up for fixed width columns with prepopulated list
        /// </summary>
        /// <param name="columns"></param>
        public DocRecordColumnCollection(IList<DocRecordColumnInfo> columns)
        {
            Columns = columns;
            CheckForFixedWidthValid();
            if (canFixedWidth)
                fixedWidthMode = true;
        }
        public DocRecordColumnCollection(params DocRecordColumnInfo[] cols): this((IList<DocRecordColumnInfo>)cols) { }
        public DocRecordColumnCollection(char Delim, params DocRecordColumnInfo[] cols)
            :this(Delim, (IList<DocRecordColumnInfo>) cols){}
        bool canFixedWidth = true;
        /// <summary>
        /// True if the collection is valid for use as FixedWidth
        /// </summary>
        public bool CanUseAsFixedWidth
            => canFixedWidth;
        internal IList<DocRecordColumnInfo> Columns { get; private set; }
        char? _Delimiter = null;
        /// <summary>
        /// Use in delimited mode. If not set, uses Environment.NewLine. 
        /// <para>Last column in fixed width will not end early on reaching the value.</para>
        /// </summary>
        public string LineEndDelimiter { get; set; } = Environment.NewLine;
        bool fixedWidthMode = false;
        /// <summary>
        /// Sets whether the columns should be used for fixed width or delimited
        /// </summary>
        public bool FixedWidthMode
        {
            get
            {
                return fixedWidthMode;
            }
            set
            {
                CheckForFixedWidthValid();
                if (value)
                {
                    if (canFixedWidth)
                        fixedWidthMode = value;
                    else
                        throw new InvalidOperationException("Collection set up is not valid for fixed width. Make sure all columns have a max size");
                }
                else
                {
                    if (string.IsNullOrEmpty(LineEndDelimiter))
                        throw new InvalidOperationException(nameof(LineEndDelimiter) + " must have a value in Delimited mode.");
                    if (_Delimiter == null)
                        throw new InvalidOperationException("Collection set up is not valid for Delimited. Make sure to set the delimiter");
                    if (LineEndDelimiter == _Delimiter.ToString())
                        throw new InvalidOperationException("Column delimiter cannot match the line end delimiter");
                    fixedWidthMode = false;                    
                }
            }
        }
        /// <summary>
        /// Sets the delimiter to thenew character
        /// </summary>
        /// <param name="Delimiter"></param>
        public void SetDelimiter(char Delimiter) => _Delimiter = Delimiter;
        /// <summary>
        /// Removes the delimiter. Will not allow collection to be used for delimited
        /// </summary>
        public void RemoveDelimiter() => _Delimiter = null;
        /// <summary>
        /// Specifies the character that should separate characters
        /// </summary>
        public char? Delimiter => _Delimiter;
        /// <summary>
        /// Usable for doc writing/reading
        /// </summary>
        public bool Valid
        {
            get
            {
                return Columns.Count > 0 
                    && ( 
                        (fixedWidthMode && CanUseAsFixedWidth)
                        .Or(!fixedWidthMode && _Delimiter != null && _Delimiter.ToString() != LineEndDelimiter)
                    );
            }
        }        
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
        /// <returns></returns>
        public DocRecordColumnInfo GetBestMatch(string Column, string alias = null)
        {
            return Columns.FirstOrDefault(c => (c.OwnerAlias == alias || alias == null) && c.ColumnName == Column) ?? Columns.FirstOrDefault(c => c.ColumnName == Column);
        }
        /// <summary>
        /// Access columns by index. Also the indexer that allows updating columns.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public DocRecordColumnInfo this[int index]
        {
            get
            {
                return Columns[index];
            }
            set
            {
                Columns[index] = value;
            }
        }
        /// <summary>
        /// Adds a new column and returns its index
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="MaxSize"></param>
        /// <param name="EarlyTerminator">For use with fixed width. Allows ending the column early. E.g. NewLine</param>
        /// <returns></returns>
        public int AddColumn(string ColumnName, int? MaxSize = null, string EarlyTerminator = null)
        {            
            Columns.Add(new DocRecordColumnInfo(ColumnName, Alias, Columns.Count)
            {
                MaxLength = MaxSize,
                EarlyTerminator = EarlyTerminator,
                NullIfEmpty = NullIfEmpty
            });

            if (MaxSize == null)
                canFixedWidth = false;
            return Columns.Count;
        }
        /// <summary>
        /// Updates the column information specified by column name, under this collection's <see cref="Alias"/>
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="newName"></param>
        /// <param name="MaxSize"></param>
        /// <param name="EarlyTerminator"></param>
        /// <param name="nullIfEmpty">If set, overrides the column value. If null, leaves the column's value alone</param>
        public void UpdateColumn(string ColumnName, string newName, int? MaxSize, string EarlyTerminator, bool? nullIfEmpty = null)
            => UpdateColumn(Alias, ColumnName, newName, MaxSize, EarlyTerminator, NullIfEmpty);
        /// <summary>
        /// Updates the column information specified by the Alias/column Name
        /// </summary>
        /// <param name="Alias"></param>
        /// <param name="ColumnName"></param>
        /// <param name="newName"></param>
        /// <param name="MaxSize"></param>
        /// <param name="EarlyTerminator"></param>
        /// <param name="NullIfEmpty">If set, overrides the column value. If null, leaves the column's value alone</param>
        public void UpdateColumn(string Alias, string ColumnName, string newName, int? MaxSize, string EarlyTerminator, bool? NullIfEmpty = null)
        {
            var col = this[Alias, ColumnName];
            col.ColumnName = newName;
            col.MaxLength = MaxSize;
            col.EarlyTerminator = EarlyTerminator;
            col.NullIfEmpty = NullIfEmpty ?? col.NullIfEmpty;
            if (MaxSize != null)
                CheckForFixedWidthValid();
            else
                canFixedWidth = false;
        }
        /// <summary>
        /// Adds the set up column to the collection
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public int AddColumn(DocRecordColumnInfo column)
        {
            Columns.Add(column);
            if (column.MaxLength == null)
                canFixedWidth = false;
            return Columns.Count;
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
                    canFixedWidth = false;
                    return;
                }
            }
            canFixedWidth = true;
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
