using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    public interface IDataRecord
    {
        string this[int Position] { get; set; }
        string this[string Column] { get; set; }
        string this[string Column, string Alias] { get; set; }
        string GetBestMatch(string column, string alias);
        string this[IRecordColumnInfo column] { get; set; }
        string this[DocRecordColumnInfo column] { get; set; }
        DocRecordColumnCollection Columns { get; }
        bool HasColumn(string alias, string Column);
        /// <summary>
        /// Returns whether or not the IDataRecord has a column name, regardless of alias.
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <returns></returns>
        bool HasColumn(string ColumnName);
        bool TryGet(string ColumnName, out object result, string alias =null);
        bool TryGet(DocRecordColumnInfo columnInfo, out object result);
        void Configure(DocRecordColumnCollection owner, bool? canWrite, IList<object> parsedContent);
        /// <summary>
        /// ID for data record from its source (if provided).
        /// <para>Should be zero-based, but usage depends on source that populates the record.</para>        
        /// </summary>
        long? ID { get; }
    }
}
