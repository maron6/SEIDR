using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Data;
using System.Data.SqlClient;
using SEIDR.Dynamics.Configurations;
using System.Windows.Threading;
using System.ServiceProcess;
using SEIDR.Dynamics;
//using DBObject = SEIDR.Processing.Data.DBObjects;
using SEIDR.DataBase;
using MahApps.Metro.Controls;
//using static SEIDR.WindowMonitor.SettingManager;
using static SEIDR.WindowMonitor.MonitorConfigurationHelpers.LibraryManagement;
using SEIDR.Dynamics.Configurations.UserConfiguration;
//using static SEIDR.WindowMonitor.sExceptionManager;
using System.Windows.Media.Imaging;
using SEIDR.Dynamics.Windows;
using System.ComponentModel.Composition;
using System.Linq;
using SEIDR.Dynamics.Configurations.QueryConfiguration;
using SEIDR.Dynamics.Configurations.DatabaseConfiguration;
using SEIDR.Dynamics.Configurations.ContextMenuConfiguration;
using SEIDR.Dynamics.Configurations.AddonConfiguration;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [Export(typeof(SEIDR_Window))]
    public partial class MainWindow : SessionWindow, SEIDR_Window
    {         
        //Queries QueryList;
        //DBConnections Connections;
        [Obsolete("Should be replaced by building as the data source changes",true)]
        Dictionary<string, ContextMenu> menus;
        //ContextMenuItems menuItems;
        private void ReadSettings()
        {
            if (myMiscSettings == null)
            {
                try
                {
                    MySession.MySettings = MiscSetting.LoadFromFile();
                }
                catch
                {
                    MySession.MySettings = new MiscSetting()
                    {
                        DefaultQueryTimeout = 160,
                        FileRefresh = 10
                    };
                    Handle("Creating new MiscSettings from basic settings", ExceptionLevel.Background);
                }
            }               
        }

        public ConfigurationViewModels.EditorMenuViewModel EditorModel { get; set; }
        public ConfigurationViewModels.QueryMenuViewModel QueryModelRoot { get; set; }
        //public ConfigurationWindows.AdminMenuViewModel AdminModel { get; set; }

        private void SetupAddonMenu()
        {            
            if(myWindowAddons != null)
            {                
                myWindowAddons.GetMenu(PluginMain, this);
            }
            else
            {
                PluginMain.Items.Clear(); //No addons. Just clear
            }
        }
        
        #region Context Menu Setup
        private void SetupQueryMenu()
        {
            QueryMenu.Items.Clear();
            //if(QueryList == null)
            //    SetupVariables();
            //InitLoad(); //Doesn't do anything, but makes sure that static constructor has been run
            //MyCurrentUser.Name = "TEST";
            /*
            if (!MyCurrentUser.CanRunQueries)
            {
                return;
            }*/
            IEnumerable<string> Categories = myQueries.GetCategories();
            Dictionary<string, MenuItem> setup = new Dictionary<string,MenuItem>();
            
            foreach (string cat in Categories)
            {
                if (cat == null)
                    continue;
                MenuItem catItem = new MenuItem()
                {
                    Name = "Q_C_" + cat,
                    Header = cat
                };
                setup.Add(catItem.Name, catItem);
                IEnumerable<string> subCategories = myQueries.GetSubCategories(cat);
                foreach (string subCat in subCategories)
                {
                    MenuItem subCatItem = new MenuItem()
                    {
                        Name = "Q_C_"+ cat + "_S_" + subCat,
                        Header = subCat
                    };
                    catItem.Items.Add(subCatItem);
                    setup.Add(subCatItem.Name, subCatItem);
                }
                if (subCategories.HasMinimumCount(1))
                    catItem.Items.Add(new Separator());
                QueryMenu.Items.Add(catItem);
                
            }            
            bool NeedSeparator = false;
            if (setup.Count > 0)            
                QueryMenu.Items.Add(new Separator());
            
            //var config = RegisterQueriesConfig.GetConfig();
            //QueryList = ConfigFolder.DeSerializeFile<Queries>(ConfigFolder.GetPath(MyAppName, "Queries.xml"));
            //Connections = ConfigFolder.DeSerializeFile<DBConnections>(ConfigFolder.GetPath(MyAppName, "DBConnections.xml"));
            //QueryList = MyData.Default.Queries.Queries;
            //var DBList = MyData.Default.Connections.DBConnections;
            //var DBconfig = RegisterConnectionsConfig.GetConfig();
            //QueryList = config.Queries;
            foreach (Query item in myQueries.ConfigurationEntries) //QueryList)
            {
                //if (string.IsNullOrWhiteSpace(item.DBConnectionName))
                //    continue;
                if (item.DBConnection == null || item.ID == null)
                    continue;
                Database db = myConnections[item.DBConnection];
                if (db == null)
                    continue;
                string color = db.ConnectionColor.nTrim(true);
                string textColor = db.TextColor.nTrim(true);           
                //string color = myConnections.GetColor(item.DBConnectionName);
                //string textColor = myConnections.GetTextColor(item.DBConnectionName);
                //string DisplayName = item.MyName;
                //if(DisplayName.ToUpper().StartsWith("Q_"))
                //    DisplayName = DisplayName.Substring(2);
                string DisplayName = item.Key;

                MenuItem nItem = new MenuItem()
                {
                    Header = "_" + DisplayName,
                    Name = item.ID.ToString(),
                    ToolTip = "CONNECTION: " + db.Key /*item.DBConnectionName*/,                    
                    Icon = new Image
                    {
                        Source = new BitmapImage(new Uri(@"Icons\Query.png", UriKind.Relative)),
                        Height= 20,
                        Width= 20
                    }
                };
                if(color != null)
                {
                    nItem.Background = (Brush)new BrushConverter().ConvertFromString(color);
                }
                if(textColor != null)
                {
                    nItem.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(textColor);
                }
                var x = new Image
                {
                    Source = new BitmapImage(new Uri(@"Icons\Query.png", UriKind.Relative)),
                    Height = 20,
                    Width = 20
                };
                //nItem.Vka
                bool added = false;
                if (item.SubCategory != null && item.Category != null)
                {
                    string N = "Q_C_" + item.Category + "_S_" + item.SubCategory;
                    if (setup.ContainsKey(N))
                    {
                        setup[N].Items.Add(nItem);
                        added = true;
                    }                    
                }
                if (!added && item.Category != null)
                {
                    string N = "Q_C_" + item.Category;
                    if (setup.ContainsKey(N))
                    {
                        setup[N].Items.Add(nItem);
                        added = true;
                    }
                }
                if (!added)
                {
                    QueryMenu.Items.Add(nItem); //item.Name);
                    NeedSeparator = true;
                }
            }
            if(NeedSeparator)
                QueryMenu.Items.Add(new Separator());
            QueryMenu.Items.Add(new MenuItem()
                { Header = "_Refresh",
                Name = "Refresh",
                ToolTip = "Refresh the current Query"
                ,Icon = new Image { Source = new BitmapImage(new Uri(@"Icons\Refresh.png", UriKind.Relative)), Height=20, Width=20}
            });
            
            /*foreach (var db in Connections) // DBconfig.DBConnections)
            {
                var dbItem = db.InternalDBConn;
                DBData.DatabaseConnection nDB = new DBData.DatabaseConnection(dbItem.Server, dbItem.DefaultCatalog);
                //DBData.DatabaseConnectionManager.AddConnections(dbItem.Name, nDB, true);
            } 
             */  //Unnecessary now that there's a DBConnections variable
        }
        
        /*enuItem openProfileContext = null;*/
        private ContextMenu SetupMenuForQuery(Query q)
        {
            if (q == null)
                return null;
            ContextMenu w = new ContextMenu();
            MenuItem RefreshClick = new MenuItem()
            {
                Header = "Refresh",
                Name = q.ID + "_CONTEXT_REFRESH",
                Icon = new Image
                {
                    Source = new BitmapImage(new Uri(@"Icons\Refresh.png", UriKind.Relative)),
                    Height = 20,
                    Width = 20
                }
            };
            MenuItem Switch = new MenuItem
            {
                Header = "_Switches"
            };

            RefreshClick.Click += RefreshClick_Click;
            var items = myContextMenus.GetChildren(q);// menuItems.GetChildren(q.Name);
            foreach (var item in items)
            {
                //If we have query editor but not context editor, context menus aren't going to link up correctly. 
                //Don't show them at all.
                if (!MyCurrentUser.CheckPermission(BasicUserPermissions.ContextMenuEditor)
                    && MyCurrentUser.CheckPermission(BasicUserPermissions.QueryEditor))
                    break; 

                if (/*item.Dashboard != null && !MyCurrentUser.CanOpenDashboard ||*/
                    item.AddonID != null && !MyCurrentUser.CanUseAddons)
                    continue;
                var m = AddChildren(item, true);
                if (m == null)
                    continue; //Check null, allow validation.
                if (item.IsSwitch)
                    Switch.Items.Add(item);
                else
                    w.Items.Add(m);
            }
            if (items.HasMinimumCount(1))
                w.Items.Add(new Separator());
            if (Switch.Items.Count > 0)
                w.Items.Add(Switch);
            w.Items.Add(RefreshClick);
            var db = myConnections[q.DBConnection];
            if (q.EnableAggregateChart || q.EnableFrequencyChart || q.EnablePieChart)
            {
                w.Items.Add(new Separator());
                if (q.EnablePieChart)
                {                    
                    MenuItem pieChart = new MenuItem
                    {
                        Header = "Open Record as PieChart (int and smallint columns only)",
                        Name = q.ID + "_PIE_CHART"
                    };

                    pieChart.Click += (sender, e) =>
                    {
                        //var v = MonitorData.ItemsSource as DataView;
                        Dashboards.PieChartDisplay pcd = new Dashboards.PieChartDisplay(q.ProcedureCall, MonitorData.SelectedItem as DataRowView);
                        pcd.ShowDialog();
                    };
                    //w.Items.Add(pieChart);


                    MenuItem aggBarChart = new MenuItem
                    {
                        Header = "Open Record as a BarChart (int and smallint columns only)",
                        Name = q.ID + "_PREAGG_BAR_CHART"
                    };
                    aggBarChart.Click += (sender, e) =>
                    {
                        var v = MonitorData.ItemsSource as DataView;
                        var pcd = new Dashboards.AggBarChartDisplay(q.ProcedureCall, MonitorData.SelectedItem as DataRowView);                        
                        pcd.ShowDialog();
                    };
                    MenuItem preAgg = new MenuItem { Header = "Pre Aggregated Charts" };
                    preAgg.Items.Add(pieChart);
                    preAgg.Items.Add(aggBarChart);
                    w.Items.Add(preAgg);
                }
                if (q.EnableAggregateChart || q.EnableFrequencyChart)
                {
                    MenuItem aggCharts = new MenuItem { Header = "Aggregate Bar Charts" };
                    foreach (string group in q.GroupedResultColumns)
                    {
                        MenuItem aggChartSub = new MenuItem { Header = Dashboards.ChartHelper.GetFriendlyName(group) };
                        if (q.EnableAggregateChart)
                        {
                            //create bar chart
                            MenuItem barChart = new MenuItem
                            {
                                Header = "Aggregate Non-Excluded",
                                ToolTip = "Open Records as a bar chart (int and smallint columns only)",
                                Name = EditableObjectHelper.GET_WPF_NAME(q.ID + "_" + group) + "_BAR_CHART_STACKED"
                            };//Need to change to be set up for grouping, with a menu item for each grouping
                            barChart.Click += (sender, e) =>
                            {
                                //if (MonitorData.CurrentColumn == null)
                                //    return;
                                DataRowView[] si = new DataRowView[MonitorData.SelectedItems.Count];
                                for (int i = 0; i < si.Length; i++)
                                {
                                    si[i] = MonitorData.SelectedItems[i] as DataRowView;
                                }
                                Dashboards.StackedBarChartDisplay sb = new Dashboards.StackedBarChartDisplay(
                                    q.ProcedureCall,
                                    $"{q.Key} ({db.Key})",
                                    Dashboards.ChartHelper.GetAggregateData(si, group, q.ExcludedResultColumns)
                                    //MonitorData.CurrentColumn.Header.ToString(),
                                    //MonitorData.SelectedItems as DataRowView[]
                                    );
                                sb.ShowDialog(); //This one might not work if using row select?...
                            };
                            aggChartSub.Items.Add(barChart);
                        }
                        if (q.EnableFrequencyChart)
                        {
                            
                            var sgroup = from g in q.GroupedResultColumns
                                         where g != @group
                                         select g;
                            foreach (string sgroupCol in sgroup)
                            {
                                string subTitle = $"{q.Key} ({db.Key}) - Chart Frequency of '{sgroup}' values associated with '{EditableObjectHelper.FriendifyLabel(group)}'";
                                //create doughnut chart aggregate... group by  main category, then ....? 
                                //Not sure what was planned... probably ignore, group with pie chart maybe.
                                MenuItem freqChart = new MenuItem
                                {
                                    Name = EditableObjectHelper.GET_WPF_NAME(q.ID + "_" + group + "_" + sgroupCol + "_FREQ_CHART")
                                    ,
                                    ToolTip = $"Count frequency of different '{sgroupCol}' values"
                                    ,
                                    Header = "Frequency: " + EditableObjectHelper.FriendifyLabel(sgroupCol)
                                };
                                freqChart.Click += (sender, args) =>
                                {
                                    DataRowView[] si = new DataRowView[MonitorData.SelectedItems.Count];
                                    for(int i = 0; i < si.Length; i++)
                                    {
                                        si[i] = MonitorData.SelectedItems[i] as DataRowView;
                                    }
                                    Dashboards.FrequencyDonutChartDisplay fdcd = new Dashboards.FrequencyDonutChartDisplay(
                                        q.ProcedureCall, subTitle,
                                        Dashboards.ChartHelper.GetAggregateData(
                                            si,
                                            group,
                                            q.ExcludedResultColumns,
                                            sgroupCol
                                        ));
                                    fdcd.ShowDialog();
                                };
                                aggChartSub.Items.Add(freqChart);
                            }
                        }
                        aggCharts.Items.Add(aggChartSub); //Group column name - Aggregate as stacked, frequency chart. Whichever is enabled or both
                    }
                    w.Items.Add(aggCharts); //Agg category
                }//charts
            }
            return w;
        }
        /*
        [Obsolete("Should call SetupMenuForQuery directly for the query when running from a non refresh"
#if !DEBUG
, true
#endif
            )]
        private void SetupContextMenus()
        {
            MonitorData.ContextMenu = null;            
            menus.Clear();
            //if (!MyCurrentUser.CanOpenContextMenus)
            //    return;
            Cursor = Cursors.Wait;
            foreach (var q in myQueries) //QueryList)
            {                
                menus.Add(q.Name, SetupMenuForQuery(q)); 
                //SetupMenuForQueryLogic was originally in this loop - added to eventually method to be called separately
            }
            //Use context menuitems to create the ContextMenus
            if(_currentQuery != null)
                MonitorData.ContextMenu = menus[_currentQuery.Name];
            Cursor = Cursors.Arrow;
        }
        */
        

        void RefreshClick_Click(object sender, RoutedEventArgs e)
        {
            Refresh(true);
            e.Handled = true; //Prevent bubbling and running extra times
        }
        /// <summary>
        /// Set CheckAddon to false when setting up the context menu after a Plugin Updates the data grid
        /// </summary>
        /// <param name="qmi"></param>
        /// <param name="checkAddon"></param>
        /// <returns></returns>
        private MenuItem AddChildren(ContextMenuConfiguration qmi, bool checkAddon)
        {
            string DisplayName = qmi.Key;
            if (DisplayName == null)
                return null; //shouldn't happen, unless there's a broken switch Target.
            //if(DisplayName.ToUpper().StartsWith("CM_"))
            //    DisplayName = DisplayName.Substring(3);
            MenuItem m = new MenuItem()
            {
                Header = "_" + DisplayName,
                Name = qmi.ID.ToString(),
                //Background = (SolidColorBrush)new BrushConverter().ConvertFromString("LightSlateGray")
            };
            if (qmi.AddonID != null)
            {
                m.Icon = new Image
                {
                    Source = new BitmapImage(new Uri(@"Icons\ContextAddon.png", UriKind.Relative)),
                    Height= 20,
                    Width=20
                };
            }
            else if (qmi.Dashboard != null)
            {
                m.Background = Brushes.Snow;
                m.ToolTip = "Opens Dashboard - " + qmi.Dashboard;
                m.Icon = new Image
                {
                    Source = new BitmapImage(new Uri(@"Icons\ContextDashboard.png", UriKind.Relative)),
                    Height = 20,
                    Width = 20
                };
            }
            else if (qmi.IsSwitch)
            {
                var q = myQueries[qmi.ParentID];
                if (q == null)
                    return null; //Broken target, don't add the menu item.                
                var db = myConnections[q.DBConnection];
                BrushConverter bc = new BrushConverter();
                m.Background = (SolidColorBrush) bc.ConvertFromString( db.ConnectionColor ?? "LightSteelBlue");
                m.Foreground = (SolidColorBrush) bc.ConvertFromString(db.TextColor ?? "Black");
                m.Icon = new Image
                {
                    Source = new BitmapImage(new Uri(@"Icons\Switch.png", UriKind.Relative)),
                    Height = 30,
                    Width = 30
                };
            }
            else if (qmi.ProcedureCall != null)
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
                    /*
                    if (child.Dashboard != null && !MyCurrentUser.CanOpenDashboard) //Don't add dashboards if user can't open them
                        continue;
                        */
                    if (checkAddon && child.AddonID != null && !MyCurrentUser.CanUseAddons)
                        continue; //Don't add plugins if user can't open them.
                    m.Items.Add(AddChildren(child, checkAddon));
                }
            }
            return m;
        }
        
        public void m_Click(object sender, RoutedEventArgs e)
        {            
            MenuItem m = sender as MenuItem;
            if(m== null || MonitorData.SelectedItems.Count == 0)
                return;
            DataRowView drv = MonitorData.SelectedItems[0] as DataRowView;
            if (drv == null)
                return;//if first record is null...shouldn't happen
            Cursor = Cursors.Wait;


            MonitorData.ContextMenu.IsEnabled = false;
            
            var cmi = myContextMenus[int.Parse(m.Name)];    // menuItems[m.Name];
            
            Database currentDB;
            if (_currentQuery != null)
                currentDB = myConnections[_currentQuery.DBConnection];    // Connections[currentQuery.DBConnectionName];
            else
                currentDB = myConnections[_pluginConfig.DatabaseID];
            if(currentDB == null)
            {
                Handle("Could not find Database Connection!", ExceptionLevel.UI_Basic);
            }            

            if (cmi.AddonID != null)
            {
                var Addon = myContextAddons[cmi.AddonID];
                string Message = "Unable to Get Add On";
                try
                {
                    SEIDR_WindowAddOn a = myLibrary.GetApp(cmi.Key, Addon.Guid, MyCurrentUser);
                    if (a == null)
                    {
                        Handle("Could not find specified plugin!", ExceptionLevel.UI_Basic);
                        MonitorData.ContextMenu.IsEnabled = true;
                        return;
                    }
                    Message = "Unable to Set Addon Parameters";
// TODO: Update this have a dictionary to pass, like for window level plugins.
                    a.Caller = MyBasicUser;
                    a.Connection = currentDB.Connection;
                    //a.IDName = cmi.ProcIDParameterName;
                    //a.IDValue = cmi.ProcID;
                    Dictionary<string, object> paramInfo = null;
                    if(cmi.ParameterInfo != null)
                    {
                        paramInfo = new Dictionary<string, object>();
                        foreach(var info in cmi.ParameterInfo)
                        {
                            paramInfo.Add(info.Key, info.Value);
                        }
                    }
                    Message = "Unable to execute addon";
                    try
                    {
                        if (cmi.MultiSelect)
                        {
                            DataRowView[] si = new DataRowView[MonitorData.SelectedItems.Count];
                            for (int i = 0; i < si.Length; i++)
                            {
                                si[i] = MonitorData.SelectedItems[i] as DataRowView;
                            }                            
                            Message = a.Execute(/*MonitorData.SelectedItems as DataRowView[]*/ si, paramInfo);                            
                        }
                        else                        
                            Message = a.Execute(drv, paramInfo);
                         
                    }
                    catch(Exception ex)
                    {
                        Handle(ex, Message, ExceptionLevel.UI_Basic);
                        MonitorData.ContextMenu.IsEnabled = true;
                        Cursor = Cursors.Arrow;
                        return;
                    }                    
                    if(Message != null)
                        Handle(Message, ExceptionLevel.UI_Basic);

                    var c = MonitorData.ContextMenu;                    
                    DataView dv = (MonitorData.ItemsSource as DataView);
                    MonitorData.ItemsSource = null;
                    SetupPaging(dv.Table);
                    MonitorData.ContextMenu = c;       //Refresh the DataGrid after calling an addon, in case the addon Altered the data. 
                }
                catch(Exception exc)
                {
                    //Message = exc.Message;
                    Handle(exc, Message);
                }
            }
            else if (cmi.UseQueue)
            {
                if (cmi.MultiSelect)
                {
                    DataRowView[] batch = (from DataRowView item in MonitorData.SelectedItems
                                           select item).ToArray();
                    Models.ContextActionQueue.BatchQueueAction(cmi, batch, currentDB.ID.Value);
                }
                else
                    Models.ContextActionQueue.QueueAction(cmi, MonitorData.SelectedItem as DataRowView, _currentQuery.DBConnection.Value);
            }
            else if(cmi.ProcedureCall != null)
            {
                int rc = ContextAction(cmi, drv, currentDB);
                if (cmi.MultiSelect && !cmi.IsSwitch) //Ensure switch does not cause a multi select
                {
                    int limit = myMiscSettings.MultiSelectContextSprocLimit ?? MonitorData.SelectedItems.Count;
                    if (limit < 1 || limit > MonitorData.SelectedItems.Count)
                        limit = MonitorData.SelectedItems.Count;
                    //multiSelectProgress.Value = ((1 * 100) / limit) ;
                    //multiSelectProgress.Visibility = Visibility.Visible;
                    for (int i = 1; i < limit; i++)
                    {
                        //multiSelectProgress.Value = ((i+ 1) * 100) / limit;
                        drv = MonitorData.SelectedItems[i] as DataRowView;
                        if (drv == null)
                            continue;
                        rc += ContextAction(cmi, drv, currentDB);                        
                    }
                    //multiSelectProgress.Visibility = Visibility.Hidden;
                }
                if (/*cmi.Dashboard == null*/ rc > 0)
                    Refresh(true);
            }
            MonitorData.ContextMenu.IsEnabled = true;
            Cursor = Cursors.Arrow;
            e.Handled = true; //Prevent bubbling and running extra times
        }
        private int ContextAction(ContextMenuConfiguration cmi, DataRowView drv, Database db)
        {
            //int rc = 0;
            if (cmi.IsSwitch)
            {
                var nq = myQueries[cmi.SwitchID];
                if (nq == null)
                    return 0;
                _lastParameters = Models.ContextSwitch.DoContextSwitch(cmi, nq, drv);
                _currentQuery = nq;
                return 1; //force a refresh
            }
            return Models.ConetextMenuItemHelper.RunContextRC(cmi, drv, db);
            /*
            using (Models.ContextMenuItemQuery cmiq = new Models.ContextMenuItemQuery(cmi, drv, db))
            {
                rc = cmiq.RunContextAction(drv);     
                rc =            
            }
            return rc;
            */
        }
        #endregion

        public MainWindow()
        {            
            InitializeComponent();                        
            this.Width = 1200;
            DataContext = this; 
            MonitorData.Visibility = Visibility.Hidden;            
            MonitorData.MouseDoubleClick += (sender, e) =>
            {
                var temp = MonitorData.SelectedItem as DataRowView;
                if (temp == null)
                    return;
                EditableDashboardDisplay edd = new EditableDashboardDisplay(temp, "Record Detail View", readOnlyMode: true);
                edd.ShowDialog(true);
                /*
                var m = MonitorData.ItemsSource as DataView;
                if (m == null)
                    return;
                var tab = m.Table;
                var idx = MonitorData.CurrentCell.Column.DisplayIndex;
                
                string col = tab.Columns[idx].ColumnName;
                var r = new Alert(string.Format("Copy {0} to clipboard?", col), Confirmation: true).ShowDialog();
                if (r.HasValue && r.Value)
                {
                    var cellText = tab.Rows[MonitorData.SelectedIndex][idx].ToString();
                    Clipboard.SetText(cellText);
                }*/
                e.Handled = true; //Prevent bubbling and running extra times
            };
            ReadSettings();            
            SetupQueryMenu();

            //These view models sh ould replace the CheckVisibility
            EditorModel = new ConfigurationViewModels.EditorMenuViewModel(MyCurrentUser);            
            EditorModel.Reconfigured += ConfigurationMenuModel_Reconfigured;
            QueryModelRoot = ConfigurationViewModels.QueryMenuViewModel.CreateRoot(myQueries, MySession); //Use for command binding
            QueryModelRoot.PropertyChanged += QueryModelRoot_PropertyChanged;

            //AdminModel = new ConfigurationWindows.AdminMenuViewModel(MyCurrentUser);            
            //AdminModel.Reconfigured += ConfigurationMenuModel_Reconfigured;
            //SetupContextMenus();
            if (MyCurrentUser.CanUseAddons)
                SetupAddonMenu();
            CheckUserVisibility();
            if (SWID.HasValue && CurrentAccessMode != UserAccessMode.SingleUser)
                MyID.Content = "SWid:" + SWID.ToString();
            else
            {
                MyID.Visibility = Visibility.Collapsed;
                ID_Line.Visibility = Visibility.Collapsed;
            }

            //_SourceData = MySession[SOURCE_DATA_CACHE_KEY] as DataTable;
            //MySession[SOURCE_DATA_CACHE_KEY] = null; //Note necessary after making _SourceData into a cache based object at all times
            if (_SourceData != null)
            {
                SetupPaging(_SourceData);
                var q = _currentQuery;
                if(q != null)
                {
                    var db = myConnections[q.DBConnection];
                    SetSessionColor(
                        db?.ConnectionColor,
                        db?.TextColor);
                    MonitorData.ContextMenu = null;
                    MonitorData.ContextMenu = SetupMenuForQuery(q);
                }
                else if(_CurrentPlugin != null)
                {
                    MonitorData.ContextMenu = this._PluginMenu;
                    var db = myConnections[_pluginConfig.DatabaseID];
                    SetSessionColor(
                        db?.ConnectionColor, db?.TextColor);
                        //myConnections.GetColor(_pluginConfig.DBConnection), 
                        //myConnections.GetTextColor(_pluginConfig.DBConnection));
                }
                else
                    sessionColor = null;
                
                //RefreshTime.Content = MySession[REFRESH_LABEL_CACHE_KEY] ?? string.Empty;
                //MySession[REFRESH_LABEL_CACHE_KEY] = null;
                RefreshTime.Content = RefreshLabelContent;
            }
            else if (_currentQuery != null)
                Refresh(true);
            else
                sessionColor = null; //source data not set, current query is null, use default background color.

            UseSessionColor = true; //cause sessionColor to be used for this page.
                        

            //*
            _SettingRefresh = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 20)
                //Change to tick each minute and check the number of ticks before refreshing or something?
                //Can then use for query refresh as well
            };
            _SettingRefresh.Tick += _SettingRefresh_Tick;
            _SettingRefresh.Start();
            // */
            //_SettingRefresh = new System.Timers.Timer(30 * 1000);
            //_SettingRefresh.Elapsed += _SettingRefresh_Tick;
            //_SettingRefresh.Enabled = true;
            _ContextQueueProcess = new System.Timers.Timer(15 * 1000);
            _ContextQueueProcess.Elapsed += _ContextQueue_Tick;
            _ContextQueueProcess.Enabled = true;
        }

        private void QueryModelRoot_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(QueryModelRoot.QueryID) && _currentQuery != null)
            {
                if(_currentQuery == null)
                {                    
                    Handle("Query not found.", ExceptionLevel.UI_Basic);                    
                    return;
                }

                QueryMenu.IsEnabled = false;
                //not a refresh.
                _PluginMenu = null;
                try
                {
                    _pluginConfig = null;
                    Refresh(false);
                }
                catch (Exception exc)
                {
                    MonitorData.Visibility = Visibility.Hidden;
                    Handle(exc, "Unable to run query", ExceptionLevel.UI_Basic);
                    return;
                }
                finally
                {
                    QueryMenu.IsEnabled = true;                    
                }
                if (MonitorData.Items != null && MonitorData.Items.Count > 0)
                    MonitorData.Visibility = Visibility.Visible;
                else
                    MonitorData.Visibility = Visibility.Hidden; //No data - Hide it!
            }

        }

        private void ConfigurationMenuModel_Reconfigured(object sender, EventArgs e)
        {            
            if (sender == EditorModel)                
            {
                var ID = _currentQuery?.ID;
                if (ID != null && !myQueries.Contains(ID))
                    _currentQuery = null;
                SetupQueryMenu();
                MonitorData.ContextMenu = SetupMenuForQuery(_currentQuery);   
                //ToDo: Replace with a view model for context actions, reconfigured on current query
                //Call reconfigure on it when updating _currentQuery or when calling reconfigure on other view models..
            }
            
        }

        const string REFRESH_LABEL_CACHE_KEY = "_LastRefreshLabel";
        
        //System.Timers.Timer _SettingRefresh = null;
        System.Timers.Timer _ContextQueueProcess = null;
        DispatcherTimer _SettingRefresh = null; //affect UI, dispatch timer
        //DispatcherTimer _ContextQueueProcess = null; //Chagne to queue timer to run in background...
        DateTime? nextFileRefresh
        {
            get { return MySession["_nextFileRefresh"] as DateTime?; }
            set { MySession.SetCache("_nextFileRefresh", value); } //Use session so that this time can be restored after a logout
        }
        #region Permissions for user, attempt for refreshing data from network.
        void _SettingRefresh_Tick(object sender, EventArgs e)
        {
            multiSelectProgress.Value = Models.ContextActionQueue.QueueAllotmentFilled;
            multiSelectProgress.ToolTip = Models.ContextActionQueue.Status;
            if (!IsActive)
                return;
            DateTime check = DateTime.Now.AddMinutes(myMiscSettings.FileRefresh);
            if (nextFileRefresh == null || nextFileRefresh.Value > check)
                nextFileRefresh = check;

            if (_currentQuery != null
                && _currentQuery.RefreshTime != null
                && (_currentQuery.LastRunTime.AddMinutes(_currentQuery.RefreshTime.Value) < DateTime.Now))
                Refresh(true); //Auto refresh query.
            else if (_pluginConfig != null
                && _pluginConfig.NextCallback.HasValue
                && _pluginConfig.NextCallback >= DateTime.Now)
            {
                _pluginConfig.NextCallback = null;
                _Callback.Invoke();
            }


            if (nextFileRefresh.Value > DateTime.Now)
                return;
            //ReadSettings();
            //Reload();
            MyBroker.LoadConfigurations(false, MySession.CurrentAccessMode == UserAccessMode.SingleUser);
            SetupQueryMenu();
            //SetupContextMenus();
            //SetupAddonMenu();
            MonitorData.ContextMenu = SetupMenuForQuery(_currentQuery); //Rebuild context menu after reloading.
            
            //CheckUserVisibility();
            ReconfigureUserModels();
            nextFileRefresh = check;
        }
        [Obsolete("Should be replaced by binding via view models.. Maybe.")]
        void CheckUserVisibility()
        {     
            
            /*                  
            //ToDo: Add and check Singleuser mode in a 'deploymentConstants' static class. Current checks should only be in non single user mode. 
            if(MyCurrentUser.Team == null || CurrentAccessMode == UserAccessMode.SingleUser)
            {
                TeamMerge_Connection.Visibility = Visibility.Collapsed;
                TeamMerge_Context.Visibility = Visibility.Collapsed;
                TeamMerge_Query.Visibility = Visibility.Collapsed;
                TeamMerge_Plugin.Visibility = Visibility.Collapsed; 
            }
            else
            {
                //If unable to edit, a parent level MenuItem will already be collapsed, so we don't need to worry about anything but existence of Team
                TeamMerge_Connection.Visibility = Visibility.Visible;
                TeamMerge_Context.Visibility    = Visibility.Visible;
                TeamMerge_Query.Visibility      = Visibility.Visible;
                TeamMerge_Plugin.Visibility     = Visibility.Visible;
                TeamMerge_Connection.Header = "Merge '" + MyCurrentUser.Team + "' defaults with My Connections";
                TeamMerge_Context.Header = "Merge '" + MyCurrentUser.Team + "' defaults with My MenuItems";
                TeamMerge_Query.Header = "Merge '" + MyCurrentUser.Team + "' defaults with My Queries";
                TeamMerge_Plugin.Header = "Merge '" + MyCurrentUser.Team + "' defaults with My Menu Addons";
            }
            
            //if (!MyCurrentUser.CanExportData)
            if(!MyCurrentUser.CheckPermission(BasicUserPermissions.CanExportData))
            {
                ExportMain.Visibility = Visibility.Collapsed; //Made visible when there are query results to export
                //QueryMenu.Visibility = Visibility.Collapsed;
                //MonitorData.Visibility = Visibility.Hidden; //Only need to hide. 
                //note that Refreshing or running a query once permission is added will make visible
            }
            else
            {
                //QueryMenu.Visibility = Visibility.Visible;
                //ExportMain.Visibility = Visibility.Collapsed;
            }            

            if (MyCurrentUser.CheckPermission(BasicUserPermissions.DatabaseConnectionEditor))
                SettingConnection.Visibility = Visibility.Visible;            
            else
                SettingConnection.Visibility = Visibility.Collapsed;
            
            SettingContext.Visibility = MyCurrentUser.CheckPermission(BasicUserPermissions.ContextMenuEditor, BasicUserPermissions.QueryEditor) ? Visibility.Visible : Visibility.Collapsed;
            SettingQuery.Visibility = MyCurrentUser.CheckPermission(BasicUserPermissions.QueryEditor) ? Visibility.Visible : Visibility.Collapsed;
            AdminMenu.Visibility = MyCurrentUser.Admin 
                && CurrentAccessMode != UserAccessMode.SingleUser
                ? Visibility.Visible : Visibility.Collapsed;
            if(MyCurrentUser.CanUseAddons && myMenuLibrary?.IsValidState == true)
            {
                AddonRefresh.Visibility = Visibility.Visible;
                PluginMain.Visibility = Visibility.Visible;
            }
            else
            {
                AddonRefresh.Visibility = Visibility.Collapsed;
                PluginMain.Visibility = Visibility.Collapsed;
            }
            SettingWindowAddon.Visibility = MyCurrentUser.CheckPermission(BasicUserPermissions.AddonEditor)
                || MyCurrentUser.SuperAdmin ? Visibility.Visible : Visibility.Collapsed;            
            */
        }
        #endregion

        #region Cached variables        
        Query _currentQuery
        {
            //get{return MySession.CurrentQuery;}
            get
            {
                //return MySession["_CurrentQuery"] as Query;
                return MySession._currentQuery;
            }
            //set { MySession.CurrentQuery = value; }
            set
            {
                MySession._currentQuery = value;
                //MySession.SetCache("_CurrentQuery", value);
            }
        }
        QueryParameter[] _lastParameters
        {
            get { return MySession._lastParameters; }
            set { MySession._lastParameters = value; }
            //get { return MySession["_LastParams"] as QueryParameter[]; }
            //set { MySession.SetCache("_LastParams", value); }
        }
        string RefreshLabelContent
        {
            get
            {
                DateTime? check = MySession.CheckCacheStartTime(SOURCE_DATA_CACHE_KEY);
                if (check == null)
                    return string.Empty;
                return "Last Refresh: " + check.Value.ToString("MMM dd yyyy, hh:mm"); 
            }
        }
        #endregion
        private void Refresh(bool usePrevSetup = false)
        {            
            var currentQ = _currentQuery;
            WindowAddonLabel.Content = "";
            WindowAddonLabel.Visibility = Visibility.Collapsed;
            RefreshTime.Content = "Last Refresh: " + DateTime.Now.ToString("MMM dd yyyy, hh:mm");            
            RefreshTime.Visibility = Visibility.Visible;
            ColorCol = null;            
            
            if (currentQ == null)
            {
                SetSessionColor();
                return;
            }
            _Callback = null;
            MonitorData.ItemsSource = null;
            var dbID = currentQ.DBConnection;
            var db = myConnections[dbID];
            if (db == null)
            {
                SetSessionColor();
                Handle("The referenced Database Connection could not be found. ID: " + dbID ?? "(NULL)", ExceptionLevel.UI_Basic);
                //new Alert("The referenced Database connection could not be found: " + currentQuery.DBConnectionName, Choice: false).ShowDialog();                
                return;
            }            
            SetSessionColor(db.ConnectionColor, db.TextColor); //The list's Methods handle "DEFAULT" case, but have updated the set sessionColor to as well.
            Cursor = Cursors.Wait;
            QueryParameter[] myParams; //= currentQuery.GetParametersForRunTime();
            if (usePrevSetup && _lastParameters != null)
                myParams = _lastParameters;
            else
            {
                myParams = currentQ.GetParametersForRunTime();
                MonitorData.ContextMenu = null;
            }
            bool b = myParams == null;
            DataRowView qEditList = null;
            if (!b)
            {
                qEditList = QueryParameter.ConvertList(myParams, true);
                b = qEditList == null || qEditList.Row.Table.Columns.Count == 0;
            }
            if (!b)
            {
                EditableDashboardDisplay queryParamEditor 
                    = new EditableDashboardDisplay(qEditList, currentQ.Key + " - Parameters");
                b = queryParamEditor.ShowDialog() ?? false;
                if (b)
                    QueryParameter.MapDataRow(queryParamEditor.myDataRowView.Row, myParams);
                else
                {
                    //Mapping not done, remove current query setting
                    _currentQuery = null;
                    Cursor = Cursors.Arrow;
                    return;//may need to do clean up? Should be fine just clearing currentQuery from cache
                }
                //If(b) outside of this block, myParams is already mapped correctly.
            }
            if (b)
            {
                DataTable rDT = new DataTable();
                try
                {
                    rDT = currentQ
                        .Execute(db, myParams, SWID)
                        .GetFirstTableOrNull();
                }
                catch(Exception ex)
                {                 
                    Handle(ex, $"Error Running Query '{currentQ.Key}'({currentQ.ID})", ExceptionLevel.UI_Basic);
                    Cursor = Cursors.Arrow;
                    return;
                }
                #region old execution
                /*
                DataTable rDT = new DataTable();
                try
                {
                    //using (SqlConnection conn = new SqlConnection(db.ConnectionString))
                    using (var model = dbManager.GetBasicHelper())
                    {
                        //conn.Open();
                        
                        //using (SqlCommand c =
                        //    new SqlCommand(_currentQuery.ProcedureCall)
                        //    {
                        //        CommandType = CommandType.StoredProcedure,
                        //        Connection = conn,
                        //        //CommandTimeout = myMiscSettings.DefaultQueryTimeout                         
                        //    }
                        //)
                        {
                            if (myParams != null)
                            {
                                foreach (var param in myParams)
                                {
                                    var key = param.ParameterName;
                                    //if (key[0] != '@')
                                    //    key = "@" + key; //pretty sure it already handles all this..
                                    model[key] = param.ParameterValue;

                                    //c.Parameters.AddWithValue(key, param.ParameterValue ?? DBNull.Value);
                                    //c.Parameters[key].Value = param.ParameterValue ?? DBNull.Value;
                                    //Don't set missing values, require correcting parameters that don't exist
                                }                                
                            }
                            model.QualifiedProcedure = _currentQuery.ProcedureCall;
                            model["SWID"] = MyCurrentUser.ID;
                            rDT = dbManager.Execute(model).GetFirstTableOrNull();
                            //SqlDataAdapter sda = new SqlDataAdapter(c);
                            //sda.Fill(rDT);
                            //if (rDT.Columns.Contains("LMUID"))
                            //    rDT.Columns.Remove("LMUID");
                            /*
                            if (MyLMUID.HasValue)
                                rDT.Columns.Add(new DataColumn
                                {
                                    ColumnName = "LMUID",
                                    DataType = typeof(short),
                                    DefaultValue = MyLMUID
                                });
                            * /
                        }
                    }
                }
                catch(Exception ex)
                {
                    Handle(ex, $"Error Running Query '{currentQ.Key}'({currentQ.ID})", ExceptionLevel.UI_Basic);
                    Cursor = Cursors.Arrow;
                    return;
                }
                */
                #endregion
                SetupPaging(rDT);
                currentQ.LastRunTime = DateTime.Now;
                _lastParameters = myParams;
                if(!usePrevSetup || MonitorData.ContextMenu == null)
                    MonitorData.ContextMenu = SetupMenuForQuery(currentQ);
                Cursor = Cursors.Arrow;
                return;
            }
            #region old logic...
#if DEBUG
            /*
            MainQueryParamSetup mySetup;
            if (usePrevSetup && _lastSetup != null)
                mySetup = _lastSetup;
            else
            {
                mySetup = new MainQueryParamSetup();
                if(_currentQuery.NeedsParameterEvaluation())
                //if (currentQuery.FromDateParam != null || currentQuery.ThroughDateParam != null || currentQuery.ActiveParam != null || currentQuery.ExtraFilter != null)
                {
                    //Display prompt for values. ToDo: Create a query model object to pass to it?
                    /*
                    QueryParameterWindow qpw
                        = new QueryParameterWindow(
                            _currentQuery.FromDateParam,
                            _currentQuery.ThroughDateParam,
                            _currentQuery.ActiveParam,
                            _currentQuery.ExtraFilter,
                            _currentQuery.IntParam1,
                            _currentQuery.IntParam2);
                    var dResult = qpw.ShowDialog();
                    if (!dResult.HasValue || !dResult.Value)
                        return;
                    mySetup = qpw.tempSetup; //Values default to null so ok to set without checking whether it should be or not                    
                    * /
                }
            }
            
            //string connString = DBData.DatabaseConnectionManager.GetConnectionString(currentQuery);
            string connString = db.ConnectionString;
            
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                using (SqlCommand c = new SqlCommand(_currentQuery.ProcedureCall)
                {
                    CommandType = CommandType.StoredProcedure,
                    Connection = conn,
                    CommandTimeout = myMiscSettings.DefaultQueryTimeout
                })
                {
                    mySetup.PopulateCommandParams(c, _currentQuery); /*
                    if (currentQuery.FromDateParam != null)
                        c.Parameters.AddWithValue("@" + currentQuery.FromDateParam, mySetup.FromDateParam.HasValue ? mySetup.FromDateParam.Value as object : DBNull.Value);
                    if (currentQuery.ThroughDateParam != null)
                        c.Parameters.AddWithValue("@" + currentQuery.ThroughDateParam, mySetup.ThroughDateParam.HasValue ? mySetup.ThroughDateParam.Value as object : DBNull.Value);
                    if (currentQuery.ActiveParam != null)
                        c.Parameters.AddWithValue("@" + currentQuery.ActiveParam, mySetup.ActiveParam.HasValue ? mySetup.ActiveParam.Value as object : DBNull.Value);
                    if (currentQuery.ExtraFilter != null)
                        c.Parameters.AddWithValue("@" + currentQuery.ExtraFilter, mySetup.ExtraFilter == null ? mySetup.ExtraFilter as object : DBNull.Value);
                    * /
                    //SqlDataReader r = c.ExecuteReader();
                    //dt.Load(r);
                    SqlDataAdapter sda = new SqlDataAdapter(c);
                    sda.Fill(dt);                    
                }
            }


            if (dt.Columns.Contains("LMUID"))
                dt.Columns.Remove("LMUID");
            if(MyLMUID.HasValue)
                dt.Columns.Add(new DataColumn {
                    ColumnName = "LMUID",
                    DataType = typeof(short),
                    DefaultValue = MyLMUID });
            SetupPaging(dt);
            //MonitorData.ItemsSource = dt.AsDataView(); //Replaced by set up paging.
            _lastSetup = mySetup;
            _currentQuery.LastRunTime = DateTime.Now;
            if (!usePrevSetup || MonitorData.ContextMenu == null)
                MonitorData.ContextMenu = SetupMenuForQuery(_currentQuery);
            if (menus.ContainsKey(_currentQuery.Name))            
                MonitorData.ContextMenu = menus[_currentQuery.Name];                
                */
#endif

            #endregion

        }
        /// <summary>
        /// Deprecate, replace with Command + Execute? Should still clear _pluginConfig/menu...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QueryMenu_Click(object sender, RoutedEventArgs e)
        {                        
            MenuItem m = e.OriginalSource as MenuItem; //sender vs original source?
            if (m != null)
            {
                
                bool useLastParam = false;
                QueryMenu.IsEnabled = false;
                //Allows us to use the same method for refreshing
                if (m.Name != "Refresh")
                {
                    int mID = int.Parse(m.Name);
                    _PluginMenu = null;
                    var check = _currentQuery?.ID;
                    if (check == mID)
                        useLastParam = true;
                    else                    
                        _currentQuery = myQueries[mID]; // QueryList[m.Name];                                        
                }
                else
                    useLastParam = true;
                try {
                    _pluginConfig = null;
                    Refresh(useLastParam);
                }
                catch(Exception exc)
                {
                    MonitorData.Visibility = Visibility.Hidden;
                    Handle(exc, "Unable to run query",  ExceptionLevel.UI_Basic);
                    //new Alert(exc.Message + "\r\nUnable to run query.", Choice: false).ShowDialog();                    
                    return;
                }
                finally
                {
                    QueryMenu.IsEnabled = true;
                    e.Handled = true; //Prevent bubbling and running extra times. 
                                      //We're always handling this at the bottom level basically.
                }
                if (MonitorData.Items != null && MonitorData.Items.Count > 0)
                    MonitorData.Visibility = Visibility.Visible;
                else
                    MonitorData.Visibility = Visibility.Hidden; //No data - Hide it!
            }
            
        }
        
