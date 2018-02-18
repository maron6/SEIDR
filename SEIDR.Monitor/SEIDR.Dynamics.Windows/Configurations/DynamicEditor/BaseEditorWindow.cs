using SEIDR.Dynamics.Configurations.UserConfiguration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;


namespace SEIDR.Dynamics.Configurations.DynamicEditor
{
    public abstract class BaseEditorWindow<windowConfig>: BasicSessionWindow, INotifyPropertyChanged
        where windowConfig : class, iWindowConfiguration
    {
        windowConfig edit;
        public windowConfig Edit
        {
            get { return edit; }
            set
            {
                edit = value;
                InvokePropertyChanged();
            }
        }
        /// <summary>
        /// Set up the window, populating the Edit property
        /// </summary>
        /// <param name="toEdit"></param>
        /// <param name="RequiredPermission"></param>
        public BaseEditorWindow(windowConfig toEdit, BasicUserPermissions RequiredPermission)
            :base(SafeWindow: true, requiredAccessMode: MultiUserAccess, requiredPermission: RequiredPermission)
        {
            Edit = toEdit;              
        }
        public BaseEditorWindow()
            :base(SafeWindow:true, requiredAccessMode: MultiUserAccess) { }

        /// <summary>
        /// Calls <see cref="BasicSessionWindow.Finish(bool)"/> with value true, but checks to make sure that <see cref="Edit"/> is not null.
        /// <para>Will throw an <see cref="InvalidOperationException"/> if Edit is null</para>
        /// </summary>
        public void Accept()
        {
            if (edit == null)
                throw new InvalidOperationException(typeof(windowConfig).Name + " - Record is null.");
            Finish(true);
            
        }
        /// <summary>
        /// Sets dialog result to false (if opened as a dialog) and then calls <see cref="BasicSessionWindow.Finish(bool)"/> with false.
        /// </summary>
        public void Revert()
        {
            edit = null;
            Finish(false);
        }
        protected void InvokePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string caller = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
