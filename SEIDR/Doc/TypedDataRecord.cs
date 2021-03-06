﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Doc.DocEditor;

namespace SEIDR.Doc
{

    /// <summary>
    /// Data accessor helper for reading data from files or databases.
    /// </summary>
    public class TypedDataRecord : IDataRecord, IDetailDataRecord
    {

        #region Implicits
        /// <summary>
        /// Convert TypedDataRecord into a dictionary
        /// </summary>
        /// <param name="record"></param>
        public static implicit operator Dictionary<string, object>(TypedDataRecord record)
        {
            return record.Columns.ToDictionary(c => c.ColumnName, c => record.content[c.Position]?.Value);
        }

        /// <summary>
        /// Convert TypedDataRecord into a dictionary
        /// </summary>
        /// <param name="record"></param>
        public static implicit operator Dictionary<string, DataItem>(TypedDataRecord record)
        {
            return record.Columns.ToDictionary(c => c.ColumnName, c => record.content[c.Position]);
        }
        /// <summary>
        /// Converts a dictionary of DataItems into a TypedDataRecord that can be used to write.
        /// </summary>
        /// <param name="keyValues"></param>
        public static implicit operator TypedDataRecord(Dictionary<string, DataItem> keyValues)
        {
            var colInfo = keyValues.Select(kv => new DocRecordColumnInfo(kv.Key, kv.Value.DataType)).ToList();
            var col = new DocRecordColumnCollection(colInfo);
            var record = new TypedDataRecord(col);
            foreach(var c in col)
            {
                record[c] = keyValues[c.ColumnName];
            }
            return record;
        }
        #endregion

        long? _ID;
        public long? ID => _ID;
        void IDetailDataRecord.SetID(long ID)
        {
            _ID = ID;
        }