#region main Setting Management
        /*
        private void SettingQuery_Click(object sender, RoutedEventArgs e)
        {
            var d = new iConfigListDisplay(myQueries); 
            var r = d.ShowDialog();
            if(r && d.Refresh)
            {
                //iConfigManager.SaveRefresh();
                SetupQueryMenu();
            }
            e.Handled = true; //Prevent bubbling and running extra times
        }

        private void SettingConnection_Click(object sender, RoutedEventArgs e)
        {
            new iConfigListDisplay(myConnections).ShowDialog();
            e.Handled = true; //Prevent bubbling and running extra times              
        }

        private void SettingContext_Click(object sender, RoutedEventArgs e)
        {
            var d = new iConfigListDisplay(myContextMenus);
            var r = d.ShowDialog();
            if (r && d.Refresh)
            {
                //iConfigManager.SaveRefresh();
                //SetupContextMenus();
                var q = _currentQuery;
                if(q != null)
                {
                    MonitorData.ContextMenu = SetupMenuForQuery(q);
                }
            }
            e.Handled = true; //Prevent bubbling and running extra times
        }*/
#endregion
        

#region data export
        private void Export_pipe_Click(object sender, RoutedEventArgs e)
        {
            string f = FileSaveHelper.GetSaveFile("All (*.*)|*.*|DAT File (*.dat)|*.dat|Text File (*.txt)|*.txt|CSV File (*.csv)|*.csv", ".dat");
            if(f != null){
                //DataTable dt = ((DataView) MonitorData.ItemsSource).Table;
                FileSaveHelper.Convert(f, "|", _SourceData);
            }
            e.Handled = true; //Prevent bubbling and running extra times
        }

        private void Export_csv_Click(object sender, RoutedEventArgs e)
        {
            string f = FileSaveHelper.GetSaveFile("All (*.*)|*.*|CSV File (*.csv)|*.csv", ".csv");
            if (f != null)
            {
                //DataTable dt = ((DataView)MonitorData.ItemsSource).Table;
                FileSaveHelper.Convert(f, ",", _SourceData);
            }
            e.Handled = true; //Prevent bubbling and running extra times
        }

        private void Export_XLS_Click(object sender, RoutedEventArgs e)
        {
            //DataTable dt = ((DataView)MonitorData.ItemsSource).Table;
            string f = FileSaveHelper.GetSaveFile("XLSX Workbook (*.xlsx)|*.xlsx", ".xlsx");
            FileSaveHelper.WriteExcelFile(f, _SourceData);
            e.Handled = true; //Prevent bubbling and running extra times
        }
