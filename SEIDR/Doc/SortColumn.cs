using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// Basic helper for <see cref="DocSorter"/>
    /// </summary>
    public sealed class SortColumn : IRecordColumnInfo
    {
        readonly int _Position;
        /// <summary>
        /// Sort order for <see cref="DocSorter"/>
        /// </summary>
        public bool SortASC { get; set; } = true;
        /// <summary>
        /// Column position for getting a record from an <see cref="IRecord"/>
        /// </summary>
        public int Position => _Position;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="position"></param>
        /// <param name="ascOrder"></param>
        public SortColumn(int position, bool ascOrder = true)
        {
            _Position = position;
            SortASC = ascOrder;
        }
    }
}
