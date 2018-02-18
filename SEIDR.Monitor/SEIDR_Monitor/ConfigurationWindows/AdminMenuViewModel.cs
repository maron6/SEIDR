using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Configurations.UserConfiguration;
using static SEIDR.Dynamics.Configurations.UserConfiguration.BasicUserPermissions;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SEIDR.WindowMonitor.ConfigurationWindows
{
    public class AdminMenuViewModel: INotifyPropertyChanged
    {
        public event EventHandler Reconfigured;        
        public bool Admin                   { get; private set; }
        public bool TeamEditorPermission    { get; private set; }
        public bool UserEditorPermission    { get; private set; }

        public bool QueryMerge          { get; private set; } //Can get other teams to edit as available after loading window (Session window -> Current user will be available
        public bool ConnectionMerge     { get; private set; }
        public bool ContextMerge        { get; private set; }
        public bool ContextAddonMerge   { get; private set; }
        public bool WindowAddonMerge    { get; private set; }

        public AdminMenuViewModel(WindowUser configured)
        {
            ReConfigure(configured);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void ReConfigure(WindowUser newUser)
        {
            Admin = newUser.Admin;
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(Admin)));
            UserEditorPermission = newUser.CheckPermission(UserEditor);
            invoke(nameof(UserEditorPermission));
            TeamEditorPermission = newUser.CheckPermission(TeamEditor);
            invoke(nameof(TeamEditorPermission));

            ConnectionMerge = newUser.CheckPermission(DatabaseConnectionEditor, TeamSettingEditor);
            QueryMerge = ConnectionMerge && newUser.CheckPermission(BasicUserPermissions.QueryEditor, TeamSettingEditor);
            ContextMerge = QueryMerge && newUser.CheckPermission(BasicUserPermissions.ContextMenuEditor, TeamSettingEditor);
            WindowAddonMerge = ContextAddonMerge = newUser.CheckPermission(BasicUserPermissions.AddonEditor, TeamSettingEditor, AddonUser);

            invoke(nameof(ConnectionMerge));
            invoke(nameof(QueryMerge));
            invoke(nameof(ContextMerge));
            invoke(nameof(WindowAddonMerge));
            Reconfigured?.Invoke(this, EventArgs.Empty);
        }
        private void invoke(string prop) { PropertyChanged.Invoke(this, new PropertyChangedEventArgs(prop)); }        
    }
}