#endregion
        
        private void SettingFolder_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = ConfigFolder.GetFolder(MiscSetting.APP_NAME);
            System.Diagnostics.Process.Start(folderPath);
            e.Handled = true; //Prevent bubbling and running extra times
        }

        
        private void SettingForceRefresh_Click(object sender, RoutedEventArgs e)
        {
            /*Reload()*/;
            MyBroker.LoadConfigurations(false, SingleUserMode);
            //ToDo: this should replace checking user visibility via binding
            ReconfigureUserModels();
            e.Handled = true; //Prevent bubbling and running extra times
            /*
            var ID = _currentQuery?.ID;
            if (ID != null && !myQueries.Contains(ID))
                _currentQuery = null;

            SetupQueryMenu();
            MonitorData.ContextMenu = SetupMenuForQuery(_currentQuery);
            //SetupContextMenus();
            //SetupAddonMenu();
            //if (MyCurrentUser.CanUseAddons)
            //    SetupAddonMenu();  //don't set up addon menu unless the addon menu is force refreshed
            CheckUserVisibility();


            
            */
            
        }
        /// <summary>
        /// Reset the models for user visibility
        /// </summary>
        public void ReconfigureUserModels()
        {
            EditorModel.ReConfigure(MyCurrentUser);
            QueryModelRoot.Reconfigure(myQueries);
            _QueueEnabled = _QueueEnabled;// Checks user permission in case of change
            //AdminModel.ReConfigure(MyCurrentUser);
        }
        private void SettingMisc_Click(object sender, RoutedEventArgs e)
        {
            
            List<string> Ignore = new List<string>();
            if(myMiscSettings.LogoutTime <= 0 || myMiscSettings.LogoutTime == BasicUserSessionManager.DEFAULT_LOGOUT_TIME_MINUTES)
                Ignore.Add("LogoutTime"); //Ignore logout time if it's less than or equal to 0, or is the default already.
            //if (!MyCurrentUser.Admin)
            //    Ignore.Add("MyAccessMode");             
            //if (!MyCurrentUser.SuperAdmin && !myMiscSettings.SkipLogin)
            //    Ignore.Add("SkipLogin"); //Skip if not in use already and not a super admin.
            
            //UserAccessMode current = myMiscSettings.MyAccessMode;
            EditableObjectDisplay msp = new EditableObjectDisplay(
                myMiscSettings, 
                Title:"My Settings", 
                ManagedSaving:true,
                ExcludeProperties:Ignore.ToArray());//Note that single user mode is always admin already
            try
            {
                var r = msp.ShowDialog();
                if (r ?? false)
                {

                    myMiscSettings = (MiscSetting)msp.myData;
                    myMiscSettings.Save();
                    MySession.SetAlertLevel(myMiscSettings.MyExceptionAlertLevel);                                      
                    //SetExceptionManager(new ExceptionManager(MyAppName, myMiscSettings.ErrorLog, myMiscSettings.MyExceptionAlertLevel));
                    Models.ContextActionQueue.QueueLimit = myMiscSettings.QueueLimit;
                    Models.ContextActionQueue.BatchSize = myMiscSettings.QueueBatchSize;
                    /* 
                    //Don't allow changing access mode from settings anymore..
                    if (current != myMiscSettings.MyAccessMode)
                    {
                        //MySession.SetAccessMode(myMiscSettings.MyAccessMode);
                        MonitorData.ItemsSource = null;
                        MonitorData.Visibility = Visibility.Hidden;
                        MonitorData.ContextMenu = null;
                        sessionColor = null;
                        _SourceData = null;
                        _currentQuery = null;
                        _pluginConfig = null;
                        _Callback = null;
                        SetupQueryMenu();
                        SetupAddonMenu();
                        SetupPaging(null);
                        //Causing a change to the user returned by currentUser. need to re-check things
                    }
                    */
                }
                else
                {
                    myMiscSettings = MiscSetting.LoadFromFile(); //Reload if user canceled the save
                }
            }
            catch(Exception ex)
            {
                Handle(ex, "Unable to update Misc Settings", ExceptionLevel.UI_Basic);
            }
            e.Handled = true; //Prevent bubbling and running extra times
            /*
            if (r.HasValue && r.Value)
                mySetting = MiscSetting.LoadFromFile();
                */
        }
