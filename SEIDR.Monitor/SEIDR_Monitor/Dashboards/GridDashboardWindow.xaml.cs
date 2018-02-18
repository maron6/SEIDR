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
using SEIDR.WindowMonitor.Models;
using System.Data.SqlClient;
using System.Data;
//using SEIDR.Processing;
using SEIDR.Dynamics.Configurations;
//using SEIDR.Processing.Data.DBObjects;
using SEIDR.Dynamics.Windows;
using SEIDR.DataBase;
using SEIDR.Dynamics.Configurations.DatabaseConfiguration;
using SEIDR.Dynamics.Configurations.ContextMenuConfiguration;
using SEIDR.Dynamics;
using static SEIDR.WindowMonitor.MonitorConfigurationHelpers.LibraryManagement;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for GridDashboardWindow.xaml
    /// </summary>
    public partial class GridDashboardWindow : SessionWindow
    {
        DatabaseManagerHelperModel model;
        SqlCommand cmd;
        Database db;
        DatabaseManager dbm;
        ContextMenuConfiguration thisDashboard;
        public GridDashboardWindow(DatabaseManagerHelperModel model, DatabaseManager manager, Database connection, ContextMenuConfiguration myConfig)
        {
            InitializeComponent();
            DataContext = this;
            this.model = model;
            cmd = null;
            db = connection;
            dbm = manager;
            thisDashboard = myConfig;

            Title = myConfig.Dashboard;
            //SetupPage(); //Was commented out for implementing paging, but don't consider this especially important
            Setup();

            DashboardData.MouseDoubleClick += ShowDetail;
        }
        public GridDashboardWindow(SqlCommand sourceCommand, Database connection, ContextMenuConfiguration myConfig)
        {
            InitializeComponent();
            DataContext = this;
            //d = new Dashboard(sourceCommand, connection);
            //current = d.RefreshList();
            cmd = sourceCommand;
            model = null;            
            db = connection;
            dbm = null;
            thisDashboard = myConfig;
                        
            Title = myConfig.Dashboard;
            //SetupPage(); //Was commented out for implementing paging, but don't consider this especially important
            Setup();

            DashboardData.MouseDoubleClick += ShowDetail;
        }
        void ShowDetail(object sender, RoutedEventArgs e)
        {
            var row = DashboardData.SelectedItem as DataRowView;
            if (row == null)
                return;
            EditableDashboardDisplay edd = new EditableDashboardDisplay(
                row,
                $"{thisDashboard.Key} Record Detail",
                readOnlyMode: true);
            edd.ShowDialog(true);
            /*var tab = (DashboardData.ItemsSource as DataView).Table;
            if (tab.Columns.Count == 0)
                return;
            string col = tab.Columns[0].ColumnName;
            var r = new Alert(string.Format("Copy {0} to clipboard?", col), Confirmation: true, Choice:true).ShowDialog();
            if (r.HasValue && r.Value)
            {
                var cellText = tab.Rows[DashboardData.SelectedIndex][0].ToString();
                Clipboard.SetText(cellText);
            }*/
            e.Handled = true;
        }
        //DataTable _SourceData;
        //int _CurrentPage = 0;
        ~GridDashboardWindow()
        {
            cmd.Dispose();
        }
        public void Setup()
        {
            colorCol = null;
            DataTable dt;
            if (cmd != null)
                dt = db.Connection.RunCommand(cmd).GetFirstTableOrNull();
            else
                dt = dbm.Execute(model).GetFirstTableOrNull();
            //if (dt.Columns.Contains("LMUID"))
            //    dt.Columns.Remove("LMUID");
            //dt.Columns.Add(MyCurrentUser.GetLMUIDColumn());
            DashboardData.ItemsSource = dt.AsDataView();
        }
        /*
        public void SetupPaging(DataTable dt)
        {

        }
        private void SetPage(int x)
        {
            Cursor = Cursors.Wait;
            DashboardData.ItemsSource = null;
            this.IsEnabled = false;
            try
            {
                _CurrentPage = x;
                PagePicker.Value = x + 1; //Page picking is 1 based, but actual page is zero based
                var d = _SourceData.Rows.Cast<DataRow>();
                if (_CurrentPage == 0)
                    DashboardData.ItemsSource = d.Take(_pageSize).CopyToDataTable().AsDataView();
                else
                    DashboardData.ItemsSource = d.Skip(_CurrentPage * _pageSize).Take(_pageSize).CopyToDataTable().AsDataView();                                                               
            }
            catch (Exception ex)
            {
                //new Alert("Unable to set page to " + x + ": " + ex.Message, Choice: false);
                Handle(ex, "Unable to set page to " + (x + 1), ExceptionLevel.UI_Basic);
                return;
            }
            finally
            {
                this.Cursor = Cursors.Arrow;
                this.IsEnabled = true;
            }
            PageNumber.Content = $"Page # {x + 1}/{_PageCount + 1}";
        }
        */


        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Setup();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        void RefreshClick_Click(object sender, RoutedEventArgs e)
        {
            Setup();
        }
        #region Context menu stuff
        private void SetupContextMenus()
        {
            DashboardData.ContextMenu = null;
            Cursor = Cursors.Wait;            
            ContextMenu w = new ContextMenu();
            MenuItem RefreshClick = new MenuItem()
            {
                Header = "Refresh",
                Name = "_CONTEXT_REFRESH",
                Icon = new Image
                {
                    Source = new BitmapImage(new Uri(@"Icons\Refresh.png", UriKind.Relative)),
                    Height = 20,
                    Width = 20
                }
            };
            RefreshClick.Click += RefreshClick_Click;
            var items = myContextMenus.GetChildren(thisDashboard);// menuItems.GetChildren(q.Name);
            foreach (var item in items)
            {
                //Note: Unlike on the main window, do NOT need to check for permission on context menus, 
                //because this window is only opened by having context menu details

                //if (item.Dashboard != null && !MyCurrentUser.CanOpenDashboard)
                //    continue;
                if (item.AddonID != null && !MyCurrentUser.CanUseAddons)
                    continue;
                try
                {
                    w.Items.Add(AddChildren(item));
                }
                catch (Exception ex)
                {
                    //new Alert(ex.Message + "\nIt may be necessary to go into the raw setting file and fix the Records.", false, false).ShowDialog();
                    Handle(ex, $"Issues Loading Context Menu: '{item.Key}'");
                    continue;
                }
            }
            if(items.HasMinimumCount(1))
                w.Items.Add(new Separator());
            w.Items.Add(RefreshClick);       
            /*
            var m = new MenuItem{Header = "Copy", Name = "_CONTEXT_COPY",
                Icon = new Image
                {
                    Source = new BitmapImage(new Uri(@"Icons\Copy.png", UriKind.Relative)),
                    Height = 20,
                    Width = 20
                }
            };            
            m.Click +=  (sender, e)=> 
            {
                var v = DashboardData.ItemsSource as DataView;
                if(v == null || DashboardData.CurrentCell == null)
                //if (v == null || DashboardData.SelectedIndex < 0 || DashboardData.SelectedCells.Count == 0)
                    return;
                Clipboard.SetText(DashboardData.CurrentCell.ToString());                
            };
            */
            //w.Items.Add(m);
            DashboardData.ContextMenu = w;
            //Use context menuitems to create the ContextMenus            
            this.Cursor = Cursors.Arrow;
        }

        private MenuItem AddChildren(ContextMenuConfiguration qmi)
        {            
            string DisplayName = qmi.Key; 
            MenuItem m = new MenuItem()
            {
                Header = "_" + DisplayName,
                Name = qmi.MyScope.ToString() + "_" +  qmi.ID,
                //Background = (SolidColorBrush)new BrushConverter().ConvertFromString("LightSlateGray")
            };
            if(qmi.AddonID != null)
            {                
                m.Icon = new Image
                {
                    Source = new BitmapImage(new Uri(@"Icons\ContextAddon.png", UriKind.Relative)),
                    Height = 20,
                    Width = 20
                };
            }
            else if (qmi.Dashboard != null && qmi.Dashboard.Trim() != "")
            {
                m.Background = Brushes.Snow;  //(SolidColorBrush)new BrushConverter().ConvertFromString("Snow");
                m.Icon = new Image
                {
                    Source = new BitmapImage(new Uri(@"Icons\ContextDashboard.png", UriKind.Relative)),
                    Height = 20,
                    Width = 20
                };
            }
            else if(qmi.ProcedureCall != null)
            {
                m.Icon = new Image
                {
                    Source = new BitmapImage(new Uri(@"Icons\ContextProc.png", UriKind.Relative)),
                    Height = 20,
                    Width = 20
                };
            }
            m.Click += m_Click;
            var c = myContextMenus.GetChildren(qmi);  // menuItems.GetChildren(qmi.Name);
            if (c != null)
            {
                foreach (var child in c)
                {
                    if (child.IsSwitch)
                        continue; //Switches shouldn't have other context menus as parents
                    //if (child.Dashboard != null && !SettingManager.Me.CanOpenDashboard)
                    //    continue;
                    if (child.AddonID != null && !MyCurrentUser.CanUseAddons)
                        continue;
                    m.Items.Add(AddChildren(child));
                }
            }
            return m;
        }

        public void m_Click(object sender, RoutedEventArgs e)
        {
            MenuItem m = sender as MenuItem;
            if (m == null || DashboardData.SelectedItems.Count == 0)
                return;
            DashboardData.ContextMenu.IsEnabled = false;
            //ContextMenuItem cmi = SettingManager.myContextMenus[m.Name];    // menuItems[m.Name];
            ContextMenuConfiguration cmi = myContextMenus[m.Name.ToString()];
            DataRowView drv = DashboardData.SelectedItems[0] as DataRowView;
            if (cmi.AddonID != null)
            {
                var addon = myContextAddons[cmi.AddonID];
                var plugin = myLibrary.GetApp(addon.AppName, addon.Guid, MyBasicUser);
                if(plugin == null)
                {
                    Handle("Could not find specified plugin!", ExceptionLevel.UI_Basic);
                    DashboardData.ContextMenu.IsEnabled = true;
                    return;
                }
                plugin.Connection = db.Connection;
                plugin.Caller = MyCurrentUser.XClone();
                //plugin.Caller = SettingManager._ME.AsBasicUser();
                //plugin.IDName = cmi.ProcIDParameterName;
                //plugin.IDValue = cmi.ProcID;
                Dictionary<string, object> param = null;
                if (cmi.ParameterInfo != null)
                {
                    param = new Dictionary<string, object>();
                    foreach (var info in cmi.ParameterInfo)
                    {
                        param.Add(info.Key, info.Value);
                    }
                }
                string Message = "Could not load Plugin";
                try {
                    if (cmi.MultiSelect)
                    {                        
                        DataRowView[] si = new DataRowView[DashboardData.SelectedItems.Count];
                        for (int i = 0; i < si.Length; i++)
                        {
                            si[i] = DashboardData.SelectedItems[i] as DataRowView;
                        }                        
                        Message = plugin.Execute(/*DashboardData.SelectedItems as DataRowView[]*/ si, param);                        
                    }   
                    else                 
                        Message = plugin.Execute(drv, param);
                    
                }
                catch (Exception ex)
                {
                    Handle(ex, Message, ExceptionLevel.UI_Basic);
                    return;
                }
                finally
                {
                    DashboardData.ContextMenu.IsEnabled = true;
                }
                if (Message != null)
                    Handle(Message, ExceptionLevel.UI_Basic); //Non error Message.
                var c = DashboardData.ContextMenu;
                DataView dv = (DashboardData.ItemsSource as DataView);
                DashboardData.ItemsSource = null;
                DashboardData.ItemsSource = dv;
                //SetupPaging(dv.Table);
                DashboardData.ContextMenu = c; //Refresh the DataGrid after calling an addon, in case the addon Altered the data source
                return;
            }
            else if (cmi.UseQueue)
            {
                if (cmi.MultiSelect)
                {
                    DataRowView[] batch = (from DataRowView item in DashboardData.SelectedItems
                                           select item).ToArray();
                    ContextActionQueue.BatchQueueAction(cmi, batch, db.ID.Value);
                }
                else
                    ContextActionQueue.QueueAction(cmi, DashboardData.SelectedItem as DataRowView, db.ID.Value);
            }
            else if (cmi.ProcedureCall != null)
            {
                int rc = ContextAction(cmi, drv, db); //Grab at 0, then if multi select, call sproc on all the other rows selected...
                if (cmi.MultiSelect)
                {
                    int limit = myMiscSettings.MultiSelectContextSprocLimit ?? DashboardData.SelectedItems.Count;
                    if (limit < 1 || limit > DashboardData.SelectedItems.Count)
                        limit = DashboardData.SelectedItems.Count;
                    for (int i = 1; i < limit; i++)
                    {
                        drv = DashboardData.SelectedItems[i] as DataRowView;
                        rc += ContextAction(cmi, drv, db);
                    }
                }
                if (rc > 0)
                    Setup();
            }
            DashboardData.ContextMenu.IsEnabled = true;
        }
        private int ContextAction(ContextMenuConfiguration cmi, DataRowView drv, Database db)
        {
            int rc = 0;
            if (string.IsNullOrWhiteSpace(cmi.ProcedureCall) && string.IsNullOrWhiteSpace(cmi.Dashboard))
                return rc;
            using (ContextMenuItemQuery cmiq = new ContextMenuItemQuery(cmi, drv, db))
            {
                rc = cmiq.RunContextAction(drv);                
            }
            return rc;
        }
        #endregion
        private void PerformExport_Click(object sender, RoutedEventArgs e)
        {
            const int PIPE_DELIMITED = 0;
            const int COMMA_DELIMITED = 1;
            const int EXCEL = 2;
            
            switch (this.ExportFormat.SelectedIndex)
            {
                case (PIPE_DELIMITED): 
                    { 
                        string f = FileSaveHelper.GetSaveFile("All (*.*)|*.*|DAT File (*.dat)|*.dat|Text File (*.txt)|*.txt|CSV File (*.csv)|*.csv", ".dat");
                        if (f != null)
                        {
                            DataTable dt = ((DataView)DashboardData.ItemsSource).Table;
                            FileSaveHelper.Convert(f, "|", dt);
                        }
                        break;
                    }
                case COMMA_DELIMITED:
                    {
                        string f = FileSaveHelper.GetSaveFile("All (*.*)|*.*|CSV File (*.csv)|*.csv", ".csv");
                        if (f != null)
                        {
                            DataTable dt = ((DataView)DashboardData.ItemsSource).Table;
                            FileSaveHelper.Convert(f, ",", dt);
                        }
                        break;
                    }
                case EXCEL:
                    {
                        DataTable dt = ((DataView)DashboardData.ItemsSource).Table;
                        string f = FileSaveHelper.GetSaveFile("All (*.*)|*.*|XLS Workbook (*.xls)|*.xls|XLSX Workbook (*.xlsx)|*.xlsx", ".xlsx");
                        FileSaveHelper.WriteExcelFile(f, dt);
                        break;
                    }
            }            
        }
        string colorCol = null; //instead of a boolean, if there's a change to use a different requirement for color. E.g. ends with
        private void DashboardData_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName.ToUpper() == "LMUID" 
                || e.PropertyName.StartsWith("hdn", StringComparison.CurrentCultureIgnoreCase) //Hidden column
                || e.PropertyName.StartsWith("DTL_", StringComparison.CurrentCultureIgnoreCase) //Single detail column
                )
                e.Column.Visibility = Visibility.Collapsed;
            else if(e.PropertyName.ToUpper() == "COLOR")
            {
                e.Column.Visibility = Visibility.Collapsed;
                colorCol = e.PropertyName;
            }
        }

        private void DashboardData_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            DataRowView row = e.Row.Item as DataRowView;
            if (row != null && colorCol != null)
            {
                var color = row[colorCol]?.ToString();
                var brush = (from bColor in typeof(Brushes).GetProperties()
                             where bColor.Name == color
                                && bColor.PropertyType == typeof(SolidColorBrush)
                             select (SolidColorBrush)bColor.GetValue(null)).FirstOrDefault();
                if (brush != null)
                    e.Row.Background = brush;
            }
        }
    }
}
