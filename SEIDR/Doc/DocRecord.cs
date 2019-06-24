using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// Read/write class for use with DocQueries, DocReader, and DocWriter
    /// </summary>
    public class DocRecord: DynamicObject, IRecord, IDataRecord
    {
        #region hashing
        static ulong GetRollingHash(string content)
        {
            var m = ulong.MaxValue - 2ul * char.MaxValue;
            ulong l;
            var h = l = 0ul;
            unchecked
            {
                foreach (char s in content)
                {
                    h = (h + (l * s) % m) % m + char.MaxValue + s;

                    if (l > m)
                        l = 0;
                    else
                        l++;
                }
            }
            return h;
        }
        /// <summary>
        /// Returns a ulong value of all columns
        /// </summary>
        public ulong FullRecordHash
        {
            //get { return unchecked((ulong)ToString().GetHashCode()); }
            get
            {
                return GetPartialHash(true, true, Columns.Columns.ToArray()) ?? 0;
            }
        }
        /// <summary>
        /// Returns an unsigned long hash code using a rolling hash
        /// </summary>
        /// <param name="ColumnsToHash"></param>
        /// <returns></returns>
        public ulong? GetPartialHash(params string[] ColumnsToHash)
            => GetPartialHash(true, ColumnsToHash);
        /// <summary>
        /// Returns an unsigned long hash code, using either a rolling hash or string's GetHashCode.
        /// If any of the column values are null or empty strings, will return null instead of a value
        /// </summary>
        /// <param name="RollingHash"></param>
        /// <param name="columnsToHash"></param>
        /// <returns></returns>
        public ulong? GetPartialHash(bool RollingHash, params string[] columnsToHash) => GetPartialHash(RollingHash, (IList<string>)columnsToHash);
        /// <summary>
        /// Returns an unsigned long hash code, using either a rolling hash or string's GetHashCode.
        /// If any of the column values are null or empty strings, will return null instead of a value
        /// </summary>
        /// <param name="RollingHash"></param>
        /// <param name="columnsToHash"></param>
        /// <returns></returns>
        public ulong? GetPartialHash(bool RollingHash, IList<string> columnsToHash)
        {
            if (Content == null || Content.Count == 0)
                return null; 
            StringBuilder work = new StringBuilder();
            if (columnsToHash.Count == 0)
                work.Append(ToString());
            else
            {
                foreach (string col in columnsToHash)
                {
                    string x = this[col];
                    if (x == null)
                        return null;
                    work.Append(x + _hash_boundary);
                }
            }
            if (RollingHash)
                return GetRollingHash(work.ToString());
            else
                return unchecked((ulong)work.ToString().GetHashCode());
        }
        const string _hash_boundary = "_\0_\0_";
        /// <summary>
        /// Returns an unsigned long hash code, using either a rolling hash method or string's GetHashCode.
        /// </summary>
        /// <param name="RollingHash"></param>
        /// <param name="includeNull">If true, will not return null if the column value is null</param>
        /// <param name="columnsToHash"></param>
        /// <returns></returns>
        public ulong? GetPartialHash(bool RollingHash,  bool includeNull, params DocRecordColumnInfo[] columnsToHash)
        {
            if (Columns == null || Columns.Count == 0)
                return null;
            StringBuilder work = new StringBuilder();
            if (columnsToHash.Length == 0)
                work.Append(ToString());
            else
            {
                foreach (var col in columnsToHash)
                {
                    string x = this[col];
                    if (col.NullIfEmpty && x == string.Empty) x = null;
                    if (x == null && !includeNull)
                        return null;                    
                    work.Append((x ?? "\0") + _hash_boundary);
                }
            }
            if (RollingHash)
                return GetRollingHash(work.ToString());
            else
                return unchecked((ulong)work.ToString().GetHashCode());
        }
        #endregion

        #region dynamic methods
        /// <summary>
        /// Use <see cref="TryGet(string, out object, string, int)"/> to return an object of Type <typeparamref name="T"/>.
        /// <para>If object is not the correct type, or fails to parse, then the default for the type will be returned instead (Unless <paramref name="ErrorNoSuccess"/> is true)</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Position"></param>
        /// <param name="ErrorNoSuccess"></param>
        /// <returns></returns>
        public T Evalaute<T>(int Position, bool ErrorNoSuccess = false)
        {
            object o;
            if(TryGet(null, out o, null, Position))
            {
                if(o is T)
                    return (T)o;
                if (o == null)
                {
                    if (default(T) != null && ErrorNoSuccess)
                        throw new Exception("Value is null but variable does not allow null.");
                    return default;
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
            if (ErrorNoSuccess)
                throw new Exception("Unable to get value");
            return default;
        }
   
        /// <summary>
        /// Use <see cref="TryGet(string, out object, string, int)"/> to return an object of Type <typeparamref name="T"/>.
        /// <para>If object is not the correct type, or fails to parse, then the default for the type will be returned instead (Unless <paramref name="ErrorNoSuccess"/> is true)</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ColumnName"></param>
        /// <param name="Alias"></param>
        /// <param name="ErrorNoSuccess"></param>
        /// <returns></returns>
        public T Evaluate<T>(string ColumnName, string Alias = null, bool ErrorNoSuccess = false)
        {
            var column = Columns.GetBestMatch(ColumnName, Alias);
            return Evalaute<T>(column, ErrorNoSuccess);            
        }
        /// <summary>
        /// Tries to get the value of the field as a variable of type <typeparamref name="T"/>. 
        /// <para>Return value indicates whether the record value was successfully parsed.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Position"></param>
        /// <param name="Result"></param>
        /// <returns></returns>
        public bool TryEvaluate<T>(int Position, out T Result)
        {            
            object o;
            if (TryGet(null, out o, null, Position))
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
        /// <summary>
        /// Tries to get the value of the field as a variable of type <typeparamref name="T"/>. 
        /// <para>Return value indicates whether the record value was successfully parsed.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ColumnName"></param>      
        /// <param name="Result"></param>
        /// <param name="Alias"></param>
        /// <returns></returns>
        public bool TryEvaluate<T>(string ColumnName, out T Result, string Alias = null)
        {
            var column = Columns.GetBestMatch(ColumnName, Alias);
            return TryEvaluate<T>(column, out Result);
        }

        /// <summary>
        /// Tries to get data associated with the column for this record, data type depending on Column's data type.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryGet(DocRecordColumnInfo col, out object result)
        {
            string val = this[col];
            return col.TryGet(val, out result);
        }
        /// <summary>
        /// Tries to get an the object associated with the value.
        /// <para>Data type will match that of the specified column via TryParse. If you just want the string value - use the <see cref="GetBestMatch(string, string, int)"/> instead.</para>
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="result"></param>
        /// <param name="alias"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool TryGet(string ColumnName, out object result, string alias = null, int position = -1)
        {
            DocRecordColumnInfo col;
            if (position >= 0)
                col = Columns[position];
            else
                col = Columns.GetBestMatch(ColumnName, alias, position);
            return TryGet(col, out result);
        }
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {            
            return TryGet(binder.Name, out result, null);            
        }        
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            string colName, alias = null;
            switch (indexes.Length)
            {
                case 1:
                    {
                        var t = indexes[0];
                        if (t.GetType() == typeof(int))
                            return TryGet(null, out result, null, (int)t);
                        //Column name
                        return TryGet(indexes[0].ToString(), out result);
                    }
                case 2:
                    {
                        //column name + position or alias.
                        var t1 = indexes[0];
                        var t2 = indexes[1];
                        int pos = -1;
                        if (t1.GetType() == typeof(int))
                        {
                            pos = (int)t1;
                            colName = t2.ToString();
                        }
                        else if(t2.GetType() == typeof(int))
                        {
                            pos = (int)t2;
                            colName = t1.ToString();
                        }
                        else
                        {
                            colName = t1.ToString();
                            alias = t2.ToString();
                        }
                        return TryGet(colName, out result, alias, pos);
                    }
                case 3:
                    {
                        //column name + position or alias.
                        var t1 = indexes[0];
                        var t2 = indexes[1];
                        var t3 = indexes[2];
                        int pos = -1;
                        if (t1.GetType() == typeof(int))
                        {
                            pos = (int)t1;
                            colName = t2.ToString();
                            alias = t3.ToString();
                        }
                        else if (t2.GetType() == typeof(int))
                        {
                            pos = (int)t2;
                            colName = t1.ToString();
                            alias = t3.ToString();
                        }
                        else
                        {
                            colName = t1.ToString();
                            alias = t2.ToString();
                            if (t3.GetType() == typeof(int))
                                pos = (int)t3;
                        }
                        return TryGet(colName, out result, alias: alias, position: pos);
                    }
            }
            result = null;
            return false;
        }

        #endregion

        #region static class map helpers
        public static DocRecord MapObject<T>(T toMap, DocRecordColumnCollection columnSet)
        {
            DocRecord r = new DocRecord(columnSet);
            var props = typeof(T)
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(p => p.CanRead);
            foreach(var prop in props)
            {
                var col = columnSet.FirstOrDefault(c => c.ColumnName == prop.Name);
                if (col == null)
                    continue;
                object nValue = prop.GetValue(toMap);
                Type underType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (underType.IsEnum)
                {
                    nValue = Enum.Parse(underType, nValue.ToString(), true);
                }
                else if (underType.IsArray)
                    continue; //Skip arrays...doesn't really make sense for coming from a dataRow, although it might be possible to do dynamically..
                else if (underType == typeof(char))
                {
                    var svalue = nValue.ToString();
                    if (svalue.Length > 1)
                        throw new InvalidCastException($"{prop.Name} tried to set a value of {svalue}, but needs to be a single char.");
                    nValue = svalue[0];
                }
                r.SetValue(col.Position, nValue);
            }
            return r;
        }
        /// <summary>
        /// Attempt to map DocRecord to an instance of Type T, where T is any type that has a parameterless constructor and setter properties.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryMapRecord<T>(out T result) where T:new()
        {
            result = new T();
            var props = typeof(T)
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(p => p.CanWrite);
            foreach(var prop in props)
            {
                var col = Columns.GetBestMatch(prop.Name);
                if (col == null)
                    continue;
                object nValue;
                if (TryGet(col, out nValue))
                {

                    Type underType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    if (underType.IsEnum)
                    {
                        nValue = Enum.Parse(underType, nValue.ToString(), true);
                    }
                    else if (underType.IsArray)
                        continue; //Skip arrays...doesn't really make sense for coming from a DocRecord, although it might be possible to do dynamically..
                    else if (underType == typeof(char))
                    {
                        var svalue = nValue.ToString();
                        if (svalue.Length > 1)
                            throw new InvalidCastException($"{prop.Name} tried to set a value of {svalue}, but needs to be a single char.");
                        nValue = svalue[0];
                    }
                    prop.SetValue(result, nValue);
                }
                else
                    return false;
            }

            return true;
        }

        #endregion

        /// <summary>
        /// Raise event for record data being changed
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnRecordDataChanged(RecordChangedEventArgs e)
        {
            RecordDataChanged?.Invoke(this, e);
        }
        /// <summary>
        /// A DocRecord has been modified - raise event to listeners
        /// </summary>
        public event EventHandler<RecordChangedEventArgs> RecordDataChanged;    



        /// <summary>
        /// Merges <paramref name="left"/> and <paramref name="right"/> into a new DocRecord using the Column meta data from <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="checkExist">If true, only sets the column if it exists in the target collection. Otherwise, may throw na error if <paramref name="left"/> or <paramref name="right"/> contains a column not in the destination.</param>
        /// <returns></returns>
        public static DocRecord Merge(DocRecordColumnCollection collection, DocRecord left, DocRecord right, bool checkExist = false)
        {            
            DocRecord l = new DocRecord(collection, true);
            foreach(var col in left.Columns)
            {
                if (checkExist && !collection.HasColumn(col.OwnerAlias, col.ColumnName))
                    continue;
                l[col.OwnerAlias, col.ColumnName] = left[col];
            }
            foreach(var col in right.Columns)
            {
                if (checkExist && !collection.HasColumn(col.OwnerAlias, col.ColumnName))
                    continue;
                l[col.OwnerAlias, col.ColumnName] = right[col];
            }
            return l;
        }
        /// <summary>
        /// If a record was missing columns because of an extra newline incorrectly included in the content...Merges the last column of this record with the first record of <paramref name="b"/>, and then adds the rest to the end of content.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="connection">Identifier to use when merging content from the first record of b into the last column of this Record</param>
        public void AddMissingContent(DocRecord b, string connection = default(string))
        {            
            if(b.Content.Count -1 + Content.Count <= Columns.Count)
            {
                Content[Content.Count - 1] += (connection?? "") + b.Content[0];
                for(int i = 1; i < b.Content.Count; i++)
                {
                    Content.Add(b.Content[i]);
                }
                return;
            }
            throw new InvalidOperationException("Attempted to merge records, but this would cause the Record to contain too many columns");
        }
        /// <summary>
        /// Checks if the record's column information contains the information being requested
        /// </summary>
        /// <param name="Alias"></param>
        /// <param name="ColumnName"></param>
        /// <returns></returns>
        public bool HasColumn(string Alias, string ColumnName)
        {
            return Columns.Exists(c => (Alias == null || c.OwnerAlias == Alias) && c.ColumnName == ColumnName);
        }
        /// <summary>
        /// Check if there's any column that matches when an alias is unspecified
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <returns></returns>
        public bool HasColumn(string ColumnName) => HasColumn(null, ColumnName);
        /// <summary>
        /// Used for determining records information and order/formatting.
        /// </summary>
        internal protected DocRecordColumnCollection Columns;        
        List<string> Content = new List<string>(); //toDo: consider changing to an array?  Or else add additional records as needed when calling toString to avoid the format getting messed up if we're allowing records to miss end columns..
        //Dictionary<DocRecordColumnInfo, string> Content;
        DocRecordColumnCollection IDataRecord.Columns => this.Columns;
        

        /// <summary>
        /// Overrides to string, combining the columns depending on the set up of the Column Collection it was created with. Includes the NewLine delimiter from the Column collection
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            /*
            if (!Columns.Valid)
                throw new InvalidOperationException("Column state is not valid.");*/

            //return string.Format(Columns.format, Content.ToArray());
            return string.Join("|", Content.ToArray()); //Move delimiter logic to MetaData only instead.
            /*
            StringBuilder output = new StringBuilder();
            Columns.ForEachIndex((col, idx) =>             
            {
                string colContent;
                if (!Content.TryGetValue(col, out colContent))
                    colContent = string.Empty;
                if (col.MaxLength != null)
                    output.Append(colContent.Substring(0, col.MaxLength.Value).PadRight(col.MaxLength.Value));
                else
                {
                    output.Append(colContent);
                }
                if (!Columns.FixedWidthMode && idx != Columns.Count)
                    output.Append(Columns.Delimiter);
            }, 1, 1);
            return output.ToString();
            */
        }
        /// <summary>
        /// Formats the DocRecord using the passed MetaData
        /// </summary>
        /// <param name="metaData"></param>
        /// <param name="includeLineEndDelimiter"></param>
        /// <returns></returns>
        public string ToString(MetaDataBase metaData, bool includeLineEndDelimiter = true)
        {
            StringBuilder output = new StringBuilder();
            int idx = 0;
            foreach (var col in Columns)
            {
                for (; idx < col.Position; idx++)
                {
                    if (metaData.Delimiter.HasValue)
                        output.Append(metaData.Delimiter.Value);
                }
                string colContent = Content[col.Position]; //Note: null/empty equivalent for purposes here.

                if (col.MaxLength != null)
                    output.Append(colContent.Substring(0, col.MaxLength.Value).PadRight(col.MaxLength.Value));
                else
                {
                    output.Append(colContent);
                }
                if (metaData.Delimiter.HasValue && col.Position == Columns.Count - 1)
                    output.Append(metaData.Delimiter.Value);
                idx++;
            }
            if (includeLineEndDelimiter)
                output.Append(metaData.LineEndDelimiter ?? Environment.NewLine);
            return output.ToString();
        }
  
  

        #region constructors
        /// <summary>
        /// Basic constructor, for use with DocReader. Does not do anything, Columns CanWrite, and content will need to be set separately
        /// </summary>        
        public DocRecord()
        { 
        }
        /// <summary>
        /// Sets up a very basic DocRecord
        /// </summary>
        /// <param name="owner"></param>
        public DocRecord(DocRecordColumnCollection owner)
            :this(owner, false)
        {            
        }
        /// <summary>
        /// Sets up the DocRecord with owner and CanWrite
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="canWrite"></param>
        public DocRecord(DocRecordColumnCollection owner, bool canWrite)            
        {
            Columns = owner;
            CanWrite = canWrite;
            //Content = new List<string>(owner.Columns.Count);
        }        
        /// <summary>
        /// Sets up the DocRecord with an owner, CanWrite, and initial content
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="canWrite"></param>
        /// <param name="ParsedContent"></param>
        public DocRecord(DocRecordColumnCollection owner, bool canWrite, IList<string> ParsedContent)
            :this(owner, canWrite)
        {
            SetParsedContent(ParsedContent);
        }
        /// <summary>
        /// Resets the underlying content list and sets the values from the Ilist
        /// </summary>
        /// <param name="ParsedContent"></param>
        protected internal void SetParsedContent(IList<string> ParsedContent)
        {
            for (int i = 0; i < ParsedContent.Count; i++)
            {
                Content.SetWithExpansion(i, ParsedContent[i]);
            }
        }
        /// <summary>
        /// Allows using the more complex constructor logic after construction. Mainly intended for use by DocColumnCollection generic parse
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="canWrite"></param>
        /// <param name="parsedContent"></param>
        protected internal virtual void Configure(DocRecordColumnCollection owner, bool? canWrite = null, IList<string>parsedContent = null)
        {
            Columns = owner;
            CanWrite = canWrite ?? CanWrite;
            if (parsedContent != null)
            {
                Content = new List<string>(new string[Columns.Count]);
                SetParsedContent(parsedContent);
            }
            else
                Content.Clear();
        }
        void IDataRecord.Configure(DocRecordColumnCollection owner, bool? canWrite, IList<object> parsedContent)
        {
            var parseList = parsedContent.Select(c => 
            {
                if (c == null || c is DBNull)
                    return null;
                return c.ToString();                
            }).ToList();
            Configure(owner, canWrite, parseList);
        }
        #endregion

        /// <summary>
        /// Sets whether user can update values of the record.
        /// </summary>
        public bool CanWrite { get; internal set; } = false;
        /// <summary>
        /// If true, indicates that there was an early line terminator in delimited mode. (e.g. Column meta data indicates that there should be columns 1, 2, 3. But DocRecord only has data for columns 1 and 2)
        /// </summary>
        public bool MissingData => Content.Count < Columns.Count;
        /// <summary>
        /// Gets/sets the column using Column name. Uses the column collection's default alias
        /// <para>Can only set the column if <see cref="CanWrite"/> is true.</para>
        /// </summary>        
        /// <param name="ColumnName"></param>
        /// <returns></returns>
        public virtual string this[string ColumnName]
        {
            get
            {                
                string x;
                var col = Columns.GetBestMatch(ColumnName);
                if (col == null)
                    throw new ArgumentException("Column not found");
                x = Content[col.Position];
                if (Columns.DefaultNullIfEmpty && x == string.Empty)
                    return null;
                return x;
            }
            set
            {
                if (CanWrite)
                {
                    var col = Columns.GetBestMatch(ColumnName);
                    if (col == null)
                        throw new ArgumentException("Column not found");

                    string x;
                    if (col.Position > Content.Count)
                        x = null;
                    else
                        x = Content[col.Position];
                    if (col.NullIfEmpty && x == string.Empty)
                        x = null;
                    Content.SetWithExpansion(col.Position, value);
                    //if (!col.TextQualify && Columns.Delimiter.HasValue && value.Contains(Columns.Delimiter.Value))
                    //{
                    //    col.TextQualify = true;
                    //    //Columns.SetFormat();
                    //}
                    if (x != value)
                    {
                        RecordChangedEventArgs e = new RecordChangedEventArgs(col, x, value);
                        OnRecordDataChanged(e);
                    }
                }
                else
                    throw new InvalidOperationException("Record does not allow writing/updating.");
            }
        }
        /// <summary>
        /// Gets/sets the column using alias + Column name.
        /// <para>Can only set the column if <see cref="CanWrite"/> is true.</para>
        /// </summary>
        /// <param name="alias"></param>
        /// <param name="ColumnName"></param>
        /// <returns></returns>
        public virtual string this[string alias, string ColumnName]
        {
            get
            {
                var col = Columns[alias, ColumnName];
                string x = Content[col.Position];
                if (col.NullIfEmpty && x == string.Empty)
                    return null;
                return x;
            }
            set
            {
                if (CanWrite)
                {

                    var col = Columns[alias, ColumnName];

                    string x;
                    if (col.Position > Content.Count)
                        x = null;
                    else
                        x = Content[col.Position];
                    if (col.NullIfEmpty && x == string.Empty)
                        x = null;
                    Content.SetWithExpansion(col.Position, value);
                    //if (!col.TextQualify && Columns.Delimiter.HasValue && value.Contains(Columns.Delimiter.Value))
                    //{
                    //    col.TextQualify = true;
                    //    Columns.SetFormat();
                    //}
                    if (x != value)
                    {
                        RecordChangedEventArgs e = new RecordChangedEventArgs(col, x, value);
                        OnRecordDataChanged(e);
                    }
                }
                else
                    throw new InvalidOperationException("Record does not allow writing/updating.");
            }
        }
        /// <summary>
        /// Tries to get the best matching column
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="alias"></param>
        /// <param name="position">Optional position filter if source file has more than one column with the same name.</param>
        /// <returns></returns>
        public string GetBestMatch(string ColumnName, string alias = null, int position = -1)
        {
            var col = Columns.GetBestMatch(ColumnName, alias, position);
            if (col == null)
                return null;

            string x;
            if (col.Position >= Content.Count)
                x = null;
            else
            {
                x = Content[col.Position];
                if (col.NullIfEmpty && x == string.Empty)
                    x = null;
            }
            return x;
        }
        string IDataRecord.GetBestMatch(string column, string alias) => GetBestMatch(column, alias, -1);
        /// <summary>
        /// Attempts to set the value for the first column that matches specified criteria.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="ColumnName"></param>
        /// <param name="alias"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool SetBestMatch(string value, string ColumnName, string alias = null, int position = -1)
        {
            var col = Columns.GetBestMatch(ColumnName, alias, position);
            if (col == null)
                return false;            
            if (col.NullIfEmpty.And(value == string.Empty))
                value = null;
            this[col] = value;         //Event logic.   
            return true;
        }
        /// <summary>
        /// Gets/sets the value associated with the column
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public virtual string this[DocRecordColumnInfo column]
        {
            get
            {
                if (!Columns.HasColumn(column))
                    throw new ArgumentException("Column is not a member of the ColumnCollection associated with this record.");
                if (column.Position >= Content.Count)
                    return null;
                string x = Content[column.Position];
                if (column.NullIfEmpty && x == string.Empty)
                    return null;                
                return x;
            }
            set
            {
                if (CanWrite)
                {
                    if(!Columns.HasColumn(column))
                        throw new ArgumentException("Column is not a member of the ColumnCollection associated with this record.");
                    string x;
                    if (column.Position >= Content.Count)
                        x = null;
                    else
                    {
                        x = Content[column.Position];
                        if (column.NullIfEmpty && x == string.Empty)
                            x = null;
                    }
                    Content.SetWithExpansion(column.Position, value);
                    //if (!column.TextQualify && Columns.Delimiter.HasValue && value.Contains(Columns.Delimiter.Value))
                    //{
                    //    column.TextQualify = true;
                    //    Columns.SetFormat();
                    //}

                    if (x != value)
                    {
                        RecordChangedEventArgs e = new RecordChangedEventArgs(column, x, value);
                        OnRecordDataChanged(e);
                    }
                }
                else
                    throw new InvalidOperationException("Record is not allowed to write.");
            }
        }
        /// <summary>
        /// Uses the Position specified by column to call <see cref="this[int]"/>.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public string this[IRecordColumnInfo column]
        {
            get
            {
                var dc = column as DocRecordColumnInfo;
                if (dc != null)
                    return this[dc];                
                return this[column.Position];
            }
            set
            {
                var dc = column as DocRecordColumnInfo;
                if (dc != null)
                    this[dc] = value;
                else
                    this[column.Position] = value;
            }
        }
        /// <summary>
        /// Getter/setter using position of the columns
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        public virtual string this[int columnIndex]
        {
            get
            {
                var col = Columns[columnIndex];
                if (col == null)
                    throw new ArgumentException("No Column specified for position " + columnIndex);
                if (columnIndex >= Content.Count)
                    return null;
                var x = Content[columnIndex];
                if (col.NullIfEmpty && x == string.Empty)
                    return null;
                return x;
            }
            set
            {
                if(!CanWrite)
                    throw new InvalidOperationException("Record is not allowed to write.");
                var col = Columns[columnIndex];
                if (col == null)
                    throw new ArgumentException("No Column specified for position " + columnIndex);

                string x;
                if (col.Position >= Content.Count)
                    x = null;
                else
                {
                    x = Content[col.Position];
                    if (col.NullIfEmpty && x == string.Empty)
                        x = null;
                }
                Content.SetWithExpansion(columnIndex, value);
                //if (!col.TextQualify && Columns.Delimiter.HasValue && value.Contains(Columns.Delimiter.Value))
                //{
                //    col.TextQualify = true;
                //    Columns.SetFormat();
                //}

                if (x != value)
                {
                    var arg = new RecordChangedEventArgs(col, x, value);
                    OnRecordDataChanged(arg);
                }
            }
        }
        #region object set with toString

        /// <summary>
        /// Attempts to set the value to the string representation of the object (by calling <see cref="object.ToString()"/> )
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="value"></param>
        public DocRecord SetValue(int columnIndex, object value)
        {
            this[columnIndex] = value.ToString();
            return this;
        }
        /// <summary>
        /// Attempts to set the value to the string representation of the object (by calling <see cref="object.ToString()"/> )
        /// </summary>
        public DocRecord SetValue(string columnName, string alias, int position, object value)
        {
            this[Columns.GetBestMatch(columnName, alias, position)] = value.ToString();
            return this;
        }
        /// <summary>
        /// Attempts to set the value to the string representation of the object (by calling <see cref="object.ToString()"/> )
        /// </summary>
        public DocRecord SetValue(string columnName, string alias, object value)
        {
            this[Columns.GetBestMatch(columnName, alias)] = value.ToString();
            return this;
        }
        /// <summary>
        /// Attempts to set the value to the string representation of the object (by calling <see cref="object.ToString()"/> )
        /// </summary>
        public DocRecord SetValue(string columnName, object value)
        {
            this[Columns.GetBestMatch(columnName)] = value.ToString();
            return this;
        }
        #endregion

    }
}
