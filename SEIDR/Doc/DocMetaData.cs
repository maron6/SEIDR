using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// MetaData for configuring other classes in the Doc namespace (e.g., <see cref="DocReader"/> )
    /// </summary>
    public sealed class DocMetaData : MetaDataBase, ISingleRecordTypeMetaData
    {
        /// <summary>
        /// Treats the MetaData as the underlying ColumnsCollection
        /// </summary>
        /// <param name="data"></param>
        public static implicit operator DocRecordColumnCollection(DocMetaData data)
        {
            return data.Columns;
        }
        /// <summary>
        /// The columns from the file
        /// </summary>
        public DocRecordColumnCollection Columns { get; private set; }
        /// <summary>
        /// Returns <see cref="Columns"/>. 
        /// </summary>
        /// <param name="DocLine"></param>
        /// <returns></returns>
        public override DocRecordColumnCollection GetRecordColumnInfos(string DocLine)
        {
            return Columns;
        }
        /// <summary>
        /// Returns <see cref="Columns"/>. 
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public override DocRecordColumnCollection GetRecordColumnInfos(IDataRecord record)
        {
            return Columns;
        }

        /// <summary>
        /// Tries to set the doc to fixed width mode. Returns true if value is updated.
        /// </summary>
        /// <param name="useFixedWidth"></param>
        /// <returns></returns>
        public bool TrySetFixedWidthMode(bool useFixedWidth)
        {
            Columns.CheckForFixedWidthValid();
            if (Columns.CanUseAsFixedWidth)
            {
                if (useFixedWidth)
                    Format = DocRecordFormat.FIX_WIDTH;
                else
                    Format = DocRecordFormat.DELIMITED;
            }
            else if (!useFixedWidth)
                Format = DocRecordFormat.DELIMITED;
            else
                return false;
            
            return true;
        }
        /// <summary>
        /// Clones the various settings of this file, but for a second file path. (E.g., to take input from a file and then modify some of the data and output it in a new location, but without changing metadata)
        /// </summary>
        /// <param name="NewFilePath"></param>
        /// <param name="WriteMode"></param>
        /// <returns></returns>
        public DocMetaData CloneForNewFile(string NewFilePath, bool? WriteMode = null)
        {
            var dm = new DocMetaData(NewFilePath)
            {
                CanWrite = WriteMode ?? this.CanWrite,
                AccessMode = this.AccessMode,
                SkipLines = this.SkipLines,         
                FileEncoding = this.FileEncoding,                
            };            
            dm.SetHasHeader(this.HasHeader)
                .SetPageSize(PageSize)                
                .SetMultiLineEndDelimiters(MultiLineEndDelimiter);
            if (this.Delimiter.HasValue)
                dm.SetDelimiter(this.Delimiter.Value);
            dm.CopyDetailedColumnCollection(this);
            return dm;                
        }
      
        /// <summary>
        /// Columns are in a valid state.
        /// </summary>
        public override bool HeaderConfigured => Columns.Valid;
        /// <summary>
        /// MetaData valid for file usage.
        /// </summary>       
        public override bool Valid
        {
            get
            {
                if (!CheckFormatValid(AccessMode))
                    return false;
                if (Columns.Count == 0 || !Columns.Valid)
                    return false;
                if(Format == DocRecordFormat.FIX_WIDTH || Format == DocRecordFormat.RAGGED_RIGHT)
                {
                    Columns.CheckForFixedWidthValid();
                    return Columns.CanUseAsFixedWidth;
                }
                return true;
            }
        }
       
        /// <summary>
        /// Creates meta data for the given file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="alias"></param>
        public DocMetaData(string file, string alias = null)
            :base(file, alias)
        {           
            Columns = new DocRecordColumnCollection(Alias);
        }
        /// <summary>
        /// Creates meta Data for the given file
        /// </summary>
        /// <param name="DirectoryPath"></param>
        /// <param name="FileName"></param>
        /// <param name="alias"></param>
        public DocMetaData(string DirectoryPath, string FileName, string alias = null)
            :this(Path.Combine(DirectoryPath, FileName), alias)
        {

        }
        /// <summary>
        /// Indicate whether or not the file has a header.
        /// </summary>
        public override bool HasHeader => _HasHeader;

        string ISingleRecordTypeMetaData.FilePath => base.FilePath;

        bool _HasHeader = true;
        /// <summary>
        /// Sets <see cref="HasHeader"/>
        /// </summary>
        /// <param name="headerIncluded"></param>
        /// <returns></returns>
        public DocMetaData SetHasHeader(bool headerIncluded)
        {
            _HasHeader = headerIncluded;
            return this;
        }
        /// <summary>
        /// Get the header line.
        /// </summary>
        /// <returns></returns>
        public override string GetHeader()
        {
            Columns.CheckSort();
            StringBuilder fmt = new StringBuilder();
            for (int i = 0; i <= Columns.LastPosition; i++)
            {
                var col = Columns.Columns[i];
                if (col == null && Delimiter.HasValue)
                {
                    fmt.Append(Delimiter);
                    continue;
                }
                if (FixWidthMode)
                {
                    int justify = col.LeftJustify ? -col.MaxLength.Value : col.MaxLength.Value;
                    fmt.Append("{" + i + "," + justify + "}");
                }
                else
                {
                    bool textQual = false;
                    if (col.ColumnName.Contains(Delimiter.Value))
                        textQual = true;
                    if (textQual)
                        fmt.Append(TextQualifier);

                    fmt.Append("{" + i + "}");

                    if (textQual)
                        fmt.Append(TextQualifier);
                    if (i < Columns.LastPosition)
                        fmt.Append(Delimiter);
                }
            }
            if (LineEndDelimiter != null)
                fmt.Append(LineEndDelimiter);
            else
                fmt.Append(Environment.NewLine);
            return string.Format(fmt.ToString(), Columns.Columns.Select(c => c.ColumnName).ToArray());
        }        
      

        /// <summary>
        /// Adds the strings to <see cref="MetaDataBase.MultiLineEndDelimiter"/>, and sorts it so that super sets are earlier. 
        /// <para>E.g., ensures \r\n comes before \r or \n, while the order of \r and \n are arbitrary.</para>
        /// </summary>
        /// <param name="endingToAdd"></param>
        /// <returns></returns>
        public override MetaDataBase AddMultiLineEndDelimiter(params string[] endingToAdd)
        {
            List<string> l;
            if (!string.IsNullOrEmpty(LineEndDelimiter))
                l = new List<string>(endingToAdd.Include(LineEndDelimiter));
            else
                l = new List<string>(endingToAdd);

            if (MultiLineEndDelimiter != null)
                l.AddRange(MultiLineEndDelimiter);
            l.Sort((a, b) =>
            {
                if (a == null || b == null)
                    return 0;
                if (a.IsSuperSet(b)) //Earlier sort position
                    return -1;
                if (a.IsSubset(b))
                    return 1;
                return 0;
            });
            MultiLineEndDelimiter = l.Where(ln => !string.IsNullOrEmpty(ln)).Distinct().ToArray();
            return this;
        }
        /// <summary>
        /// Use if there may be a mixture of /r/n, /r, /n, etc   
        /// </summary>
        /// <param name="endings"></param>
        public override MetaDataBase SetMultiLineEndDelimiters(params string[] endings)
        {
            List<string> l;
            if (!string.IsNullOrEmpty(LineEndDelimiter))
                l = new List<string>(endings.Include(LineEndDelimiter));
            else
                l = new List<string>(endings);

            l.Sort((a, b) =>
            {
                if (a.IsSuperSet(b)) //Earlier sort position
                    return -1;
                if (a.IsSubset(b))
                    return 1;
                return 0;
            });
            MultiLineEndDelimiter = l.Where(ln => !string.IsNullOrEmpty(ln)).ToArray();
            return this;
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
            if(SetDefault)
                Columns.DefaultNullIfEmpty = nullifyEmpty;
            Columns.Columns.ForEach(c => c.NullIfEmpty = nullifyEmpty);
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
            if(SetDefault)
                Columns.DefaultNullIfEmpty = NullifyEmpty;
            Columns.Columns.Where(c => columnPredicate(c)).ForEach(c => c.NullIfEmpty = NullifyEmpty);
            return this;
        }

        /// <summary>
        /// Adds basic columns to be delimited by <see cref="Delimiter"/>.
        /// </summary>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public DocMetaData AddDelimitedColumns(params string[] columnNames)
        {
            foreach(var col in columnNames)
            {
                Columns.AddColumn(col);
            }
            return this;
        }
        /// <summary>
        /// Adds one or more populated <see cref="DocRecordColumnInfo"/> instances to the Columns Collection
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public DocMetaData AddDetailedColumn(params DocRecordColumnInfo[] column)
        {
            foreach(var col in column)
            {
                Columns.AddColumn(col);
            }
            return this;
        }
        /// <summary>
        /// Copies the columns from the collection to the end of this doc's column collection. 
        /// <para>Note: Will replace the alias.</para>
        /// </summary>
        /// <param name="columnCollection"></param>
        /// <returns></returns>
        public DocMetaData AddDetailedColumnCollection(DocRecordColumnCollection columnCollection)
        {
            foreach (var col in columnCollection)
            {
                Columns.AddColumn(col.ColumnName, col.MaxLength, col.LeftJustify, col.TextQualify, col.DataType);
            }
            return this;
        }
        /// <summary>
        /// Remove the columns from the column collection. Should be done *before* reading or writing - may lead to DocRecords being in an inconsistent state otherwise.
        /// </summary>
        /// <param name="toRemove"></param>
        /// <returns></returns>
        public DocMetaData RemoveColumn(params DocRecordColumnInfo[] toRemove)
        {
            foreach(var c in toRemove)
            {
                Columns.RemoveColumn(c);
            }            
            return this;
        }
        /// <summary>
        /// Attempts to remove the specified column.
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="alias"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public DocMetaData RemoveColumn(string ColumnName, string alias = null, int position = -1)
        {
            Columns.RemoveColumn(alias, ColumnName, position);
            return this;
        }
        /// <summary>
        /// Copies the columns from the collection to the end of this doc's column collection. Maintains alias.
        /// </summary>
        /// <param name="columnCollection"></param>
        /// <returns></returns>
        public DocMetaData CopyDetailedColumnCollection(DocRecordColumnCollection columnCollection)
        {
            foreach (var col in columnCollection)
            {
                Columns.CopyColumnIntoCollection(col);
            }            
            return this;
        }
        /// <summary>
        /// Creates a new <see cref="DocRecordColumnInfo"/> and adds it to the Columns collection
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="MaxLength">optional limit</param>
        /// <param name="TextQual"></param>
        /// <param name="DataType"></param>        
        /// <returns></returns>
        public DocMetaData AddColumn(string ColumnName, int? MaxLength = null, bool TextQual = false, DocRecordColumnType DataType = DocRecordColumnType.Unknown)
        {
            if (FixWidthMode && MaxLength == null)
                throw new ArgumentNullException(nameof(MaxLength), $"MetaData indicates fixed width mode, but a length was not provided for new column '{ColumnName}'");
            Columns.AddColumn(ColumnName, MaxLength, textQualify: TextQual, dataType: DataType);
            return this;
        }

      
    }   
}
