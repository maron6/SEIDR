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
        /// Indicates type of data
        /// </summary>
        public DocRecordColumnType DataType { get; set; } = DocRecordColumnType.Unknown;
        /// <summary>
        /// Optional formatting for certain data types.
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
        
    }
}
