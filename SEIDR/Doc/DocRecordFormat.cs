using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// Describe format for reading/writing to files using a <see cref="MetaDataBase"/> derived class.
    /// </summary>
    public enum DocRecordFormat
    {
        /// <summary>
        /// Delimited Output format when formatting a record as a string.
        /// </summary>
        DELIMITED,
        /// <summary>
        /// Set width for all columns when formatting record as a string
        /// </summary>
        FIX_WIDTH,
        /// <summary>
        /// Set width for all columns when formatting record as a string, except the last column does not need to be padded.
        /// </summary>
        RAGGED_RIGHT,
        /// <summary>
        /// Hybrid of Delimited/RaggedRight
        /// </summary>
        VARIABLE_WIDTH,
        /// <summary>
        /// Bit Condensed Object notation. Variation of BSON
        /// </summary>
        BitCON,
        /// <summary>
        /// Variation of Binary JSON
        /// </summary>
        BSON
    }
}
