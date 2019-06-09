using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// For some methods in DocRecord, and derived classes, to specify type of data.
    /// </summary>
    public enum DocRecordColumnType
    {
        /// <summary>
        /// Variable character
        /// </summary>
        Varchar = 0,
        /// <summary>
        /// NonVariable characters, indicates length should be constant (padded if needed). Use with <see cref="DocRecordColumnInfo.MaxLength"/>
        /// </summary>
        NVarchar = 1,
        Tinyint = 2,
        Smallint = 3,
        Int = 4,
        Bigint = 5,
        DateTime = 6,
        Date = 7,
        Decimal
    }
}
