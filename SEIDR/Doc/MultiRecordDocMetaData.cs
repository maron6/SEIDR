using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{    
    /// <summary>
    /// MetaData for a Doc file containing multiple sets
    /// </summary>
    public sealed class MultiRecordDocMetaData : MetaDataBase
    {
        /// <summary>
        /// Default column name for the Key Column (First column)
        /// </summary>
        public const string DEFAULT_KEY_NAME = "Key";
        /// <summary>
        /// Attempts to get the DocRecordColumn Collection associated with the key. 
        /// <para>If the key has not been added yet, it is added.</para>
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public DocRecordColumnCollection this[string Key]
        {
            get
            {
                DocRecordColumnCollection colSet;
                if (Key == null)
                    Key = DEFAULT_KEY;
                if (!ColumnSets.TryGetValue(Key, out colSet))
                {
                    colSet = new DocRecordColumnCollection(Alias);                    
                    //colSet.SetDelimiter(RecordDelimiter);
                    ColumnSets.Add(Key, colSet);
                }                
                return colSet;
            }
        }
        #region Collection creation
        public DocRecordColumnCollection CreateCollection(string Key, string TextQualifier = null, bool IncludeKeyColumn = true)
        {
            var colSet = new DocRecordColumnCollection(Alias);
            if(TextQualifier != null)
                SetTextQualifier(TextQualifier);
            //colSet.TextQualifier = TextQualifier;
            //colSet.SetDelimiter(RecordDelimiter);
            ColumnSets.Add(Key, colSet);
            if (IncludeKeyColumn)
                colSet.AddColumn(DEFAULT_KEY_NAME);
            return colSet;
        }
        public DocRecordColumnCollection CreateCollection(string Key, string TextQualifier, params DocRecordColumnInfo[] columnInfos)
        {
            var colSet = CreateCollection(Key, TextQualifier);
            foreach (var col in columnInfos)
            {
                colSet.AddColumn(col);
            }
            return colSet;
        }
        public DocRecordColumnCollection CreateCollection(string Key, params DocRecordColumnInfo[] columnInfos)
        {
            return CreateCollection(Key, null, columnInfos);
        }
        public DocRecordColumnCollection CreateCollection(string Key, string TextQualifier, params string[] columnNames)
        {
            var colSet = CreateCollection(Key, TextQualifier);
            foreach (var col in columnNames)
            {
                colSet.AddColumn(col);
            }
            return colSet;
        }
        public DocRecordColumnCollection CreateCollection(string Key, params string[] columnNames)
        {
            return CreateCollection(Key, null, columnNames);
        }

        public DocRecordColumnCollection CreateCollection(string Key, string TextQualifier, bool AddKeyColumn, params string[] columnNames)
        {
            var colSet = CreateCollection(Key, TextQualifier, AddKeyColumn);            
            foreach (var col in columnNames)
            {
                colSet.AddColumn(col);
            }
            return colSet;
        }
        public DocRecordColumnCollection CreateCollection(string Key, bool AddKeyColumn, params string[] columnNames)
        {
            return CreateCollection(Key, null, AddKeyColumn, columnNames);
        }
        #endregion
        /// <summary>
        /// Default key to use if no match.
        /// </summary>
        public const string DEFAULT_KEY = "";
        /// <summary>
        /// Key Identifiers for various column sets in the file.
        /// <para>Key will be compared against file data using <see cref="BaseExtensions.Like(string, string, bool)"/> (Regular Expressions NOT escaped)</para>
        /// <para>NULL should be used as a key for default collection, if relevant.</para>
        /// </summary>
        public Dictionary<string, DocRecordColumnCollection> ColumnSets { get; private set; }
        /// <summary>
        /// Gets a DocRecord using the default Column Collection (<see cref="DEFAULT_KEY"/> ). If there is no default key, return null.
        /// </summary>
        /// <returns></returns>
        public override DocRecord GetBasicRecord()
        {
            DocRecordColumnCollection res;
            if (ColumnSets.TryGetValue(DEFAULT_KEY, out res))
                return new DocRecord(res) { CanWrite = CanWrite };
            return null;
        }
        /// <summary>
        /// Gets a DocRecord using a column collection associated with the key. Will error if there is no matching key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public DocRecord GetBasicRecord(string key)
        {
            var colSet = ColumnSets.First(kv => kv.Key.Like(key, false)).Value;
            return new DocRecord(colSet);
        }
        /// <summary>
        /// Attempt to Get the appropriate column set for a line of a file.
        /// </summary>
        /// <param name="DocLine"></param>
        /// <returns></returns>
        public override DocRecordColumnCollection GetRecordColumnInfos(string DocLine)
        {
            if (string.IsNullOrEmpty(DocLine))
                return null;
            bool exact = true;
            string k;
            IEnumerable<KeyValuePair<string, DocRecordColumnCollection>> kvSearch = ColumnSets;
            if (Format.In(DocRecordFormat.DELIMITED, DocRecordFormat.VARIABLE_WIDTH))            
                k = DocLine.Split(Delimiter.Value)[0];                            
            else if(Format == DocRecordFormat.SBSON)            
                k = SBSONHelper.GetKey(DocLine, FileEncoding);             
            else
            {
                k = DocLine;
                exact = false;
                kvSearch = ColumnSets.OrderByDescending(kv => kv.Key.Length); //most detailed search first.
                /*
                E.g. Keys: 
                142...
                14 ...
                 14...
                 would want to match on 142 first, although because of spacing, that may not be an issue as long as the first column is consistent size
             */
            }
            foreach(var kv in kvSearch)
            {
                string compare = kv.Key + (exact? string.Empty: "%");                
                if (compare == DEFAULT_KEY)
                {
                    if (string.IsNullOrEmpty(k))
                    {
                        if(exact)
                            return kv.Value;
                        return null;
                    }
                    continue;
                }
                if(compare.Like(k, false))
                {
                    return kv.Value;
                }
            }
            DocRecordColumnCollection res;
            if (ColumnSets.TryGetValue(DEFAULT_KEY, out res))
                return res;
            return null;
        }

        /// <summary>
        /// Sets <see cref="DocRecordColumnCollection.DefaultNullIfEmpty"/>,
        /// and also sets the <see cref="DocRecordColumnInfo.NullIfEmpty"/> for each column associated with the ColumnCollection
        /// </summary>
        /// <param name="nullifyEmpty"></param>
        /// <param name="SetDefault">If false, just sets the values on individual columns, not the collection's default.</param>
        /// <returns></returns>
        public override MetaDataBase SetEmptyIsNull(bool nullifyEmpty, bool SetDefault = true)
        {
            ColumnSets.Values.ForEach(Columns =>
            {
                if (SetDefault)
                    Columns.DefaultNullIfEmpty = nullifyEmpty;
                Columns.Columns.ForEach(c => c.NullIfEmpty = nullifyEmpty);
            });
            return this;
        }
        /// <summary>
        /// Sets <see cref="DocRecordColumnCollection.DefaultNullIfEmpty"/>,
        /// and also (conditionally) sets the <see cref="DocRecordColumnInfo.NullIfEmpty"/> for each column associated with the ColumnCollection that matches <paramref name="columnPredicate"/>.
        /// 
        /// </summary>
        /// <param name="NullifyEmpty"></param>
        /// <param name="columnPredicate"></param>
        /// <param name="SetDefault">If false, just sets the values on individual columns, not the collection's default.</param>
        /// <returns></returns>
        public override MetaDataBase SetEmptyIsNull(bool NullifyEmpty, Predicate<DocRecordColumnInfo> columnPredicate, bool SetDefault = true)
        {
            ColumnSets.Values.ForEach(Columns =>
            {
                if(SetDefault)
                    Columns.DefaultNullIfEmpty = NullifyEmpty;
                Columns.Columns.Where(c => columnPredicate(c)).ForEach(c => c.NullIfEmpty = NullifyEmpty);
            });
            return this;
        }
        /// <summary>
        /// Returns the Column Collection that matches this record, based on the key.
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public override DocRecordColumnCollection GetRecordColumnInfos(IRecord record)
        {
            if (record == null)
                return null;
            var k = record[0];

            foreach (var kv in ColumnSets)
            {
                if (kv.Key == null)
                {
                    if (string.IsNullOrEmpty(k))
                        return kv.Value;
                    continue;
                }
                if (kv.Key.Like(k, false))
                {
                    return kv.Value;
                }
            }
            DocRecordColumnCollection res;
            if (ColumnSets.TryGetValue(DEFAULT_KEY, out res))
                return res;
            return null;
        }
    
        /// <summary>
        /// MetaData for reading a file with multiple record types. (First Column data should specify which column set to use)
        /// </summary>
        /// <param name="FilePath"></param>
        /// <param name="Alias"></param>
        public MultiRecordDocMetaData(string FilePath, string Alias = null)
            :base(FilePath, Alias)
        {
            ColumnSets = new Dictionary<string, DocRecordColumnCollection>();
        }    
        
        /// <summary>
        /// Indicates that file has header information contained - hard coded to false as this doesn't really make sense for multi record.
        /// <para>Call <see cref="MetaDataBase.SetSkipLines(int)"/> if needed.</para>
        /// </summary>
        public override bool HasHeader => false;
        /// <summary>
        /// No support for headers in MultiRecord. SKip lines with <see cref="MetaDataBase.SkipLines"/> if needed.
        /// </summary>
        public override bool HeaderConfigured => false;
        /// <summary>
        /// Confirm that underlying column information is valid.
        /// </summary>
        public override bool Valid
        {
            get
            {
                if (!CheckFormatValid(AccessMode))
                    return false;
                
                if (ColumnSets.Count == 0)
                    return false;
                if (ColumnSets.Exists(cc => cc.Value.Valid == false || cc.Value.Count == 0))
                    return false;


                if (Format.In(DocRecordFormat.FIX_WIDTH, DocRecordFormat.RAGGED_RIGHT))
                {
                    bool valid = true;
                    ColumnSets.Values.ForEach(colSet =>
                    {
                        if (valid)
                        {
                            colSet.CheckForFixedWidthValid();
                            if (!colSet.CanUseAsFixedWidth)
                                valid = false;
                        }
                    });
                    if (!valid)
                        return false;
                }
                return true;
            }
        }



        /// <summary>
        /// Do not call for MultiRecord - Headers are not supported.
        /// </summary>
        /// <returns></returns>
        public override string GetHeader()
        {
            throw new NotImplementedException();
        }
    }
}
