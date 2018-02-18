using System;
using System.Collections.Generic;
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
using SEIDR.Dynamics.Configurations;
using SEIDR.WindowMonitor.SettingWindows;
using SEIDR.Dynamics;
using System.Data;
//using static SEIDR.WindowMonitor.sExceptionManager;
using SEIDR.WindowMonitor.MonitorConfigurationHelpers;
using System.ComponentModel;
using SEIDR.Dynamics.Configurations.UserConfiguration;

namespace SEIDR.WindowMonitor.ConfigurationWindows
{    
    /// <summary>
    /// Interaction logic for iConfigListDisplay.xaml
    /// </summary>
    public partial class ConfigurationListMergeWindow : SessionWindow, INotifyPropertyChanged
    {
        WindowConfigurationScope editType;
        public bool NeedRefresh { get; set; }
        public Visibility UpdateTeam { get; set; } = Visibility.Collapsed;
        iWindowConfigurationList<iWindowConfiguration> localConfig;
        iWindowConfigurationList<iWindowConfiguration> teamConfig;
        static readonly string ID_COLUMN = nameof(iWindowConfiguration.ID);

        public event PropertyChangedEventHandler PropertyChanged;

        static ConfigurationListMergeWindow()
        {
            ID_COLUMN = nameof(iWindowConfiguration.ID).ToUpper();
        }
        public ConfigurationListMergeWindow()
            :base(true)
        {            
            InitializeComponent();
            DataContext = this;        
        }
        
        /// <summary>
        /// Check if the user has permission for Team Editing and open a picker to choose the team if so.
        /// </summary>
        /// <typeparam name="E"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="localConfiguration"></param>
        /// <param name="teamConfiguration"></param>
        public void Configure<E, T>(E localConfiguration, E teamConfiguration) 
            where E: WindowConfigurationList<T>
            where T: iWindowConfiguration
        {
            if (localConfiguration.LoadModel == null )
            {
                Handle("Missing LoadModel on Local Configuration");
                CanShowWindow = false;
                return;
            }
            if(teamConfiguration.LoadModel == null)
            {
                Handle("Missing LoadModel on Team Configuration");
                CanShowWindow = false;
                return;
            }
            if(localConfiguration.ValidationScope != teamConfiguration.ValidationScope)
            {
                Handle($"Mismatching Scopes on  configuration lists: {localConfiguration.ValidationScope.GetDescription()} versus {teamConfiguration.ValidationScope.GetDescription()}.");
                CanShowWindow = false;
                return;
            }
            
            editType = localConfiguration.ValidationScope;
            if (editType == WindowConfigurationScope.UNK)
            {
                Handle("Unsupported Configuration Type");
                CanShowWindow = false;
                return;
            }
            if (!MyCurrentUser.CheckPermission(editType, localConfiguration.LoadModel))
            {
                Handle("You do not have permission to edit this type of Configuration");
                CanShowWindow = false;
                return;
            }            
            Title = editType.GetDescription();


            localConfig = (iWindowConfigurationList<iWindowConfiguration>)localConfiguration;
            teamConfig = (iWindowConfigurationList<iWindowConfiguration>)teamConfiguration;
            MyDisplayData.ItemsSource = localConfig.MyData.DefaultView;

            UpdateTeam = MyCurrentUser.CheckPermission(BasicUserPermissions.TeamSettingEditor) 
                ? Visibility.Visible 
                : Visibility.Collapsed;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdateTeam)));
        }
        /// <summary>
        /// Get the name of the first selected item in the config manager
        /// </summary>
        /// <param name="remove">If true, also remove it from the GUI's datagrid</param>
        /// <returns></returns>
        private int? GetLocalSelectedID(bool remove = false)
        {
            //var dg = sender as DataGrid;
            var dg = MyDisplayData;
            if (dg == null || dg.SelectedIndex < 0)
                return null;
            var dt = ((DataView)dg.ItemsSource).ToTable();
            var id = dt.Rows[dg.SelectedIndex][ID_COLUMN] as int?;
            if (remove && id.HasValue)
            {
                localConfig.Remove(id.Value); //Remove now? Why not. will save at the end. DB call might have already saved..
                RefreshLocal();                
            }
            return id;
        }
        private int? GetTeamSelectedID(bool remove = false)
        {
            var dg = this.Team;
            if (dg == null || dg.SelectedIndex < 0)
                return null;
            var dt = ((DataView)dg.ItemsSource).ToTable();
            var id = dt.Rows[dg.SelectedIndex][ID_COLUMN] as int?;
            if (remove && id.HasValue)
            {
                teamConfig.Remove(id.Value); //Remove now? Why not. will save at the end. DB call might have already saved..
                RefreshLocal();
            }
            return id;
        }
        private void RefreshLocal()
        {            
            var dg = MyDisplayData;
            dg.ItemsSource = null;
            dg.ItemsSource = localConfig.MyData.DefaultView;            
        }
        private void RefreshTeam()
        {
            var dg = Team;
            dg.ItemsSource = null;
            dg.ItemsSource = teamConfig.MyData.DefaultView;
        }        
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NeedRefresh = localConfig.HasAltered;
            if (NeedRefresh)                                        
                localConfig.Save();                            
            if (teamConfig.HasAltered)
                teamConfig.Save();

            Finish();                       
            //iConfig
        }
                
        

        private void teamInsert_Click(object sender, RoutedEventArgs e)
        {
            var id = GetLocalSelectedID();
            var c = localConfig[id].DClone();
            c.ID = null;
            teamConfig.Add(c);
            RefreshTeam();
        }

        private void teamUpdateID_Click(object sender, RoutedEventArgs e)
        {
            var id = GetLocalSelectedID();
            var c = localConfig[id].DClone();
            teamConfig.Update(c);
            RefreshTeam();
        }

        private void teamUpdateKey_Click(object sender, RoutedEventArgs e)
        {
            var id = GetLocalSelectedID();
            var c = localConfig[id].DClone();
            var l = teamConfig[c.Key];
            c.ID = l.ID;
            teamConfig[l.ID] = c;            
            RefreshTeam();
        }

        private void addLocal_Click(object sender, RoutedEventArgs e)
        {
            var id = GetLocalSelectedID();
            var c = teamConfig[id].DClone();
            c.ID = null;
            localConfig.Add(c);
            RefreshLocal();
        }

        private void updateLocalID_Click(object sender, RoutedEventArgs e)
        {
            var id = GetTeamSelectedID();
            var c = teamConfig[id].DClone();            
            localConfig.Update(c);
            RefreshTeam();
        }

        private void updateLocalKey_Click(object sender, RoutedEventArgs e)
        {
            var id = GetTeamSelectedID();
            var c = teamConfig[id].DClone();
            var l = localConfig[c.Key];
            c.ID = l.ID;
            localConfig[l.ID] = c;            
            RefreshTeam();
        }

        private void removeLocal_Click(object sender, RoutedEventArgs e)
        {
           GetLocalSelectedID(true);
        }

        private void removeTeam_Click(object sender, RoutedEventArgs e)
        {
            GetTeamSelectedID(true);
        }
    }
}
