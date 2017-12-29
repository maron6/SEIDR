using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    public interface IRecord
    {
        bool HasColumn(string alias, string Column);
        string this[string alias, string column] { get; set; }
        string this[int index] { get; set; }

        ulong? GetPartialHash(bool RollingHash, bool ExcludeEmpty, bool includeNull, params DocRecordColumnInfo[] columnsToHash);
    }
}