#region DataGrid events
        private void MonitorData_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string cl = e.PropertyName.ToUpper();
            if (cl == "COLOR")
            {
                //e.Cancel = true;
                e.Column.Visibility = Visibility.Collapsed; //Cancel? OrJust hidden to make sure the data is available?
                ColorCol = e.PropertyName;
            }
            else if(
                 //cl == "LMUID" || //update: Set this in the parameter model for passing instead of using a hidden data row column..
                cl.StartsWith("HDN")  // Hidden column. Doesn't need to be seen by the user, but may be useful in a plugin or something.
                                         // e.g. hdnURL to open a web page with a query string specific to the record
                || cl.StartsWith("DTL_")  //Secondary Detail column - should only be seen in the details (and be read-only). 
                                          //E.g., load progress details that go beyond what the user needs to see for 
                                          // overall (so just when looking at the specific record)
                )
            {
                e.Column.Visibility = Visibility.Collapsed;
            }            
        }
        string ColorCol = null;
        private void MonitorData_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            
            
            DataRowView row = e.Row.Item as DataRowView;
            if (row != null && ColorCol != null)
            {
                string color = row[ColorCol]?.ToString().Trim();
                if (color.Length == 0)
                    return;
                
                var brush = (from bColor in typeof(Brushes).GetProperties()
                             where bColor.Name == color 
                                && bColor.PropertyType == typeof(SolidColorBrush)
                             select (SolidColorBrush)bColor.GetValue(null)).FirstOrDefault();
                if (brush != null)
                    e.Row.Background = brush;                
                /*
                try
                //Action r = (() =>
                {
                    e.Row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
                }
                catch (Exception ex)
                {
                    Handle(ex, $"Error setting background color for Row (Color='{color.ToString()}'", ExceptionLevel.UI_Advanced);
                }
                */
            }
            
            /*);
            r.WithoutErrorHandling();
            */
        }
