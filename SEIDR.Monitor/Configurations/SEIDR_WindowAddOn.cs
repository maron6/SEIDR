using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data;
using Ryan_UtilityCode.Processing.Data.DBObjects;

namespace Ryan_UtilityCode.Dynamics.Configurations
{
    /// <summary>
    /// For use by SEIDR.Window as an Addon with the context menu
    /// </summary>
    public interface SEIDR_WindowAddOn
    {
        /// <summary>
        /// Connection used by query that populated the datatable which will pass Rows
        /// </summary>
        DatabaseConnection Connection { get; set; }
        string IDName { get; set; }
        /// <summary>
        /// A numeric ID for indicating command or state, depending on your description.
        /// </summary>
        int? IDValue { get; set; }
        /// <summary>
        /// Person calling the app
        /// </summary>
        BasicUser Caller { get; set; }
        /// <summary>
        /// Method to be called if mutliselect is true in meta data
        /// </summary>
        /// <param name="selectedRows"></param>
        /// <returns>Message to display after call</returns>
        string Execute(DataRowView[] selectedRows);
        /// <summary>
        /// Method to be called if multiselect is false. Just to make it simpler in the addon. Leave the unused one as unimplemented 
        /// </summary>
        /// <param name="selectedRow"></param>
        /// <returns></returns>
        string Execute(DataRowView selectedRow);
    }
    /// <summary>
    /// Meta data for use in the SEIDR.Window when setting up the calls for the SEIDR_WindowAddOn.
    /// <para>
    /// Can be populated with [ExportMetaData] from system component model
    /// </para>
    /// </summary>
    public interface SEIDR_WindowAddon_MetaData
    {
        string AddonName { get; }
        /// <summary>
        /// UI tooltip
        /// </summary>
        [DefaultValue(null)]
        string Description { get; }
        /// <summary>
        /// Display name for users when deciding what value to give to IDName
        /// </summary>
        [DefaultValue(null)]
        string IDNameParameterName { get; }
        [DefaultValue(null)]
        string IDNameParameterTooltip { get;  }
        /// <summary>
        /// Display name for users when deciding what value to give IDValue
        /// </summary>
        [DefaultValue(null)]
        string IDValueParameterName { get; }
        [DefaultValue(null)]
        string IDValueParameterTooltip { get;  }
        /// <summary>
        /// If true, multiple rows will be passed in Execute
        /// </summary>
        [DefaultValue(false)]
        bool MultiSelect { get;  }
    }
}