        void IDetailDataRecord.ClearID()
        {
            _ID = null;
        }
        internal int? PageID { get; set; }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < Columns.Count; i++)
            {
                if(i < content.Count)
                    sb.Append(Columns[i].FormatValue(content[i]?.Value));
                if (i < Columns.Count - 1)
                    sb.Append('|');                
            }
            return sb.ToString();
        }
        string IDataRecord.this[int Position]
        {
            get
            {
                var col = Columns[Position];
                var d = this[Position]?.Value;
                return col.FormatValue(d);
            }
            set => SetValue(Position, value);
        }
        string IDataRecord.this[string Column]
        {
            get
            {
                var col = Columns.GetBestMatch(Column);
                var d = this[Column]?.Value;
                return col.FormatValue(d);
                
            }
            set => SetValue(Column, value);
        }
        string IDataRecord.this[IRecordColumnInfo column]
        {
            get
            {
                var col = Columns[column.Position];
                var d = this[column.Position]?.Value;
                return col.FormatValue(d);
            }
            set => SetValue(column.Position, value);
        }
        string IDataRecord.this[DocRecordColumnInfo column]
        {
            get
            {
                return column.FormatValue(this[column]?.Value);                
            }
            set => SetValue(column.Position, value);
        }
        string IDataRecord.this[string Column, string Alias] { get => this[Column, Alias]; set => SetValue(Column, value, Alias); }

        public DataItem this[int Position]
        {
            get
            {
                if (Position >= content.Count)
                    content.SetWithExpansion(Position, new DataItem(null, DocRecordColumnType.NUL), new DataItem(null, DocRecordColumnType.NUL));
                return content[Position];
            }
            set
            {
                SetValue(Position, value?.Value);                
            }
        }
        public DataItem this[string Column, string Alias = null]
        {
            get
            {
                var col = Columns.GetBestMatch(Column, Alias);
                if (col == null)
                    throw new ArgumentException("Column not found", nameof(Column));
                return content[col.Position];
            }
            set
            {
                //var col = Columns.GetBestMatch(Column, Alias);
                SetValue(Column, value?.Value, Alias);
                /*
                if (value.DataType != DocRecordColumnType.NUL && col.DataType != DocRecordColumnType.Unknown)
                {                    
                    if (col.DataType != value.DataType)
                    {
                        throw new ArgumentException("Argument data type does not match expected: " + col.DataType);
                    }
                }
                content.SetWithExpansion(col.Position, value, new DataItem(null, DocRecordColumnType.NUL));*/
            }
        }                
        public void SetValue(int Position, object value) => SetValue(Columns[Position], value);
        /// <summary>
        /// Sets the value in the internal content array.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="value"></param>
        /// <param name="RaiseColChanged"></param>
        protected void SetValue(DocRecordColumnInfo column, object value, bool RaiseColChanged = true)
        {
            if (!CanWrite && RaiseColChanged)
                throw new InvalidOperationException("Not allowed to write values.");
            DataItem x = null;
            DataItem n;
            if (column < content.Count)
                x = content[column];
            if (value == null || value == DBNull.Value || column.DataType == DocRecordColumnType.NUL)
            {
                n = new DataItem(null, DocRecordColumnType.NUL);
                content.SetWithExpansion(column.Position, n, n);
            }
            else if(column.DataType <= DocRecordColumnType.NVarchar 
                && column.NullIfEmpty
                && value.ToString() == string.Empty)
            {
                n = new DataItem(null, DocRecordColumnType.NUL);
                content.SetWithExpansion(column.Position, n, n);
            }
            else
            {
                if(column.Array)
                {
                    DataItem.CheckArrayObject(ref value, column.DataType);                    
                }
                if (value is string)
                {
                    object o;
                    if (column.TryGet((string)value, out o))
                    {
                        n = new DataItem(o, column.DataType);
                        content.SetWithExpansion(column.Position, n, new DataItem(null, DocRecordColumnType.NUL));
                    }
                    else
                        throw new ArgumentException("Value type (" + value.GetType().Name + ") does not match expected - " + column.DataType, nameof(value));
                }
                else if (column.CompareDataType(value))
                {
                    n = new DataItem(value, column.DataType);
                    content.SetWithExpansion(column.Position, n, new DataItem(null, DocRecordColumnType.NUL));
                }
                else
                    throw new ArgumentException("Value type (" + value.GetType().Name + ") does not match expected - " + column.DataType, nameof(value));
            }
            if (RaiseColChanged && x != null)
            {
                TypedRecordChangedEventArgs e = new TypedRecordChangedEventArgs(column, x, n);
                RecordChanged?.Invoke(this, e);
                //record changed event
            }
        }
        /// <summary>
        /// Event watch for when the record has a column changed.
        /// </summary>
        public event EventHandler<TypedRecordChangedEventArgs> RecordChanged;
        public void SetValue(string columnName, object value, string Alias = null)
        {
            var col = Columns.GetBestMatch(columnName, Alias);
            SetValue(col, value);
        }
        /// <summary>
        /// Indicates whether or not the values can be modified.
        /// </summary>
        public bool CanWrite { get; internal set; } = true;
        /// <summary>
        /// Column information.
        /// </summary>
        public DocRecordColumnCollection Columns { get; private set; }

        public TypedDataRecord()
        {
            Columns = new DocRecordColumnCollection();
            content = new List<DataItem>();
        }
        public TypedDataRecord(DocRecordColumnCollection sourceOwner)
        {
            Columns = sourceOwner;
            content = new List<DataItem>();
        }
        public void Configure(DocRecordColumnCollection owner, bool? canWrite, IList<object> parsedContent)
        {
            Columns = owner;
            content = new List<DataItem>(owner.Count);
            CanWrite = canWrite ?? true;
            foreach(var col in Columns.Columns)
            {
                if (col.Position >= parsedContent.Count)
                    break;
                SetValue(col, parsedContent[col], false);
            }
        }
        List<DataItem> content;
        /// <summary>
        /// For debug purposes - allows viewing a copy of the internal content.
        /// </summary>
        public DataItem[] ContentCopy
        {
            get { return content.ToArray(); }
        }
        string IDataRecord.GetBestMatch(string column, string alias)
        {
            return ((IDataRecord)this)[column, alias];
        }
        /// <inheritdoc />
        public bool HasColumn(string alias, string Column)
        {
            return Columns.HasColumn(alias, Column);
        }

        /// <inheritdoc />
        public bool HasColumn(string columnName)
        {
            return Columns.HasColumn(null, columnName, -1);
        }
        /// <inheritdoc />
        public bool TryGet(DocRecordColumnInfo columnInfo, out object result)
        {
            if (content.Count > columnInfo.Position)
            {
                result = content[columnInfo];
                if (result != null)
                    result = ((DataItem)result).Value;
                return true;
            }
            result = null;
            return false;
        }
        /// <inheritdoc />
        public bool TryGet(string colName, out object result, string alias = null)
        {
            var col = Columns.GetBestMatch(colName, alias);            
            if (col != null && content.Count > col.Position)
            {
                result = content[col];
                if (result != null)
                    result = ((DataItem)result).Value;
                return true;
            }
            result = null;
            return false;
        }
        /// <summary>
        /// Represent object as list of key values. For debug purposes.
        /// </summary>
        public List<KeyValuePair<string, DataItem>> KeyValues
        {
            get
            {
                var ret = new List<KeyValuePair<string, DataItem>>(Columns.Count);
                foreach (var col in Columns)
                {
                    ret.Add(new KeyValuePair<string, DataItem>(col.ColumnName, content[col.Position]));
                }
                return ret;
            }
        }
        /// <summary>
        /// Represent object as list of key values, sorted by column name. For debug purposes.
        /// </summary>
        public List<KeyValuePair<string, DataItem>> KeyValuesAlphabetical
        {
            get
            {
                var ret = new List<KeyValuePair<string, DataItem>>(Columns.Count);
                //var ret = new Dictionary<string, DataItem>(Columns.Count); //Looks the same in debug, so just return a list with defined order.
                foreach (var col in Columns.OrderBy(c => c.ColumnName))
                {
                    ret.Add(new KeyValuePair<string, DataItem>(col.ColumnName, content[col.Position]));
                }
                return ret;
            }
        }
        /// <summary>
        /// Attempts to map the object to a TypedDataRecord based on the passed set of columns and getter properties on the object.
        /// <para>Note: affected by attributes such as <see cref="DataBase.DatabaseManagerIgnoreMappingAttribute"/> and <see cref="DataBase.DatabaseManagerFieldMappingAttribute"/></para>
        /// </summary>
        /// <param name="columnInfos"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static TypedDataRecord Map(DocRecordColumnCollection columnInfos, object source)
        {
            var props = DataBase.DatabaseManagerExtensions.GetGetters(source.GetType())
                    .Where(s => columnInfos.Exists(c => c.ColumnName == s.Key));

            TypedDataRecord ret = new TypedDataRecord(columnInfos);
            foreach (var prop in props)
            {
                ret[prop.Key] = new DataItem(
                    prop.Value.Invoke(source, parameters: null), 
                    columnInfos[prop.Key].DataType);
            }
            return ret;
        }

    }
}