#endregion

#region Admin Click Events
        /*
        private void Admin_Users_Click(object sender, RoutedEventArgs e)
        {
            //Shouldn't need to check really, but...
            if(CurrentAccessMode == UserAccessMode.SingleUser || MyCurrentUser.IMPERSONATION)
            {
                var m = (sender as MenuItem);
                if(m!= null)
                {
                    m.Visibility = Visibility.Collapsed;
                }
                e.Handled = true;
                return;
            }
            //Note, we don't really care about results because users should not be able to modify their own user setting entry
            new iConfigListDisplay(GetUsersCopy()).ShowDialog();  //Does not use the DefaultConfig_Edit because there's only one User file
                                                            //with a local copy of the network version.
            e.Handled = true;
        }

        private void Admin_Queries_Click(object sender, RoutedEventArgs e)
        {
            var tc = new HelperWindows.TeamChooser(true);
            bool? r = !tc.NeedShow; //flip so that it matches what would be the dialog result of success.
            if(!r.Value)
                tc.ShowDialog();
            if (r.HasValue && r.Value)
            {
                string t = tc.Team;
                var ce = new SettingWindows.DefaultConfig_Edit(myQueries, iConfigListType.Query, true, t);
                if (!ce.OkContinue)
                    return;
                r = ce.ShowDialog();
                if(r.HasValue && r.Value)
                {
                    SetupQueryMenu();
                }
            }
        }

        private void Admin_DBs_Click(object sender, RoutedEventArgs e)
        {
            var tc = new HelperWindows.TeamChooser(true);
            bool? r = !tc.NeedShow;
            if (!r.Value)
                tc.ShowDialog();
            if (r.HasValue && r.Value)
            {
                string t = tc.Team;
                var ce = new SettingWindows.DefaultConfig_Edit(myConnections, iConfigListType.DatabaseConnection, true, t);
                if (!ce.OkContinue)
                    return;
                ce.ShowDialog();
            }
        }

        private void Admin_Context_Click(object sender, RoutedEventArgs e)
        {
            var tc = new HelperWindows.TeamChooser(true);
            bool? r = !tc.NeedShow;
            if (!r.Value)
                tc.ShowDialog();
            if (r.HasValue && r.Value)
            {
                string t = tc.Team;
                var ce = new SettingWindows.DefaultConfig_Edit(myContextMenus, iConfigListType.ContextMenu, true, t);
                if (!ce.OkContinue)
                    return;
                r = ce.ShowDialog();
                if (r.HasValue && r.Value)
                {
                    //SetupContextMenus();
                    MonitorData.ContextMenu = null;
                    Dynamics.Configurations.Query q = _currentQuery;
                    if(q!= null)
                        MonitorData.ContextMenu = SetupMenuForQuery(q);
                }
            }
        }
        */
