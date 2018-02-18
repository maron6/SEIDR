using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Windows.Controls;



namespace SEIDR.Dynamics.Windows
{
    /// <summary>
    /// Represents the Main window of the SEIDR.WindowMonitor application
    /// </summary>
    public interface SEIDR_Window
    {
        /// <summary>
        /// Updates the datagrid of the SEIDR_Window caller. If <paramref name="Callback"/> is populated, then it will be invoked in a number of minutes, depending on <paramref name="WaitPeriod"/> (5 if null).
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="pluginID">See the 'internalName' from Addon Setup: <see cref="SEIDR_WindowMenuAddOn.Setup(Configurations.BasicUser, Processing.Data.DBObjects.DatabaseConnection, string, Dictionary{string, object})"/></param>
        /// <param name="startingMenu">Optional context menu for further updates to the window.</param>
        /// <param name="Callback">A callback, parameterless void method delegate to be called after "<paramref name="WaitPeriod"/>" minutes. <para>
        /// Will be called only once, unless another call to either UpdateDisplay or <see cref="UpdateDisplayNoContextChange"/> is made
        /// </para>
        /// </param>
        /// <param name="WaitPeriod">Number of minutes to wait before calling <paramref name="Callback"/>. If null and callback is not null, will default to 5.<para>
        /// Ignored if Callback is null.</para></param>
        void UpdateDisplay(DataTable dt, int pluginID, ContextMenu startingMenu= null, Action Callback = null, ushort? WaitPeriod = null);
        /// <summary>
        /// Updates the datagrid of the SEIDR_Window caller. Should only be used in the context menu 'startingMenu' after doing an initial Update.
        /// <para>Also, if a callback was specified originally, then populating waitPeriod will cause the same Callback to be called </para>
        /// <para> again. But it must be explicitly set in this case. </para>
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="waitPeriod">If Callback was specified originally, will call the same callback again in waitPeriod minutes.</param>
        void UpdateDisplayNoContextChange(DataTable dt, ushort? waitPeriod = null);
        /// <summary>
        /// Update the window label at the bottom right of the SEIDR_Window caller 
        /// </summary>
        /// <param name="pluginName"></param>
        /// <param name="LabelText"></param>
        void UpdateLabel(string pluginName, string LabelText);
    }
}
