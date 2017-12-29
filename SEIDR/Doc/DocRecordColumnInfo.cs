using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// Column information for Doc Reader/Writer
    /// </summary>
    public sealed class DocRecordColumnInfo
    {        
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
        /// <summary>
        /// For use with fixed width, cause column to end early. 
        /// <para>E.g., final column ends as soon as it reaches a newline instead of after <see cref="MaxLength"/> characters</para>
        /// <para>Note: last column in fixed width should account for the space taken by NewLine</para>
        /// </summary>
        public string EarlyTerminator { get; set; } = null;
        /// <summary>
        /// Name of the column
        /// </summary>
        public string ColumnName { get; internal set; }
        /// <summary>
        /// Alias of the column's owner
        /// </summary>
        public string OwnerAlias { get; internal set; }
        /// <summary>
        /// Creates a new Delimited Record Column info record, for getting information out of a delimited record
        /// </summary>
        /// <param name="Column"></param>
        /// <param name="Alias"></param>
        public DocRecordColumnInfo(string Column, string Alias)
        {
            ColumnName = Column;
            OwnerAlias = Alias;
        }
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
            if (other.ColumnName == ColumnName && other.OwnerAlias == OwnerAlias)
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
            return a?.ColumnName == b?.ColumnName && a?.OwnerAlias == b?.OwnerAlias;
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
                cols[i] = new DocRecordColumnInfo(ColumnList[i], Alias);
            }
            return cols;
        }
    }
}