#endregion
        
#region Merge menus
            /*
        private void DefaultMerge_Connection_Click(object sender, RoutedEventArgs e)
        {            
            var ce = new SettingWindows.DefaultConfig_Edit(myConnections, iConfigListType.DatabaseConnection, false, null);
            if (!ce.OkContinue)
                return;
            ce.ShowDialog();
            e.Handled = true;
        }

        private void DefaultMerge_Team_Click(object sender, RoutedEventArgs e)
        {
            string Team = MyCurrentUser.Team;
            var ce = new SettingWindows.DefaultConfig_Edit(myConnections, iConfigListType.DatabaseConnection, false, Team);
            if (!ce.OkContinue)
                return;
            ce.ShowDialog();
            e.Handled = true;
        }

        private void DefaultMerge_Query_Click(object sender, RoutedEventArgs e)
        {
            var ce = new SettingWindows.DefaultConfig_Edit(myQueries, iConfigListType.Query, false, null);
            if (!ce.OkContinue)
                return;
            var r = ce.ShowDialog();
            if (r.HasValue && r.Value)
            {
                SetupQueryMenu();
            }
            e.Handled = true;
        }

        private void TeamMerge_Query_Click(object sender, RoutedEventArgs e)
        {
            string Team = MyCurrentUser.MyTeam;
            var ce = new SettingWindows.DefaultConfig_Edit(myQueries, iConfigListType.Query, false, Team);
            if (!ce.OkContinue)
                return;
            var r = ce.ShowDialog();
            if (r.HasValue && r.Value)
            {
                SetupQueryMenu();
            }
            e.Handled = true;
        }

        private void DefaultMerge_Context_Click(object sender, RoutedEventArgs e)
        {
            var ce = new SettingWindows.DefaultConfig_Edit(myContextMenus, iConfigListType.ContextMenu, false, null);
            if (!ce.OkContinue)
                return;
            var r = ce.ShowDialog();
            if (r.HasValue && r.Value)
            {
                //SetupContextMenus();
                var q = _currentQuery;
                if(q!= null)
                {
                    MonitorData.ContextMenu = SetupMenuForQuery(q);
                }
            }
            e.Handled = true;
        }
        private void DefaultMerge_Plugin_Click(object sender, RoutedEventArgs e)
        {
            var ce = new SettingWindows.DefaultConfig_Edit(myAddons, iConfigListType.SEIDR_MenuAddOn, false);
            if (!ce.OkContinue)
                return;
            var r = ce.ShowDialog();
            if (r.HasValue && r.Value)
            {
                SetupAddonMenu();
            }
            e.Handled = true;
        }

        private void TeamMerge_Plugin_Click(object sender, RoutedEventArgs e)
        {
            string Team = MyCurrentUser.MyTeam;
            var ce = new SettingWindows.DefaultConfig_Edit(myAddons, iConfigListType.SEIDR_MenuAddOn, false, Team);
            if (!ce.OkContinue)
                return;
            var r = ce.ShowDialog();
            if (r.HasValue && r.Value)
            {
                SetupAddonMenu();
            }
            e.Handled = true;
        }

        private void TeamMerge_Context_Click(object sender, RoutedEventArgs e)
        {
            string Team = MyCurrentUser.MyTeam;
            var ce = new SettingWindows.DefaultConfig_Edit(myContextMenus, iConfigListType.ContextMenu, false, Team);
            if (!ce.OkContinue)
                return;
            var r = ce.ShowDialog();
            if (r.HasValue && r.Value)
            {
                //SetupContextMenus();
                var q = _currentQuery;
                if (q != null)
                {
                    MonitorData.ContextMenu = SetupMenuForQuery(q);
                }
            }
            e.Handled = true;
        }
        */
