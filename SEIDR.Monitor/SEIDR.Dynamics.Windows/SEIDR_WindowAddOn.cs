using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data;
//using SEIDR.Processing.Data.DBObjects;
using SEIDR.DataBase;
using SEIDR.Dynamics.Configurations.UserConfiguration;

namespace SEIDR.Dynamics.Configurations
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
        /// <summary>
        /// Gets a datarow for editing to be used for storing parameters to pass when executing.
        /// </summary>
        /// <returns></returns>
        DataRowView GetParameterInfo();
        /// <summary>
        /// Person calling the app
        /// </summary>
        BasicUser Caller { get; set; }
        /// <summary>
        /// Method to be called if mutliselect is true in meta data
        /// </summary>
        /// <param name="selectedRows"></param>
        /// <returns>Message to display after call</returns>
        string Execute(IEnumerable<DataRowView> selectedRows, Dictionary<string, object> Parameters);
        /// <summary>
        /// Method to be called if multiselect is false. Just to make it simpler in the addon. Leave the unused one as unimplemented 
        /// </summary>
        /// <param name="selectedRow"></param>
        /// <returns></returns>
        string Execute(DataRowView selectedRow, Dictionary<string, object> Parameters);
    }

    public class SEIDR_WindowAddOn_ParameterHelper: IDisposable
    {
        DataTable dt;
        public SEIDR_WindowAddOn_ParameterHelper()
        {
            dt = new DataTable();
        }
        public void AddParameter(string Name, Type t, string Tooltip = null, object defaultValue = null)
        {
            dt.Columns.Add(new DataColumn
            {
                ColumnName = Name,
                Caption = Tooltip,
                DataType = t,
                DefaultValue = defaultValue ?? DBNull.Value
            });
        }

        public void Dispose()
        {
            ((IDisposable)dt).Dispose();
        }

        public DataRowView GetParameters()
        {
            dt.Rows.Add(dt.NewRow());
            return dt.AsDataView()[0];
        }
    }
    /// <summary>
    /// Meta data for use in the SEIDR.Window when setting up the calls for the SEIDR_WindowAddOn.
    /// <para>
    /// Can be populated with [ExportMetaData] from system component model
    /// </para>
    /// </summary>
    public interface SEIDR_WindowAddon_MetaData
    {
        /// <summary>
        /// The name of the addon for identification in SEIDR.Window
        /// </summary>
        string AddonName { get; }
        /// <summary>
        /// Help ensure unique
        /// </summary>
        [DefaultValue("00000000-0000-0000-0000-000000000000")]
        string Guid { get; }
        /// <summary>
        /// UI tooltip
        /// </summary>
        [DefaultValue(null)]
        string Description { get; }
        /// <summary>
        /// If true, will call get Parameter Info for populating a dictionary to be passed when executing
        /// </summary>
        [DefaultValue(true)]
        bool NeedParameterMapping { get; }
        ///// <summary>
        ///// Display name for users when deciding what value to give to IDName
        ///// </summary>
        //[DefaultValue(null)]
        //string IDNameParameterName { get; }
        ///// <summary>
        ///// Tooltip for the ID Name
        ///// </summary>
        //[DefaultValue(null)]
        //string IDNameParameterTooltip { get;  }
        ///// <summary>
        ///// Display name for users when deciding what value to give IDValue
        ///// </summary>
        //[DefaultValue(null)]
        //string IDValueParameterName { get; }
        //[DefaultValue(null)]
        //string IDValueParameterTooltip { get;  }
        /// <summary>
        /// If true, multiple rows will be passed in Execute
        /// </summary>
        [DefaultValue(false)]
        bool MultiSelect { get;  }
        /// <summary>
        /// If true, will not show for users who do not have permission to use addons using session cache
        /// </summary>
        [DefaultValue(false)]
        bool RequireSessionCache { get; }
        /// <summary>
        /// Limit users to admins or members of the team. Or members with the permission added.
        /// </summary>
        [DefaultValue(null)]
        string Team { get; }
        /// <summary>
        /// If true, checks that the user is either a super user or explicitly has permission for this addon name
        /// </summary>
        [DefaultValue(false)]
        bool RequirePermission { get; }
    }
}
