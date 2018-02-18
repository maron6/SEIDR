using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Dynamics.Configurations
{
    public interface iWindowConfiguration
    {
        /// <summary>
        /// Scope description for the Configuration Record (should basically be constant)
        /// </summary>       
        WindowConfigurationScope MyScope { get; }
        /// <summary>
        /// ID that should be used for actual comparisons/finding the record to update..
        /// <para>If overridingSupportPaging, should also override the Add</para>
        /// </summary>
        int? ID { get; set; }
        /// <summary>
        /// The ID that users will see
        /// </summary>
        string Key { get; set; }
        /// <summary>
        /// Any description to be used by users for extra detail
        /// </summary>
        string Description { get; set; }
        /// <summary>
        /// For use if paging is supported.
        /// <para>E.g., if saved to a DB instead of straight to a file</para>
        /// </summary>
        int RecordVersion { get; set; }
        /// <summary>
        /// Should load as false, but allow being set when Update is called.
        /// <para>Will allow filtering on records that need to be updated to reduce number of DB calls if no bulk insert/update</para>
        /// </summary>        
        bool Altered { get; set; }
    }
}