#endregion
#region Addon Libraries, implement plugin updates to the MainWindow
        private void AddonRefresh_Click(object sender, RoutedEventArgs e)
        {
            ForceAddonRefresh();
            SetupAddonMenu();
            e.Handled = true; //Prevent bubbling and running extra times
        }

        private void AddonFolder_Open_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(LibraryFolder);
            }
            catch(Exception ex)
            {
                Handle(ex, "Unable to open folder", ExceptionLevel.UI);
                //AddonFolder_Open.IsEnabled = false;
            }
            e.Handled = true; //Prevent bubbling and running extra times
        }
        /*
        private void Admin_Addons_Click(object sender, RoutedEventArgs e)
        {            
            var tc = new HelperWindows.TeamChooser(true);
            bool? r = !tc.NeedShow;
            if (!r.Value)
                r = tc.ShowDialog();

            if (r.HasValue && r.Value)
            {
                string t = tc.Team;
                var ce = new SettingWindows.DefaultConfig_Edit(myAddons, iConfigListType.SEIDR_MenuAddOn, true, t);                
                r = ce.ShowDialog();
                if (r.HasValue && r.Value)
                {
                    SetupAddonMenu();
                }
            }
            e.Handled = true; //Prevent bubbling and running extra times
        }

        private void SettingWindowAddon_Click(object sender, RoutedEventArgs e)
        {
            iConfigListDisplay icd = new iConfigListDisplay(myAddons);
            icd.ShowDialog();
            if ( icd.Refresh)
                SetupAddonMenu();
            e.Handled = true; //Prevent bubbling and running extra times
        }
        */
        #region plugin Cached properties
        WindowAddonConfiguration _pluginConfig
        {
            get
            {
                return MySession["_APP_PLUGIN_CONFIG"] 
                    as WindowAddonConfiguration;
            }
            set
            {                
                MySession["_APP_PLUGIN_CONFIG"] = value;
            }
        }
        Action _Callback { get
            {
                return MySession["_PLUGIN_CALLBACK"] as Action;
            }
            set
            {
                MySession["_PLUGIN_CALLBACK"] = value;
            }
        }        
        ContextMenu _PluginMenu
        {
            get
            {
                return MySession["_APP_PLUGIN_MENU"] as ContextMenu;
            }
            set
            {
                MySession["_APP_PLUGIN_MENU"] = value;
            }
        }
        string _CurrentPlugin
        {
            get
            {
                return _pluginConfig.Key;                
            }
        }
#endregion
        public void UpdateDisplay(DataTable dt, int pluginID, ContextMenu setupMenu = null, Action Callback = null, ushort? WaitPeriod = null)
        {
            //_pluginConfig = myAddons[pluginName];
            _pluginConfig = myWindowAddons[pluginID];
            if (_pluginConfig == null)
            {
                var e = new ArgumentException("Plugin Name not found! Use the 'internalName' passed to Setup");
                Handle(e, "Plugin call was unable to update the Data Display", ExceptionLevel.UI_Advanced);
                throw e;
            }
            _Callback = Callback;
            if (Callback != null)
            {
                ushort temp = WaitPeriod ?? 5;               
                _pluginConfig.NextCallback = DateTime.Now.AddMinutes(temp);
            }
            else
                _pluginConfig.NextCallback = null;
            //MonitorData.ItemsSource = dt.AsDataView();
            var db = myConnections[_pluginConfig.DatabaseID];
            SetSessionColor(db.ConnectionColor, db.TextColor);
            SetupPaging(dt);            
            _currentQuery = null;
            
            //MonitorData.ContextMenu = menus[pluginName];
            try
            {
                SetupPluginContextMenu(_pluginConfig, setupMenu);//make sure this is done after setting up paging..
            }catch(Exception e)
            {
                Handle(e, "Unable to set up Plugin Context Menu");
            }
        }
        
        public void UpdateDisplayNoContextChange(DataTable dt, ushort? waitPeriod = null)
        {
            ContextMenu hold = MonitorData.ContextMenu; //keep the Context menu because we lose it after resetting the itemsSource
            if (_Callback != null && waitPeriod.HasValue)
                _pluginConfig.NextCallback = DateTime.Now.AddMinutes(waitPeriod.Value); 
            //MonitorData.ItemsSource = dt.AsDataView(); 
            SetupPaging(dt);
            MonitorData.ContextMenu = hold;
        }
        private void SetupPluginContextMenu(WindowAddonConfiguration plugin /*int pluginID*/, ContextMenu startup)
        {
            MonitorData.ContextMenu = null;                       
            //if (!MyCurrentUser.CanOpenContextMenus)
            //    return; 
            ContextMenu w = startup?? new ContextMenu();
            
            var items = myContextMenus.GetChildren(plugin /*pluginName*/);// menuItems.GetChildren(q.Name);
            if (startup != null && items.HasMinimumCount(1))
                w.Items.Add(new Separator());
            if (MyCurrentUser.CheckPermission(BasicUserPermissions.ContextMenuEditor))
            { 
                foreach (var item in items)
                {                    
                    /*
                    if (item.Dashboard != null && !MyCurrentUser.CanOpenDashboard
                        /*|| item.AddOn != null && !Me.CanUseAddons ) //Note: don't need to check addon when context menu is from an Addon
                        continue;
                    */
                    w.Items.Add(AddChildren(item, false)); //Add children without checking for addons.                                    
                }
                if (items.HasMinimumCount(1))
                    w.Items.Add(new Separator());
            }
            var m = new MenuItem
            {
                Header = "Copy Value",
                Name = /*MenuItemBuilder.CleanName(plugin.Key)*/ plugin.ID + "_CONTEXT_COPY"
            };
            m.Click += (sender, e) => {
                var v = MonitorData.ItemsSource as DataView;
                if (v == null || MonitorData.SelectedIndex < 0 || MonitorData.SelectedCells.Count == 0)
                    return;
                int h = MonitorData.CurrentCell.Column.DisplayIndex; //.ToString();
                if (h >= 0)
                {
                    string data = v.Table.Rows[MonitorData.SelectedIndex][h].ToString();
                    Clipboard.SetText(data);
                }            
                e.Handled = true; //Prevent bubbling and running extra times
            };
            w.Items.Add(m);
            _PluginMenu = w;
            MonitorData.ContextMenu = w;
            MonitorData.ContextMenu.Visibility = Visibility.Visible;
        }

        public void UpdateLabel(string pluginName, string LabelText)
        {
            WindowAddonLabel.Content = $"SEIDR Plugin - {pluginName}: {LabelText}";
            WindowAddonLabel.Visibility = Visibility.Visible;
        }

        private void PluginFolder_Open_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(WindowLibraryFolder);
        }
