using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// Record event changed.
    /// </summary>
    public class RecordChangedEventArgs : EventArgs
    {
        public DocRecord SourceRecord { get; private set; }
        /// <summary>
        /// Column that was changed.
        /// </summary>
        public DocRecordColumnInfo ColumnChanged { get; private set; }
        /// <summary>
        /// Value that column was changed from
        /// </summary>
        public string OldValue { get; private set; }
        /// <summary>
        /// Value that column was changed to.
        /// </summary>
        public string NewValue { get; private set; }
        /// <summary>
        /// Args passed to event handlers when a DocRecord column value is changed.
        /// </summary>
        /// <param name="columnModified"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <param name="source"></param>
        public RecordChangedEventArgs(DocRecordColumnInfo columnModified, string oldValue, string newValue, DocRecord source)
        {
            ColumnChanged = columnModified;
            OldValue = oldValue;
            NewValue = newValue;
            SourceRecord = source;
        }
    }
}
