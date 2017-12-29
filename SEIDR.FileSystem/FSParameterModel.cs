using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.FileSystem
{
    class FSParameterModel
    {
        public int Profile_OperationID { get; set; }
        public string DestinationFolder { get; set; }
        /// <summary>
        /// * Replaced with actual file name. &gtYYYY>
        /// </summary>
        public string DestinationFileNameFormat { get; set; }
        public string DateFormat { get; set; }
        public string GrabAllFilter { get; set; }
        public FileOperation? FileOperation { get; set; }
        public bool UseBatchDate { get; set; } = true;
    }
}