#endregion

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //ToDo: Use a status bar, see about taking this logic off..

            /*DividerMiddleLine.Width = ((sender as Window).Width / 2) - 240;*/
            WindowAddonLabel.Width = Width - 672;
                /*208 default width... 880 - 208 for length to take off*/
        }
#region MonitorData paging
        int _CurrentPage = 0;
        int _pageSize = 25;
        int _PageCount = 0;
        const string SOURCE_DATA_CACHE_KEY = "_SourceData";
        DataTable _SourceData
        { //Consideration? We could store _Source data in the session cache and restore it after login.
            //Note that underscore cache keys will not be removed automatically by session manager
            get
            {
                return MySession[SOURCE_DATA_CACHE_KEY] as DataTable;
            }
            set
            {
                MySession[SOURCE_DATA_CACHE_KEY] = value;
            }
        }
        /// <summary>
        /// Sets up the datatable to be grabbed in 'pages'. 
        /// <para>Also sets visibility of export menu depending on permission and number of rows
        /// </para>
        /// </summary>
        /// <param name="dt"></param>
        private void SetupPaging(DataTable dt)
        {
            ExportMain.Visibility = 
                dt != null 
                && dt.Rows.Count > 0 
                && MyCurrentUser.CheckPermission(BasicUserPermissions.CanExportData)                     
                ? Visibility.Visible 
                : Visibility.Collapsed; //Export Menu
            _pageSize = (int)myMiscSettings.DataTablePageSize;
            if (dt == null)
                _PageCount = -1;
            else
            {
                _PageCount = dt.Rows.Count / _pageSize;
                if (dt.Rows.Count % _pageSize == 0)
                    _PageCount--; //Subtract one due to zero based pages and not having leftover records
            }
            PagePicker.Maximum = _PageCount; //1 through count, access pagePicker value - 1
            if(_PageCount > 1)
            {
                PagePicker.Visibility = Visibility.Visible;
                GoToPage.Visibility = Visibility.Visible;
            }
            else
            {
                PagePicker.Visibility = Visibility.Hidden;
                GoToPage.Visibility = Visibility.Hidden;
                if(dt == null)
                {
                    if (_SourceData != null)
                    {
                        _SourceData.Clear();
                        _SourceData.Dispose();
                        _SourceData = null;
                    }
                    return;
                }
                
            }
            if (dt.Rows.Count > 0 && MyCurrentUser.CheckPermission(BasicUserPermissions.CanExportData))
            {
                ExportMain.IsEnabled = true;
                ExportMain.Visibility = Visibility.Visible;
                MonitorData.Visibility = Visibility.Visible;
            }
            else if(dt != null)
            {
                ExportMain.Visibility = Visibility.Collapsed;
                MonitorData.Visibility = Visibility.Collapsed;
                Handle("No results");
            }
            if (_SourceData != null && dt.TableName != "SOURCE_DATA")
            {
                _SourceData.Clear();
                _SourceData.Dispose();
            }
            _SourceData = dt;
            _SourceData.TableName = "SOURCE_DATA";
            if(_SourceData.Rows.Count > 0)
                SetPage(0);
        }
        private void SetPage(int x)
        {
            Cursor = Cursors.Wait;            
            MonitorData.ItemsSource = null;
            this.IsEnabled = false;
            try
            {
                _CurrentPage = x;
                PagePicker.Value = x + 1; //Page picking is 1 based
                MonitorData.ItemsSource = PageDataTable(_SourceData, _CurrentPage, _pageSize);
                PageNumber.Content = $"Page # {x + 1}/{_PageCount + 1}";

                //var d = _SourceData.Rows.Cast<DataRow>();
                //if (_CurrentPage == 0)
                //    MonitorData.ItemsSource = d.Take(_pageSize).CopyToDataTable().AsDataView();
                //else
                //    MonitorData.ItemsSource = d.Skip(_CurrentPage * _pageSize).Take(_pageSize).CopyToDataTable().AsDataView();
                /*MonitorData.ItemsSource = _SourceData.Rows.Cast<DataRow>()
                                                .Skip((_CurrentPage - 1) * _pageSize)
                                                .Take(_pageSize)
                                                .CopyToDataTable()
                                                .AsDataView();
                                                */
            }
            catch(Exception ex)
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
        }
        
        private void GoToPage_Click(object sender, RoutedEventArgs e)
        {
            if (_SourceData == null)
            {
                GoToPage.IsEnabled = false;
                PagePicker.IsEnabled = false;
                return;
            }
            SetPage((int)PagePicker.Value - 1);
        }
#endregion
#region help docs
        private void help_Menu_Click(object sender, RoutedEventArgs e)
        {
            DocDisplayWindow ddw = new DocDisplayWindow(DocDisplayWindow.GENERAL);
            ddw.Show();
            e.Handled = true;
        }

        private void plugins_Click(object sender, RoutedEventArgs e)
        {
            DocDisplayWindow ddw = new DocDisplayWindow(DocDisplayWindow.PLUGINS);
            ddw.Show();
            e.Handled = true;
        }

        private void charts_Click(object sender, RoutedEventArgs e)
        {
            DocDisplayWindow ddw = new DocDisplayWindow(DocDisplayWindow.CHARTS);
            ddw.Show();
            e.Handled = true;
        }

        private void query_Details_Click(object sender, RoutedEventArgs e)
        {

            DocDisplayWindow ddw = new DocDisplayWindow(DocDisplayWindow.QUERIES);
            ddw.Show();
            e.Handled = true;
            /*
            //TODO: Open a help page with string resources explaining topic. maybe faq-ish... 
            //take an array of strings and do a bullet list after main description?
            string Details = "Queries should call stored procedures with one table set. Results will be paged depending on settings. Each row's background color can be modified by query results if a 'COLOR' column is included"
                    + Environment.NewLine + "Columns starting with 'hdn' will be hidden but useable in context menus and addons - useful in cases like using a context addon that can start processes to open your browser to a website to a site indicated by hdnURL"
                    + Environment.NewLine + "Columns starting with 'dtl' will be hidden until the details are viewed - this can be done by double clicking or if there's a context menu set up with single detail view checked."
                    + " Also, Columns ending with 'progress' and containing a numeric value will be turned into a progress bar on the detail page."
                    + Environment.NewLine + Environment.NewLine + "Query parameters should be set from the query editor before saving a query - they can be marked as being something to override at run time, however."
                    + " Note that removing/adding parameters may cause the UI to be unable to run the stored procedure - this can be corrected either in the XML setting file or by rebuilding the parameters"
                    ;
                    
            */
        }
        private void context_Details_Click(object sender, RoutedEventArgs e)
        {
            DocDisplayWindow ddw = new DocDisplayWindow(DocDisplayWindow.CONTEXT_MENUS);
            ddw.Show();
            e.Handled = true;
        }
#endregion
        private void logFolder_Click(object sender, RoutedEventArgs e)
        {
            //string dir = ConfigFolder.GetSafePath(miscSettingMyAppName, ExceptionManager.LOG_SUBFOLDER);
            string dir = WindowExceptionManager.GetDirectory();
            try
            {
                System.Diagnostics.Process.Start(dir);
            }
            catch (Exception ex)
            {
                Handle(ex, "Unable to open Error Log folder", ExceptionLevel.UI_Basic);
            }
        }
        #region Multi select Queue info
        private void multiSelectProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            /*
             
             */
            MetroProgressBar pb = sender as MetroProgressBar;
            if (pb == null)
                return;
            if (pb.Value < 30)
                pb.Foreground = Brushes.Red;
            else if (pb.Value < 40)
                pb.Foreground = Brushes.Orange;
            else if (pb.Value < 60)
                pb.Foreground = Brushes.Yellow;
            else if (pb.Value < 80)
                pb.Foreground = Brushes.YellowGreen;
            else if (pb.Value < 95)
                pb.Foreground = Brushes.GreenYellow;
            else
                pb.Foreground = Brushes.Green;
        }

        private void Queue_Control_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b == null)
                return;
            b.Visibility = Visibility.Collapsed;
            if (b == Queue_Start)
            {
                Queue_Stop.Visibility = Visibility.Visible;                
                _QueueEnabled = true;
            }
            else
            {
                Queue_Start.Visibility = Visibility.Visible;
                _QueueEnabled = false;
            }
        }
        bool _QueueEnabled
        {
            get
            {                
                bool? o = MySession["_CONTEXTQUEUE_ENABLED"] as bool?;
                return o ?? MyCurrentUser.CheckPermission(BasicUserPermissions.ContextQueue); //Default to true.                
            }
            set
            {
                if (MyCurrentUser.CheckPermission(BasicUserPermissions.ContextQueue))
                {
                    MySession["_CONTEXTQUEUE_ENABLED"] = value;
                    //_ContextQueueProcess.IsEnabled = value; //For dispatch timer
                    _ContextQueueProcess.Enabled = value;
                }
                else
                    _ContextQueueProcess.Enabled = false;
            }
        }
        void _ContextQueue_Tick(object sender, EventArgs e)
        {
            _ContextQueueProcess.Stop(); 
            Models.ContextActionQueue.ProcessQueueBatch();
            multiSelectProgress.Value =  Models.ContextActionQueue.QueueAllotmentFilled;  
            if (_QueueEnabled)//Shouldn't be called while disabled actually...
                _ContextQueueProcess.Start();
        }
        #endregion
    }
}
