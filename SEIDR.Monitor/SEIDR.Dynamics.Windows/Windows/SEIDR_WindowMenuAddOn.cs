using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using SEIDR.Processing.Data.DBObjects;
using SEIDR.DataBase;
using SEIDR.Dynamics.Configurations;
using System.Windows.Controls;
using System.Data;
using System.ComponentModel.Composition;
using System.ComponentModel;
using SEIDR.Dynamics.Configurations.UserConfiguration;

namespace SEIDR.Dynamics.Windows
{
    /// <summary>
    /// Interface for MEF - Create a menu that can be opened from the plugin menu. May then open up windows or perform other operations when clicked.
    /// </summary> 
    //[InheritedExport]
    public interface SEIDR_WindowMenuAddOn
    {
        /// <summary>
        /// Take a Basic user (to allow double checking permissions of any kind), Database connection for any queries, 
        /// <para>and parameters set by User in SEIDR_Window
        /// </para>
        /// </summary>
        /// <param name="User">Basic user information as defined in SEIDR.Window</param>
        /// <param name="Connection">SQL Server connection info, with Trusted Connection(Windows Authentication)</param>
        /// <param name="internalName">Should be passed to callerWindow when calling it.</param>
        /// <param name="setParameters">User set paramters based on paramterInfo provided in Meta data</param>
        /// <returns></returns>
        MenuItem Setup(BasicUser User, DatabaseConnection Connection, int internalID, Dictionary<string, object> setParameters);
        /// <summary>
        /// Main window of calling application. Can call UpdateDisplay with a datatable and the name of this plugin from the meta data to 
        /// <para>change the display on the main window, and also setup context menus that have this addon as a parent.
        /// </para>
        /// </summary> 
        SEIDR_Window callerWindow { set; }
        /// <summary>
        /// Returns the parameter info for passing to MenuItem setup
        /// </summary>
        /// <returns></returns>
        Dictionary<string, Type> GetParameterInfo();
    }
    /// <summary>
    /// Meta data to be implemented by export attributes for Window addons.
    /// </summary>
    public interface SEIDR_WindowMenuAddOn_MetaData
    {        
        /// <summary>
        /// Unique name for this plugin
        /// </summary>
        string Name { get; }
        /// <summary>
        /// If true, check that the user has permission using this addon's name. Else, assume user can use addon.
        /// </summary>
        [DefaultValue(true)]
        bool RequirePermission { get; }
        /// <summary>
        /// If true, users will need to map parameters from the the method on your addon in order to use it. 
        /// <para>See GetParameterInfo</para>
        /// </summary>
        [DefaultValue(true)]
        bool HasParameterMapping { get; }
        /// <summary>
        /// If not null, requires that the user is either a super admin (admin level is 0 or 1) or member of the specified team.
        /// <para>If don't want to check, don't export a value or explicitly export null</para>
        /// </summary>
        [DefaultValue(null)]
        string Team { get; }
        /// <summary>
        /// If true, users should not be able to create more than one setup for this plug in
        /// <para>
        /// If false, users will be able to create multiple setups for the addon. E.g., multiple database connections</para>
        /// </summary>
        [DefaultValue(true)]
        bool Singleton { get; }
        /// <summary>
        /// If true, plugin has potential to update the datatable on the SEIDR main window. Allows setup of context menus 
        /// </summary>
        [DefaultValue(false)]
        bool UsesCaller { get; }
        /// <summary>
        /// If true, user will not be able to access plugin unless they have access to set values in the sesssion cache.
        /// <para>
        /// Use in cases like expectation that plugin relies on information from another plugin (e.g. context -> Window version)
        /// and needs to have the information set in the session cache.
        /// </para>
        /// </summary>
        [DefaultValue(false)]
        bool RequireSessionCache { get; }
    }

    public class SEIDR_WindowAddon_SettingConverter<T> where T : SEIDR_WindowAddon_MetaData
    {
        /// <summary>
        /// Creates a data row for use in an editable dashboard...
        /// </summary>
        /// <param name="info"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public DataRow Convert(T info, out ComboDisplay[] x)
        {
            DataTable dt = new DataTable();
            var temp = new List<ComboDisplay>();
            var props = info.GetType().GetProperties();
            foreach(var prop in props)
            {
                //if(prop.PropertyType != ComboBo)
            }
            DataTable t = new T[] { info }.ToDataTable();
            x = temp.ToArray();
            return dt.Rows[0];
        }
    }
}
