using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// Read/write class for use with DocQueries, DocReader, and DocWriter
    /// </summary>
    public class DocRecord: IRecord
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
                return GetPartialHash(true, true, true, Columns.Columns.ToArray()) ?? 0;
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
        public ulong? GetPartialHash(bool RollingHash, params string[] columnsToHash)
        {
            if (Content == null || Content.Count == 0)
                return null; 
            StringBuilder work = new StringBuilder();
            if (columnsToHash.Length == 0)
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
        /// <param name="ExcludeEmpty">If true, will treat empty strings the same as null</param>
        /// <param name="includeNull">If true, will not return null if the column value is null</param>
        /// <param name="columnsToHash"></param>
        /// <returns></returns>
        public ulong? GetPartialHash(bool RollingHash, bool ExcludeEmpty, bool includeNull, params DocRecordColumnInfo[] columnsToHash)
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
                    if (ExcludeEmpty && x == string.Empty) x = null;
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

        /// <summary>
        /// Merges the DocRecords and specifies the alias for the new Column Collection underlying.
        /// <para>The new alias will be used for any new columns added to the underlying collection</para>
        /// </summary>
        /// <param name="Alias"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static DocRecord Merge(string Alias, DocRecord left, DocRecord right)
        {
            DocRecordColumnCollection columns = DocRecordColumnCollection.Merge(Alias, left.Columns, right.Columns);
            List<string> newContent = new List<string>(left.Columns.Count + right.Columns.Count);
            foreach(var col in left.Columns)
            {
                newContent.Add(left[col]);
            }
            foreach(var col in right.Columns)
            {
                newContent.Add(right[col]);
            }
            return new DocRecord(columns, left.CanWrite, newContent);
        }
        public DocRecord Merge(DocRecord toMerge)
        {
            DocRecordColumnCollection cols = DocRecordColumnCollection.Merge(null, Columns, toMerge.Columns);
            List<string> newContent = new List<string>(Columns.Count + toMerge.Columns.Count);
            foreach(var col in Columns)
            {
                newContent.Add(this[col]);
            }
            foreach(var col in toMerge.Columns)
            {
                newContent.Add(toMerge[col]);
            }
            return new DocRecord(cols, CanWrite, newContent);
        }
        public DocRecord Merge(DocRecordColumnCollection collection, DocRecord left, DocRecord right)
        {            
            DocRecord l = new DocRecord(collection, left.CanWrite);
            foreach(var col in left.Columns)
            {
                l[col] = left[col];
            }
            foreach(var col in right.Columns)
            {
                l[col] = right[col];
            }
            return l;
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
        public bool HasColumn(string ColumnName) => HasColumn(null, ColumnName);
        /// <summary>
        /// Used for determining records...
        /// </summary>
        DocRecordColumnCollection Columns;
        Dictionary<DocRecordColumnInfo, string> Content;
        /// <summary>
        /// Overrides to string, combining the columns depending on the set up of the Column Collection it was created with
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (!Columns.Valid)
                throw new InvalidOperationException("Column state is not valid.");
            StringBuilder output = new StringBuilder();
            foreach(var col in Columns)
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
                if (!Columns.FixedWidthMode)
                    output.Append(Columns.Delimiter);
            }
            return output.ToString();
        }

        #region constructors
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
            Content = new Dictionary<DocRecordColumnInfo, string>();
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
            for(int i = 0; i < Columns.Count; i++)
            {
                Content[owner[i]] = ParsedContent[i];
            }
        }
        #endregion

        /// <summary>
        /// Sets whether user can update values of the record.
        /// </summary>
        public bool CanWrite { get; private set; } = false;
        /// <summary>
        /// Gets/sets the column using Column name. Uses the column collection's default alias
        /// <para>Can only set the column if <see cref="CanWrite"/> is true.</para>
        /// </summary>        
        /// <param name="ColumnName"></param>
        /// <returns></returns>
        public string this[string ColumnName]
        {
            get
            {
                string x;
                var col = Columns[ColumnName];
                if (!Content.TryGetValue(col, out x))
                    x = null;
                return x;
            }
            set
            {
                if (CanWrite)
                {
                    Content[Columns[ColumnName]] = value;
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
        public string this[string alias, string ColumnName]
        {
            get
            {
                string x;
                var col = Columns[alias, ColumnName];
                if (!Content.TryGetValue(col, out x) 
                    || col.NullIfEmpty.And(x==string.Empty) )
                    x = null;
                return x;
            }
            set
            {
                if (CanWrite)
                {
                    Content[Columns[alias, ColumnName]] = value;
                }
                else
                    throw new InvalidOperationException("Record does not allow writing/updating.");
            }
        }
        /// <summary>
        /// Gets/sets the value associated with the column
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public string this[DocRecordColumnInfo column]
        {
            get
            {
                string x;
                if (!Content.TryGetValue(column, out x) || column.NullIfEmpty.And(x == string.Empty))
                    x = null;
                return x;
            }
            set
            {
                if (CanWrite)
                    Content[column] = value;
                else
                    throw new InvalidOperationException("Record is not allowed to write.");
            }
        }
        /// <summary>
        /// Getter/setter using index of the columns
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        public string this[int columnIndex]
        {
            get
            {
                return this[Columns[columnIndex]];
            }
            set
            {
                this[Columns[columnIndex]] = value;
            }
        }
    }
}
