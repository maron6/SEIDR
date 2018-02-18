using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static SEIDR.WindowMonitor.SettingManager;
using Ryan_UtilityCode.Dynamics.Configurations;
using MahApps.Metro.Controls;

namespace SEIDR.WindowMonitor.SettingWindows
{

    /// <summary>
    /// Interaction logic for UserEditor.xaml
    /// </summary>
    public partial class UserEditor : SessionWindow //Inherit sessionwindow for knowing if active, but use the actual user(Me)
    {
        
        string[] invisiblePermissions;
        public User item { get; set; }
        public UserEditor(User toEdit)
            :base(true, UserAccessMode.Team)
        {
            if (!MyCurrentUser.Admin)
            {
                CanShowWindow = false;
                Handle("You are not logged in as an Admin - cannot edit users", 
                    SEIDR.Dynamics.ExceptionLevel.UI_Basic);
                return;
            }
            if (toEdit == null)
                item = User.DefaultUser;
            else
                item = SEIDR.Dynamics.Extensions.XClone(toEdit);

            _AdminBool = item.IsAdmin;            
            InitializeComponent();
            UseSessionColor = false;
            //if (_ME.AdminLevel > 1)
            if(!_ME.SuperAdmin)
                AdminLevelPicker.Visibility = Visibility.Collapsed;
            else
            {
                TeamChooser.IsReadOnly = false;
                TeamChooser.IsEditable = true;//High level admins allowed to edit teams
            }
            if (_ME.CanUseAddons /*
                && (_ME.SuperAdmin 
                    || item.AdminLevel == null 
                    || item.AdminLevel > _ME.AdminLevel + 2)*/
                    && _ME.AddonsAdmin
                )
            {
                invisiblePermissions = item.GetInvisiblePermissions(_ME); // Me);
                permissionList.ItemsSource = _ME.GetPermissions(); //Needs to also grab the addons available to the editor
                Permissions.ItemsSource = new ObservableCollection<string>(item.GetVisiblePermissions(_ME)); // Me));
            }
            else
            {
                AddonPermissionList.Visibility = Visibility.Collapsed;
            }
            AdminLevelPicker.Value = item.AdminLevel;
            AdminLevelPicker.Minimum = (int)_ME.AdminLevel + 1;
            AdminLevelPicker.Maximum = ushort.MaxValue;
            string defTeam = _ME.MyTeam?? User.DEFAULT_TEAM_GUI_PLACEHOLDER;
            TeamChooser.ItemsSource = GetTeams(true);
            TeamChooser.SelectedIndex = TeamChooser.Items.IndexOf(defTeam);

            Domains.ItemsSource = User.GetDomains();
            Domains.SelectedIndex = Domains.Items.IndexOf(item.Domain);
            if (Domains.Items.Count <= 1) //Includes default domain already.
                Domains.Visibility = Visibility.Collapsed; //use default domain in this case, and don't need to show
            AdminCheck.IsChecked = item.IsAdmin;
            DataContext = this;
            SetBindings();
            if (toEdit != null)
                NameText.IsEnabled = false;
        }
        #region binding
        private void SetVisibility()
        {            
            Action<CheckBox, bool> setVis = (o, v) => { o.Visibility = v ? Visibility.Visible : Visibility.Collapsed; };
            setVis(editContextAddonCheck, _ME.CanEditContextAddons);
            setVis(edit_Addon_check, _ME.CanEditAddons);
            //setVis(LoaderControlCheck, Me.LoaderControl);
            //setVis(ETLTeamCheck, Me.ETLTeam);
            setVis(edit_conn_check, _ME.CanEditConnections);
            setVis(edit_context_check, _ME.CanEditContextMenus);
            //setVis(Run_query_check, __ME.CanRunQueries);
            setVis(edit_query_Check, _ME.CanEditQueries);
            //setVis(context_Open_Check, Me.CanOpenContextMenus);
            //setVis(Open_DashBoard_check, Me.CanOpenDashboard);
            setVis(Use_Addons_Check, _ME.CanUseAddons);
            setVis(CanDataExport, _ME.CanExportData);
            setVis(Use_SessionCache_Check, _ME.CanUseCache);
        }
        private void SetBindings()
        {
            //Originally Had some issues with Some bindings working but not others for some reason, decided to just set them all in the C#            
            var d = GetBindings();
            Action<DependencyObject, string> set = (o, v) => { BindingOperations.SetBinding(o, CheckBox.IsCheckedProperty, d[v]); };            
            set(editContextAddonCheck, "CanEditContextAddons");
            set(edit_Addon_check, "CanEditAddons");
            //set(ETLTeamCheck, "ETLTeam");
            set(edit_conn_check, "CanEditConnections");
            set(edit_context_check, "CanEditContextMenus");
            set(edit_query_Check, "CanEditQueries");
            set(Run_query_check, "CanRunQueries");
            //set(context_Open_Check, "CanOpenContextMenus");
            //set(Open_DashBoard_check, "CanOpenDashboard");
            set(Use_Addons_Check, "CanUseAddons");
            set(CanDataExport, "CanExportData");
            set(Use_SessionCache_Check, "CanUseCache");
            
        }
        private Dictionary<string, Binding> GetBindings()
        {
            Dictionary<string, Binding> ret = new Dictionary<string, Binding>();
            var props = typeof(User).GetProperties();
            foreach(var prop in props)
            {
                ret.Add(prop.Name, new Binding
                {
                    Path = new PropertyPath("item." + prop.Name),
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Mode = BindingMode.TwoWay
                });
            }
            return ret;
        }
        #endregion
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            string newTeam = TeamChooser.Text;
            if (newTeam == User.DEFAULT_TEAM_GUI_PLACEHOLDER)
                newTeam = null;
            if (_ME.CanUseAddons &&  _ME.AddonsAdmin)
            {
                string[] x = new string[Permissions.Items.Count];
                for (int i = 0; i < Permissions.Items.Count; i++)
                {
                    x[i] = Permissions.Items[i] as string;
                }
                List<string> temp = new List<string>(x);
                temp.AddRange(invisiblePermissions); //Make sure to keep invisible permissions
                x = (from t in temp
                     select t
                    ).Distinct().ToArray();
                item.SetPermissions(x); //If not set here, will be the permissions from original cloning
            }
            item.MyTeam = newTeam;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        bool _AdminBool;
        public bool AdminBool
        {
            get { return _AdminBool; }
            set
            {
                if (_AdminBool == value)
                    return;
                //item.IsAdmin = value;
                _AdminBool = value;
                if (value)
                    item.AdminLevel = (ushort)(1 + (MyCurrentUser.AdminLevel.Value));
                else
                    item.AdminLevel = null;
                AdminLevelPicker.Value = item.AdminLevel;
            }
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            item.IsAdmin = AdminCheck.IsChecked ?? false;
            if (AdminCheck.IsChecked ?? false)
            {
                item.AdminLevel = _ME.AdminLevel ?? 0 + 1;
                AdminLevelPicker.Value = item.AdminLevel;
            }
        }

        private void Domains_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            item.Domain = Domains.SelectedValue.ToString();
            if (string.IsNullOrWhiteSpace(item.Name))
                return;
            var t = item.Name;
            item.Name = item.Domain + "\\" + t.Substring(t.IndexOf("\\" + 1)); //Set name to Domain + UserName            
        }
        
        private void NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            item.AdminLevel = (ushort?)AdminLevelPicker.Value;
            _AdminBool = e.NewValue.HasValue;
        }

        private void removePermission_Click(object sender, RoutedEventArgs e)
        {            
            (Permissions.ItemsSource as ObservableCollection<string>).Remove(Permissions.SelectedItem as string);            
        }

        private void AddPermission_Click(object sender, RoutedEventArgs e)
        {
            string x = permissionList.SelectedValue as string;
            if (x == null)
                return;
            var temp = (Permissions.ItemsSource as ObservableCollection<string>);
            if (temp.Contains(x))
                return;
            temp.Add(x);
        }
    }
}
