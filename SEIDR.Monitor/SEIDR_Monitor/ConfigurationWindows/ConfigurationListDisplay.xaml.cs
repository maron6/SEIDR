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

namespace SEIDR.WindowMonitor.ConfigurationWindows
{    
    /// <summary>
    /// Interaction logic for iConfigListDisplay.xaml
    /// </summary>
    public partial class ConfigurationListDisplay : SessionWindow
    {
        WindowConfigurationScope editType;
        public bool NeedRefresh { get; private set; } = false;
        iWindowConfigurationList<iWindowConfiguration> myConfig;
        static readonly string ID_COLUMN = nameof(iWindowConfiguration.ID);
        static ConfigurationListDisplay()
        {
            ID_COLUMN = nameof(iWindowConfiguration.ID).ToUpper();
        }
        public ConfigurationListDisplay()
            :base(true)
        {            
            InitializeComponent();                        
        }
        
        public void Configure<E, T>(E configurationToEdit) 
            where E: WindowConfigurationList<T>
            where T: iWindowConfiguration
        {
            if (configurationToEdit.LoadModel == null)
            {
                Handle("Missing LoadModel on configuration");
                CanShowWindow = false;
                return;
            }
            Clone.Visibility = configurationToEdit.Cloneable ? Visibility.Visible : Visibility.Collapsed;
            editType = configurationToEdit.ValidationScope;

            if (!MyCurrentUser.CheckPermission(editType, configurationToEdit.LoadModel))
            {
                Handle("You do not have permission to edit this type of Configuration");
                CanShowWindow = false;
                return;
            }
            string s = editType.GetDescription();
            Title = s;
            if (editType == WindowConfigurationScope.UNK)
                throw new Exception("Unsupported Configuration type.");

            myConfig = (iWindowConfigurationList<iWindowConfiguration>)configurationToEdit;
            MyDisplayData.ItemsSource = myConfig.MyData.DefaultView;
        }
        /// <summary>
        /// Get the name of the first selected item in the config manager
        /// </summary>
        /// <param name="remove">If true, also remove it from the GUI's datagrid</param>
        /// <returns></returns>
        private int? GetSelectedID(bool remove = false)
        {
            //var dg = sender as DataGrid;
            var dg = MyDisplayData;
            if (dg == null || dg.SelectedIndex < 0)
                return null;
            var dt = ((DataView)dg.ItemsSource).ToTable();
            var id = dt.Rows[dg.SelectedIndex][ID_COLUMN] as int?;
            if (remove && id.HasValue)
            {
                myConfig.Remove(id.Value); //Remove now? Why not. will save at the end. DB call might have already saved..
                Refresh();
                //dt.Rows.RemoveAt(dg.SelectedIndex);
                ////dg.Items.RemoveAt(dg.SelectedIndex);
                //dg.ItemsSource = null;
                //dg.ItemsSource = dt.AsDataView();
            }
            return id;
        }
        private void Refresh()
        {
            var dg = MyDisplayData;
            dg.ItemsSource = null;
            dg.ItemsSource = myConfig.MyData.DefaultView;
        }
        private DataRow GetSelectedRow()
        {
            var dg = MyDisplayData;
            if (dg == null || dg.SelectedIndex < 0)
                return null;
            DataRowView drv = dg.SelectedItem as DataRowView;
            return drv.Row;
        }

