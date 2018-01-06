using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SEIDR.Doc
{
    /*
    public class JoinedDelimitedRecords
    {
        public DelimitedRecord[] Content
        {
            get
            {
                return content.ToArray();
            }
        }
        List<DelimitedRecord> content;
        public string this[string alias, string column]
        {
            get
            {
                return this[alias]?[column];
            }
        }
        public DelimitedRecord this[string alias]
        {
            get
            {
                foreach(var record in content)
                {
                    if (record.ALIAS == alias)
                        return record;
                }
                return null;
            }
        }
        public char Delimiter { get; set; } = '|';
        public JoinedDelimitedRecords(params DelimitedRecord[] Records)
        {
            if (Records
                    .GroupBy(r => r.ALIAS)
                    .Where(g => g.HasMinimumCount(2))
                    .HasMinimumCount(1))
                throw new InvalidOperationException("There are at least two records sharing the same alias");
            content = new List<DelimitedRecord>(Records);
            foreach(var record in Records)
            {
                if (record.Delimiter != null)
                {
                    Delimiter = record.Delimiter.Value;
                    break;
                }                
            }                        
        }
        /// <summary>
        /// Tries to add the delimited record to the Content. 
        /// <para>Will throw an invalid operation exception if the content already contains a record for the same alias
        /// </para>
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public JoinedDelimitedRecords AddRecord(DelimitedRecord r)
        {
            if (content.Exists(c => c.ALIAS == r.ALIAS))
                throw new InvalidOperationException("Joined records already has an entry from this Alias");
            content.Add(r);
            return this;
        }
        /// <summary>
        /// Replaces any existing delimited record with the same alias. Then returns this JoinedDelimitedRecords instance to allow chaining.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public JoinedDelimitedRecords Replace(DelimitedRecord r)
        {
            content = content.Where(c => c.ALIAS != r.ALIAS).ToList();
            content.Add(r);
            return this;
        }
        /// <summary>
        /// Sets the delimited record in this instance based on the alias. Replaces any existing delimited record for the same alias
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public JoinedDelimitedRecords Set(DelimitedRecord r) => Replace(r);
    }
    */
    /// <summary>
    /// Contains individual lines from Delimited Document.
    /// <para>Read only except for being able to add extra columns to the end as a sort of tag.</para>
    /// <para>Can be used to write a record to a new file using the ToString() method.</para>
    /// </summary>
    [Obsolete("Replace with DocRecord..")]
    public class DelimitedRecord : IRecord
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
        /// Returns a ulong value of the record's ToString as a hash
        /// </summary>
        public ulong FullRecordHash
        {
            get { return unchecked((ulong)ToString().GetHashCode()); }
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
        public ulong? GetPartialHash(bool RollingHash, params string[] columnsToHash)
        {
            if (content == null || content.Length == 0)
                return null;
            StringBuilder work = new StringBuilder();            
            if (columnsToHash.Length == 0)
                work.Append(ToString());
            else
            {
                foreach (string col in columnsToHash)
                {
                    string x = this[col];
                    if (x == null || x == string.Empty)
                        return null;
                    work.Append(x + boundary);
                }
            }
            if (RollingHash)
                return GetRollingHash(work.ToString());
            else
                return unchecked((ulong)work.ToString().GetHashCode());
        }
        const string boundary = "\0__\0__";
        /// <summary>
        /// Returns an unsigned long hash code, using either a rolling hash or string's GetHashCode.
        /// If any of the column values are null or empty strings, will return null instead of a value
        /// </summary>
        /// <param name="RollingHash"></param>
        /// <param name="ExcludeEmpty">If true, will treat empty strings as a null</param>
        /// <param name="columnsToHash"></param>
        /// <returns></returns>
        public ulong? GetPartialHash(bool RollingHash, bool ExcludeEmpty, bool includeNull, params DocRecordColumnInfo[] columnsToHash)
        {
            if (_header == null || _header.Count == 0)
                return null;
            StringBuilder work = new StringBuilder();
            if (columnsToHash.Length == 0)
                work.Append(ToString());
            else
            {
                foreach (var col in columnsToHash)
                {
                    string x = this[col];
                    if (ExcludeEmpty && x == string.Empty) x = null;
                    if (x == null && !includeNull)
                        return null;

                    work.Append(x + boundary);
                }
            }
            if (RollingHash)
                return GetRollingHash(work.ToString());
            else
                return unchecked((ulong)work.ToString().GetHashCode());
        }
        #endregion

        /// <summary>
        /// Returns a copy of the header information
        /// </summary>
        public IList<DocRecordColumnInfo> HeaderList
        {
            get { return new List<DocRecordColumnInfo>(_header); }
        }
        List<DocRecordColumnInfo> _header;
        Dictionary<DocRecordColumnInfo, string> _Content; //replace with index? Meh.
        string[] content;        
        int contentLength;
        List<string> _Header;
        char? _Delimiter;
        /// <summary>
        /// Gets the delimiter associated with the record
        /// </summary>
        public char? Delimiter => _Delimiter;
        public bool ContainsHeader(string x)
        {
            if (string.IsNullOrWhiteSpace(x))
                return false;
            return _header.Exists(h => h.ColumnName == x);
            //if (_Header.Contains(x) || x.In(extras))
            //    return true;
            //return false;
        }
        public bool ContainsHeader(string alias, string header)
        {
            if (string.IsNullOrWhiteSpace(header))
                return false;
            return _header.Exists(h => (h.OwnerAlias == alias || alias == null) && h.ColumnName == header);
        }
        /// <summary>
        /// Changes the delimiter, but only if the record already has a delimiter.
        /// </summary>
        /// <param name="newValue"></param>
        /// <returns>Returns true if the delimiter was changed, else false</returns>
        public bool ChangeDelimiter(char newValue)
        {
            if (!_Delimiter.HasValue)
                return false;
            _Delimiter = newValue;
            return true;
        }
        /// <summary>
        /// If set to true, will return null instead of erroring when the line does not have enough records for the index even though
        /// <para>the header length indicates that it should have enough records.</para>
        /// <para>Used when accessing without headers</para>
        /// </summary>
        public static bool NullIfTruncated { get; set; } = false;

        string IRecord.this[int index]
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
        string IRecord.this[string alias, string column]
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets the value of the record at the provided index. For use when Headers are not being used.
        /// </summary>
        /// <param name="index">0 based index to grab content from</param>
        /// <returns></returns>
        public string this[int index]
        {
            get
            {
                /*
                if(index >= contentLength && extras != null)
                {
                    index -= contentLength;
                    if (index > extras.Length && NullIfTruncated)
                        return null;
                    return extras[index];
                }*/
                if (NullIfTruncated && index >= content.Length /*&& index < _Header.Count*/ )  
                    return null; //If the line is truncated, return null instead of erroring
                return content[index];
            }
        }
        /// <summary>
        /// Gets the first value of the record for the provided column( any alias)
        /// </summary>
        /// <param name="Column"></param>
        /// <returns></returns>
        public string this[string Column]
            => this[null, Column];
        /*{
            get
            {
                if (_Header == null)
                    throw new InvalidOperationException("Instance is not set up for accessing data by column name");
                int x = _Header.IndexOf(Column);
                //compare against EXPECTED length of the content (using original header info)
                /*if( x >= contentLength && extras != null)
                {
                    x -= contentLength;
                    if (x >= extras.Length && NullIfTruncated)
                        return null;
                    return extras[x];
                }* /
                if (NullIfTruncated && x >= content.Length)
                    return null;
                return content[x];
            }
        }*/
        /// <summary>
        /// Gets the content owned by the specified column under the specified alias
        /// </summary>
        /// <param name="Alias"></param>
        /// <param name="ColumnName"></param>
        /// <returns></returns>
        public string this[string Alias, string ColumnName]
            => this[new DocRecordColumnInfo(ColumnName, Alias)];
        public string this[DocRecordColumnInfo Column]
        {
            get
            {
                if (_header == null)
                    throw new InvalidOperationException("Instance is not set up for accessing data by column name");
                if(Column.OwnerAlias == null)
                {
                    var col = _header.FirstOrDefault(h => h.ColumnName == Column.ColumnName);
                    if (col != null)
                        return _Content[col];
                    return null;
                }

                string content;
                if (!_Content.TryGetValue(Column, out content))
                    content = null;
                return content;
            }
        }
        public void AddColumn(DocRecordColumnInfo Column, string Value = null)
        {
            _header.Add(Column);//controls content order
            _Content.Add(Column, Value);
        }
        public void AddColumn(string ColumnName, string Value = null)
        {
            AddColumn(new DocRecordColumnInfo(ColumnName, null), Value);
        }

        /// <summary>
        /// Gets the value specified by the column and converts it to T. Does not handle WhiteSpace
        /// </summary>
        /// <typeparam name="T">Must be a primitive type. See the System.TypeCode enumeration</typeparam>
        /// <param name="Alias">Owning alias. Null will search for the for the first column that matches by name</param>
        /// <param name="Column"></param>
        /// <param name="defaultVal">Default value to use if there's no content</param>
        /// <returns></returns>
        public T GetValue<T>(string Alias, string Column, T defaultVal = default(T))
        {
            string x = this[Alias, Column];            
            if (x == null)            
                return defaultVal;                            
            //return (T) Convert.ChangeType(x, Type.GetTypeCode(typeof(T)));
            return (T)Convert.ChangeType(x, typeof(T));
        }
        /// <summary>
        /// Gets the value specified for the first column that matches (regardless of alias) and converts to T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Column"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public T GetValue<T>(string Column, T defaultVal = default(T))
            => GetValue(null, Column, defaultVal);
        /// <summary>
        /// Gets the value specified at the given index and converts it to T.
        /// </summary>
        /// <typeparam name="T">Must be a primitive type. See the System.TypeCode enumeration</typeparam>
        /// <param name="index">Numeric index of the data to grab</param>
        /// <returns></returns>
        public T GetValue<T>(int index) where T: class
        {
            string x = this[index];
            if (x == null)
                return null as T;
            return (T)Convert.ChangeType(x, Type.GetTypeCode(typeof(T)));
        }
        /// <summary>
        /// Returns a new list copy of the internal content of this record.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetContent()
        {        
            if(_header != null)
            {
                return _Content.OrderedMap(_header);
            }
            var temp = new List<string>(content);
            if (content.Length < contentLength)
                temp.AddRange(new string[contentLength - content.Length]);
            return temp;
        }
        /// <summary>
        /// Combines delimited records. Must be set up for using Headers
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static DelimitedRecord Merge(DelimitedRecord left, DelimitedRecord right)
        {
            if (left?._header == null || left?._Content == null)
                throw new ArgumentException(nameof(left), "Not set up for using headers!");
            if (right?._header == null || right?._Content == null)
                throw new ArgumentException(nameof(right), "Not set up for using headers!");

            DelimitedRecord r = new DelimitedRecord(left._header.Count + right._header.Count, left.Delimiter ?? right.Delimiter);
            foreach(var col in left._header)
            {
                r.AddColumn(col, left[col]);
            }
            foreach(var col in right._header)
            {
                r.AddColumn(col, right[col]);
            }
            return r;
        }
        /// <summary>
        /// Returns a new delimited record with the empty columns at the beginning
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="record"></param>
        /// <returns></returns>
        public static DelimitedRecord MergeEmpty(IList<DocRecordColumnInfo> columns, DelimitedRecord record)
        {
            if (record?._header == null || record?._Content == null)
                throw new ArgumentException(nameof(record), "Not set up for using headers!");
            DelimitedRecord r = new DelimitedRecord(columns.Count + record._header.Count, record.Delimiter);
            foreach(var col in columns)
            {
                r.AddColumn(col);
            }
            foreach(var col in record._header)
            {
                r.AddColumn(col, record[col]);
            }
            return r;
        }
        /// <summary>
        /// Gets a new delimited record containing a subset of the columns from the working record
        /// </summary>
        /// <param name="work"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public static DelimitedRecord GetSubset(DelimitedRecord work, IList<DocRecordColumnInfo> columns)
        {
            DelimitedRecord r = new DelimitedRecord(columns.Count, work.Delimiter);
            foreach(var col in columns)
            {
                r.AddColumn(col, work[col]);
            }
            return r;
        }
        private DelimitedRecord(int contentLength, char? Delimiter)
        {
            this.contentLength = contentLength;
            _Header = new List<string>();
            _header = new List<DocRecordColumnInfo>();
            _Delimiter = Delimiter;
        }
        private DelimitedRecord(string[] Header, int contentLength, char? Delimiter, string Alias)
        {
            this.contentLength = contentLength;
            _Header = new List<string>(Header);
            _header = new List<DocRecordColumnInfo>(DocRecordColumnInfo.CreateColumns(Alias, Header));
            _Delimiter = Delimiter;
        }
        
        public DelimitedRecord(DocRecordColumnInfo[] Headers, char? Delimiter, string[] Content)
        {
            _header = new List<DocRecordColumnInfo>(Headers);
            _Content = new Dictionary<DocRecordColumnInfo, string>();
            Content.MapInto(_Content, Headers);                        
        }
        /// <summary>
        /// Creates a delimited record but with no Header information.
        /// <para> Will not be able to access column data using column names</para>
        /// </summary>
        /// <param name="Content"></param>
        /// <param name="Delimited"></param>
        public DelimitedRecord(string[] Content, char? Delimited = '|')
        {
            content = Content;
            contentLength = content.Length;
            _Delimiter = Delimited;
            _header = null;
            _Header = null;
        }
        /// <summary>
        /// Creates a new record
        /// </summary>
        /// <param name="Header"></param>
        /// <param name="Content"></param>
        /// /// <param name="contentExpectedLength">Expected number of columns to be in the passed content - should match the number of columns in the physical file when reading.</param>
        /// <param name="OwnerAlias">Alias of object owning the columns</param>
        /// <param name="Delimiter">Delimiter that split Content into an array</param>
        public DelimitedRecord(string[] Header, string[] Content, int contentExpectedLength, char? Delimiter = '|',
            string OwnerAlias = null)
            :this(Header, contentExpectedLength, Delimiter, Alias: OwnerAlias)
        {            
            content = Content;
            for(int i=  0; i < Header.Length; i++)
            {
                string header = Header[i];
                var col = new DocRecordColumnInfo(header, OwnerAlias);
                string newContent = null;
                if (i < content.Length)
                    newContent = content[i];
                _Content[col] = newContent;
            }
        }
        /// <summary>
        /// Creates a new delimited record using the delimiter provided to split the content string
        /// </summary>
        /// <param name="Header"></param>
        /// <param name="Content"></param>
        /// <param name="contentExpectedLength">Expected number of columns to be in the passed content - should match the number of columns in the physical file when reading.</param>
        /// <param name="Delimiter"></param>
        /// <param name="Alias">Owner alias</param>
        public DelimitedRecord(string[] Header, string Content, int contentExpectedLength, char? Delimiter = '|', string Alias = null)
            :this(Header, contentExpectedLength, Delimiter, Alias:Alias)
        {                     
            if (Delimiter.HasValue)
                content = Content.SplitOutsideQuotes(Delimiter.Value);
            else
                content = new string[] { Content };
            for (int i = 0; i < Header.Length; i++)
            {
                string header = Header[i];
                var col = new DocRecordColumnInfo(header, Alias);
                string newContent = null;
                if (i < content.Length)
                    newContent = content[i];
                _Content[col] = newContent;
            }
        }        
        /// <summary>
        /// Converts the record into a instance of type 'T'
        /// <para>Note: Fields may or may not work as expected with setting values.</para>
        /// <para>Note: Complex types like lists or arrays are not supported, only primitives. If you want more complex logic, you'll have to convert yourself</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ConvertToType<T>() where T:new()
        {
            T temp = new T();
            var props = typeof(T).GetProperties();
            var fields = typeof(T).GetFields();

            foreach(var field in fields)
            {
                if (!_Header.Contains(field.Name))
                    continue;
                string x = this[field.Name];
                //Could probably also use declaring type since this is a field so there's no definition to change from inheritance
                object o = Convert.ChangeType(x, field.ReflectedType); 
                field.SetValue(temp, o);
            }

            foreach(var prop in props)
            {
                if (!prop.CanWrite)
                    continue;
                if (!_Header.Contains(prop.Name))
                    continue;
                string x = this[prop.Name];
                object o = Convert.ChangeType(x, prop.PropertyType);
                prop.SetValue(temp, o, null);
            }

            return temp;
        }
        /// <summary>
        /// Combines the content into a delimited string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {

            if (!_Delimiter.HasValue)
            {
                if (content.Length == 0)
                    return string.Empty;
                return content[0];
            }
            if(_header != null)
            {
                StringBuilder sb = new StringBuilder();
                foreach(var col in _header)
                {
                    sb.Append(_Content[col] ?? string.Empty);
                    sb.Append(_Delimiter ?? '|');
                }
                return sb.ToString();
            }                
            List<string> temp = new List<string>(content);
            if(content.Length < contentLength)
            {
                temp.AddRange(new string[contentLength - content.Length]); //add null array to fill in the gap.
            }
            //if(extras != null)
            //    temp.AddRange(extras);
            return string.Join(_Delimiter.ToString(), temp.ToArray());
        }

        public bool HasColumn(string alias, string Column)
        {
            throw new NotImplementedException();
        }
    }
}
