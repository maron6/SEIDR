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
    public class MultiRecordDocMetaData : MetaDataBase
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
                    colSet = new DocRecordColumnCollection(Alias)
                    {
                        LineEndDelimiter = this.LineEndDelimiter,
                    };
                    colSet.SetDelimiter(RecordDelimiter);
                    ColumnSets.Add(Key, colSet);
                }                
                return colSet;
            }
        }
        #region Collection creation
        public DocRecordColumnCollection CreateCollection(string Key, string TextQualifier = null, bool IncludeKeyColumn = true)
        {
            var colSet = new DocRecordColumnCollection(Alias);
            colSet.TextQualifier = TextQualifier;
            colSet.SetDelimiter(RecordDelimiter);
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
            colSet.SetDelimiter(RecordDelimiter);            
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
        /// Attempt to Get the appropriate column set for a line of a file.
        /// </summary>
        /// <param name="DocLine"></param>
        /// <returns></returns>
        public override DocRecordColumnCollection GetRecordColumnInfos(string DocLine)
        {
            if (string.IsNullOrEmpty(DocLine))
                return null;
            var k = DocLine.Split(RecordDelimiter)[0];
            
            foreach(var kv in ColumnSets)
            {
                if (kv.Key == DEFAULT_KEY)
                {
                    if (string.IsNullOrEmpty(k))
                        return kv.Value;
                    continue;
                }
                if(kv.Key.Like(k, false))
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
        /// Delimiter. Multi Record DocMetaData must be delimited.
        /// </summary>
        public char RecordDelimiter { get; set; } = '|';
        /// <summary>
        /// Delimiter for the records in the Document.
        /// </summary>
        public override char? Delimiter => RecordDelimiter;
        /// <summary>
        /// Used for line endings, unless <see cref="MetaDataBase.ReadWithMultiLineEndDelimiter"/> is true.
        /// </summary>
        public override string LineEndDelimiter
        {
            get { return _LineEnd; }
        }
        string _LineEnd = Environment.NewLine;
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
                if (ColumnSets.Count == 0)
                    return false;
                if (ColumnSets.Exists(cc => cc.Value.Valid == false))
                    return false;
                return true;
            }
        }


        public override MetaDataBase SetDelimiter(char delimiter)
        {
            RecordDelimiter = delimiter;
            return this;
        }

        public override MetaDataBase SetLineEndDelimiter(string endLine)
        {
            _LineEnd = endLine;
            return this;
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