        private void OpenRecordEditor(object sender, bool TryEdit = true)
        {            
            int? x = GetSelectedID(); //sender);
            iWindowConfiguration val = myConfig[x]; //null getter should be instant null
            if (TryEdit && val == null)
                return;            
            switch (editType)//Scope of the list, not the item.
            {
                case WindowConfigurationScope.A:
                    {
                        /*
                        var display = new display(val);
                        if(display.ShowDialog() ?? false)
                        {
                            if(TryEdit)
                                myConfig.Update(val);
                            else
                                myConfig.Add(val);
                        }
                        */
                        var u = MyBroker.AddEditWindowAddon(val as Dynamics.Configurations.AddonConfiguration.WindowAddonConfiguration);
                        if (u != null)
                        {
                            if (TryEdit)
                                myConfig.Update(u);
                            else
                                myConfig.Add(u);                            
                        }
                        break;
                    }
                case WindowConfigurationScope.Q:
                    {
                        var u = MyBroker.AddEditQuery(val as Dynamics.Configurations.QueryConfiguration.Query);
                        //if (u != null)
                        //    myConfig[val.ID] = u;
                        if (u != null)
                        {
                            if (TryEdit)
                                myConfig.Update(u);
                            else
                                myConfig.Add(u);
                        }
                        break;
                    }
                case WindowConfigurationScope.ACM: //ACM .... similar to context menu but separate now? Merge?
                    {
                        var u = MyBroker.AddEditContextAddon(val as Dynamics.Configurations.AddonConfiguration.ContextAddonConfiguration);
                        if (u != null)
                        {
                            if (TryEdit)
                                myConfig.Update(u);
                            else
                                myConfig.Add(u);
                        }
                        break;
                    }
                case WindowConfigurationScope.CM: //These three should be all be CM
                case WindowConfigurationScope.D:
                case WindowConfigurationScope.SW:
                    {
                        var u = MyBroker.AddEditContextMenu(val as Dynamics.Configurations.ContextMenuConfiguration.ContextMenuConfiguration);
                        if (u != null)
                        {
                            if (TryEdit)
                                myConfig.Update(u);
                            else
                                myConfig.Add(u);
                        }
                        break;
                    }
                case WindowConfigurationScope.DB:
                    {
                        var u = MyBroker.AddEditDatabaseConfig(val as Dynamics.Configurations.DatabaseConfiguration.Database);
                        if (u != null)
                        {
                            if (TryEdit)
                                myConfig.Update(u);
                            else
                                myConfig.Add(u);
                        }
                        break;
                    }
                case WindowConfigurationScope.U:
                    {
                        var u = MyBroker.AddEditUser(val as Dynamics.Configurations.UserConfiguration.WindowUser);
                        if (u != null)
                        {
                            if (TryEdit)
                                myConfig.Update(u);
                            else
                                myConfig.Add(u);
                        }
                        break;
                    }
                case WindowConfigurationScope.TM:
                    {
                        var u = MyBroker.AddEditTeam(val as Dynamics.Configurations.UserConfiguration.Team);
                        if (u != null)
                        {
                            if (TryEdit)
                                myConfig.Update(u);
                            else
                                myConfig.Add(u);
                        }
                        break;                        
                    }
                default:
                    {
                        Handle("Unhandled configuration type editor!", ExceptionLevel.UI_Basic);
                        return;
                    }                    
            }
            MyDisplayData.ItemsSource = null;
            MyDisplayData.ItemsSource = myConfig.MyData.DefaultView;
        }
        private void MyData_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                OpenRecordEditor(sender, true);
            }
            catch(Exception ex)
            {
                Handle(ex, "Error editing record", ExceptionLevel.UI);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NeedRefresh = myConfig.HasAltered;
            if (NeedRefresh)
                myConfig.Save();
            DialogResult = true;
            Close();            
            //iConfig
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            OpenRecordEditor(sender);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            GetSelectedID(remove:true); //sender);            
        }        
        private void Create_Click(object sender, RoutedEventArgs e)
        {
            OpenRecordEditor(sender, false);
            //MyDisplayData.ItemsSource = myConfig.MyData.DefaultView; //Reset in the 
        }

        private void Clone_Click(object sender, RoutedEventArgs e)
        {
            DataRow source = GetSelectedRow();
            if (source == null)
                return;
            var id = GetSelectedID();
            string name = source[nameof(iWindowConfiguration.Key)].ToString();
            var tc = myConfig[id];
            //if(tc.MyScope == WindowConfigurationScope.SW)            
            //{
            //    Handle("Cannot clone Switches.", ExceptionLevel.UI_Basic);
            //    return; //Clone target query to a different queryID
            //}
            ConfigurationClone cc = new ConfigurationClone(tc);            
            //CloneNameConfirm cnc = new CloneNameConfirm(name);
            var r = cc.ShowDialog();
            if(r)
            {
                myConfig.Add(cc.cloned);                                
                MyDisplayData.ItemsSource = null;
                MyDisplayData.ItemsSource = myConfig.MyData.DefaultView;
            }
        }
        private void MyDisplayData_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string col = e.PropertyName.ToUpper();
            if (col == ID_COLUMN)
                e.Column.Visibility = Visibility.Collapsed;            
        }
    }
}
