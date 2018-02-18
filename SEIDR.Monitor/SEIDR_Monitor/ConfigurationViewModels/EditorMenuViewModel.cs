using SEIDR.Dynamics;
using SEIDR.Dynamics.Configurations;
using SEIDR.Dynamics.Configurations.AddonConfiguration;
using SEIDR.Dynamics.Configurations.ContextMenuConfiguration;
using SEIDR.Dynamics.Configurations.DatabaseConfiguration;
using SEIDR.Dynamics.Configurations.QueryConfiguration;
using SEIDR.Dynamics.Configurations.UserConfiguration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static SEIDR.WindowMonitor.MonitorConfigurationHelpers.LibraryManagement;

namespace SEIDR.WindowMonitor.ConfigurationViewModels
{
    public class EditorMenuViewModel: INotifyPropertyChanged
    {
        public event EventHandler Reconfigured;
        
        public bool EditQuery           { get; private set; }
        public bool UseTeamQuery        { get; private set; } = true;
        public bool EditConnections     { get; private set; }
        public bool EditTeamConnections { get; private set; }
        public bool EditContextMenu     { get; private set; }
        public bool EditTeamContextMenu { get; private set; }
        public bool EditAddons          { get; private set; }
        public bool EditTeamAddons      { get; private set; }

        public EditorMenuViewModel(WindowUser configured)
        {
            ReConfigure(configured);
        }
        private void invoke(string prop) { PropertyChanged.Invoke(this, new PropertyChangedEventArgs(prop)); }

        public event PropertyChangedEventHandler PropertyChanged;
        //ToDo: Need a root Model, with collection under root? Maybe?
        public ObservableCollection<ConfigurationEditMenuModel> MenuInfo { get; set; }

        public void ReConfigure(WindowUser newUser)
        {
            MenuInfo.Clear();
            MenuInfo.Add(new ConfigurationEditMenuModel("Edit Local Queries", WindowConfigurationScope.Q, false)
            {
                Visible = newUser.CheckPermission(BasicUserPermissions.QueryEditor)
                    ? Visibility.Visible
                    : Visibility.Collapsed
            });
            MenuInfo.Add(new ConfigurationEditMenuModel("Edit Team Queries", WindowConfigurationScope.Q, true)
            {
                Visible = newUser.CheckPermission(BasicUserPermissions.QueryEditor, BasicUserPermissions.TeamSettingEditor)
                    ? Visibility.Visible
                    : Visibility.Collapsed
            });
            EditQuery = newUser.CheckPermission(BasicUserPermissions.QueryEditor | BasicUserPermissions.DatabaseConnectionEditor);

            //EditTeamQuery = newUser.CheckPermission(BasicUserPermissions.QueryEditor | BasicUserPermissions.TeamSettingEditor | BasicUserPermissions.DatabaseConnectionEditor);
            EditConnections = newUser.CheckPermission(BasicUserPermissions.DatabaseConnectionEditor);
            //EditTeamConnections = newUser.CheckPermission(BasicUserPermissions.DatabaseConnectionEditor | BasicUserPermissions.TeamSettingEditor);

            EditContextMenu = EditQuery && newUser.CheckPermission(BasicUserPermissions.ContextMenuEditor);
            //EditTeamContextMenu = EditContextMenu && newUser.CheckPermission(BasicUserPermissions.TeamSettingEditor);
            EditAddons = newUser.CheckPermission(BasicUserPermissions.AddonEditor | BasicUserPermissions.AddonUser);
            //EditTeamAddons = EditAddons && newUser.CheckPermission(BasicUserPermissions.TeamSettingEditor);
            EditAddons.XClone();            
            //reSet list of models...
            invoke(nameof(EditQuery));
            invoke(nameof(EditConnections));
            invoke(nameof(EditContextMenu));
            invoke(nameof(EditAddons));
            Reconfigured?.Invoke(this, EventArgs.Empty);
        }   
        public void Edit(object sender, RoutedEventArgs e)
        {
            MenuItem m = e.OriginalSource as MenuItem;
            if (m == null)
                return;
            ConfigurationEditMenuModel tagModel = m.Tag as ConfigurationEditMenuModel;
            if (tagModel == null || tagModel.MyScope == WindowConfigurationScope.UNK)
                return;
            var cld = new ConfigurationWindows.ConfigurationListDisplay();
            WindowConfigurationLoadModel clm = new WindowConfigurationLoadModel
            {
                UserSpecific = !tagModel.TeamSetting,
                TeamID = tagModel.TeamSetting ? __SESSION__.CurrentUser.TeamID : null
            };
            switch (tagModel.MyScope)
            {
                case WindowConfigurationScope.Q:
                    {                        
                        var q = SessionBroker.LoadQuery(ref clm);
                        q.LoadModel = clm;                        
                        cld.Configure<QueryList, Query>(q);
                        break;
                    }
                case WindowConfigurationScope.A:
                    {
                        var a = SessionBroker.LoadWindowAddons(ref clm);
                        a.LoadModel = clm;
                        cld.Configure<WindowAddonList, WindowAddonConfiguration>(a);
                        break;
                    }
                case WindowConfigurationScope.ACM:
                    {
                        var acm = SessionBroker.LoadContextAddons(ref clm);
                        acm.LoadModel = clm;
                        cld.Configure<ContextAddonList, ContextAddonConfiguration>(acm);
                        break;
                    }
                case WindowConfigurationScope.U:
                    {
                        var ul = SessionBroker.LoadUsers(false, ref clm);
                        ul.LoadModel = clm;
                        cld.Configure<WindowUserCollection, WindowUser>(ul);
                        break;
                    }
                case WindowConfigurationScope.TM:
                    {                        
                        var tm = SessionBroker.LoadTeams(ref clm);
                        tm.LoadModel = clm;
                        cld.Configure<TeamList, Team>(tm);
                        break;
                    }
                case WindowConfigurationScope.CM:
                case WindowConfigurationScope.D:
                case WindowConfigurationScope.SW:
                    {
                        var cm = SessionBroker.LoadContextMenus(ref clm);
                        cm.LoadModel = clm;
                        cld.Configure<ContextMenuList, ContextMenuConfiguration>(cm);
                        break;
                    }
                case WindowConfigurationScope.DB:
                    {
                        var db = SessionBroker.LoadConnections(ref clm);
                        db.LoadModel = clm;
                        cld.Configure<DatabaseList, Database>(db);
                        break;
                    }
            }
            if (cld.ShowDialog() && cld.NeedRefresh)
            {
                SessionBroker.LoadConfigurations(false, __SESSION__.SingleUserMode);                
            }            
            e.Handled = true;
        }
    }
    public class ConfigurationEditMenuModel:INotifyPropertyChanged
    {
        Visibility enabled = Visibility.Collapsed;
        public Visibility Visible
        {
            get { return enabled; }
            set
            {
                enabled = value; invoke(nameof(Visible));
            }
        }
        public string DisplayName { get; private set; }
        public readonly WindowConfigurationScope MyScope;
        public readonly bool TeamSetting;
        public ConfigurationEditMenuModel(string display, WindowConfigurationScope scope, bool ForTeam)
        {
            DisplayName = display;
            MyScope = scope;
            TeamSetting = ForTeam;
        }
        private void invoke(string prop) { PropertyChanged.Invoke(this, new PropertyChangedEventArgs(prop)); }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
