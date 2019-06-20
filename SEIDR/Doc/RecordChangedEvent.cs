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
        /// <para>(DocRecord will be the event's sender)</para>
        /// </summary>
        /// <param name="columnModified"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        public RecordChangedEventArgs(DocRecordColumnInfo columnModified, string oldValue, string newValue)
        {
            ColumnChanged = columnModified;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
    /// <summary>
    /// For responding when a <see cref="TypedDataRecord"/> is modified.
    /// </summary>
    public class TypedRecordChangedEventArgs : EventArgs
    {
        public DocRecordColumnInfo ColumnChanged { get; private set; }
        public DataItem OldValue { get; private set; }
        public DataItem NewValue { get; private set; }
        public TypedRecordChangedEventArgs(DocRecordColumnInfo columnModified, DataItem oldValue, DataItem newValue)
        {
            ColumnChanged = columnModified;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
