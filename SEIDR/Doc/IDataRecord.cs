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
        bool TryGet(DocRecordColumnInfo columnInfo, out object result);
        void Configure(DocRecordColumnCollection owner, bool? canWrite, IList<object> parsedContent);
    }
}
