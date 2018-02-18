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
using System.Data.SqlClient;
using System.Data;
using SEIDR.Dynamics.Configurations;
//using SEIDR.Processing.Data.DBObjects;
using SEIDR.DataBase;

namespace SEIDR.Dynamics.Windows
{
    /// <summary>
    /// Interaction logic for GridDashboardWindow.xaml
    /// </summary>
    public partial class CRUDWindow : BasicSessionWindow //Window
    {
        SqlCommand cmd;
        SqlCommand createInfo;
        string _Insert;
        string _Update;
        string _delete;
        DatabaseConnection db;        
        /// <summary>
        /// Sets up a CRUD window for editing a table..
        /// </summary>
        /// <param name="connection">Database connection to run procedures on</param>
        /// <param name="PageName">Title for window.</param>
        /// <param name="ListProc">Lists the records available to edit. Should already have any parameters set.</param>
        /// <param name="CreateInfoProc">Procedure name that should give detail information about creating or editing a record, including what columns should not be editable and any lookups</param>
        /// <param name="InsertProc">Procedure for performing the insert from EditableDashboardDisplay</param>
        /// <param name="UpdateProc">Procedure for performing the update from Editable DashboardDisplay</param>
        /// <param name="DeleteProc">Procedure for deleting or logically deleting a selected record. Should have a return value of 0 for success, else failure.</param>
        /// <param name="CACHE_KEY">For use with caching if mainly used for looking at details and inserts/updates are rare</param>
        public CRUDWindow(DatabaseConnection connection, string PageName, SqlCommand ListProc, 
            string CreateInfoProc, string InsertProc, string UpdateProc, string DeleteProc)
        {
            InitializeComponent();
            DataContext = this;
            UseSessionColor = false;
            //d = new Dashboard(sourceCommand, connection);
            //current = d.RefreshList();
            cmd = ListProc;
            
            db = connection;            
            Title = PageName;
            //SetupPage();
            createInfo = new SqlCommand(CreateInfoProc) { CommandType = CommandType.StoredProcedure };

            _Insert = InsertProc;
            _Update = UpdateProc;
            _delete = DeleteProc;
            if (_Insert == null)
                Create.Visibility = Visibility.Collapsed;
            if (_Update == null)
                Edit.Visibility = Visibility.Collapsed;
            if (_delete == null)
                Delete.Visibility = Visibility.Collapsed;
            Setup();
        }
        /// <summary>
        /// Sets up a CRUD window for editing a table..
        /// </summary>
        /// <param name="connection">Database connection to run procedures on</param>
        /// <param name="PageName">Title for window.</param>
        /// <param name="ListProc">Lists the records available to edit. Should already have any parameters set.</param>
        /// <param name="CreateInfoProc">Procedure name that should give detail information about creating or editing a record, including what columns should not be editable and any lookups</param>
        /// <param name="InsertProc">Procedure for performing the insert from EditableDashboardDisplay</param>
        /// <param name="UpdateProc">Procedure for performing the update from Editable DashboardDisplay</param>
        /// <param name="DeleteProc">Procedure for deleting or logically deleting a selected record. Should have a return value of 0 for success, else failure.</param>
        /// <param name="CACHE_KEY">For use with caching if mainly used for looking at details and inserts/updates are rare</param>
        public CRUDWindow(DatabaseConnection connection, string PageName, SqlCommand ListProc,
            string CreateInfoProc, string InsertProc, string UpdateProc, string DeleteProc, string CACHE_KEY)
            :this(connection, PageName, ListProc, CreateInfoProc, InsertProc, UpdateProc, DeleteProc)
        {
            SESSION_KEY = CACHE_KEY;            
        }


        public void AddMenuItem(MenuItem m)
        {
            DashboardData.ContextMenu.Items.Add(m);
        }
        public void Nest(string PageName, string ListProc,
            string CreateInfoProc, string InsertProc, string UpdateProc, string DeleteProc)
        {

            Action a = () =>
            {
                DataRowView rv = DashboardData.SelectedItem as DataRowView;
                if (rv == null)
                    return;
                try
                {
                    using (SqlConnection c = new SqlConnection(db.ConnectionString))
                    {
                        c.Open();
                        SqlCommand list = new SqlCommand(ListProc) { CommandType = CommandType.StoredProcedure, Connection = c };
                        SqlCommandBuilder.DeriveParameters(list);
                        c.Close();
                        var cl = rv.DataView.Table.Columns;
                        foreach (SqlParameter sqp in list.Parameters)
                        {
                            string n = sqp.ParameterName.Replace("@", "");
                            if (cl.Contains(n))
                            {
                                sqp.Value = rv[n];
                            }
                        }
                        new CRUDWindow(db, PageName, list, CreateInfoProc, InsertProc, UpdateProc, DeleteProc).ShowDialog();
                    }
                }
                catch(Exception e)
                {
                    new Alert(e.Message, Choice: false).ShowDialog();
                }
            };
            MenuItem m = MenuItemBuilder.BuildInitial(PageName, a);
            DashboardData.ContextMenu.Items.Add(m);
        }
        ~CRUDWindow()
        {            
            if(cmd!= null)
                cmd.Dispose();
            if (createInfo != null)
                createInfo.Dispose();
        }
        readonly string SESSION_KEY;
        public void Setup()
        {
            colorCol = null;

            DataTable dt = SessionManager[SESSION_KEY] as DataTable;
            if (dt == null)
            {
                //dt = db.RunQuery(cmd);
                dt = db.RunCommand(cmd).GetFirstTableOrNull();
            }
            //DashboardData.ItemsSource = dt.AsDataView();
            SetupPaging(dt);
        }


        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            SessionManager[SESSION_KEY] = null;
            Setup();            
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        void RefreshClick_Click(object sender, RoutedEventArgs e)
        {
            SessionManager[SESSION_KEY] = null;
            Setup();
        }
        private void SetupContextMenus()
        {
            
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

            w.Items.Add(m);            
            */

            DashboardData.ContextMenu = w;
            //Use context menuitems to create the ContextMenus            
            this.Cursor = Cursors.Arrow;
        }
        private void CopyClick(object sender, EventArgs e)
        {
            var v = DashboardData.ItemsSource as DataView;
            if (v == null ||DashboardData.CurrentCell == null)// || DashboardData.SelectedIndex < 0 || DashboardData.SelectedCells.Count == 0)
                return;
            Clipboard.SetText(DashboardData.CurrentCell.Item.ToString());
            return;/*
            string h = DashboardData.SelectedCells[0].Column.Header.ToString();
            if (v.Table.Columns.Contains(h))
            {
                string data = v.Table.Rows[DashboardData.SelectedIndex][h].ToString();
                Clipboard.SetText(data);
            }*/
        }        

        

        string colorCol = null; //instead of a boolean, if there's a change to use a different requirement for color. E.g. ends with
        private void DashboardData_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {            
            if(e.PropertyName.ToUpper() == "COLOR")
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
                var color = row[colorCol].ToString();

                try
                {
                    e.Row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
                }
                catch { }
            }
        }
        const string INSERT_PARM = "@InsertRecord";
        private void Create_Click(object sender, RoutedEventArgs e)
        {
            
            DataSet d = null;
            try
            {
                using (SqlConnection c = new SqlConnection(db.ConnectionString))
                {
                    c.Open();
                    createInfo.Connection = c;
                    createInfo.Parameters.Clear();
                    SqlCommandBuilder.DeriveParameters(createInfo);
                    c.Close();
                }
                if (createInfo.Parameters.Contains(INSERT_PARM)) //If does not have @Insert, assume that insert and edit are treated the same
                    createInfo.Parameters[INSERT_PARM].Value = true;
                d = db.RunCommand(createInfo);
            }
            catch(Exception ex)
            {
                //new Alert("Failed to set up command for Create record info", Choice: false).ShowDialog();
                Handle(ex, "Failed to set up command for Create record info");
                return;
            }
            
            if (d == null || d.Tables.Count == 0 || d.Tables[0].Rows.Count == 0)
            {
                new Alert("No Creation info available from procedure.", Choice: false).ShowDialog();
                return;
            }
            

            List<ComboDisplay> cd = null;
            if (d.Tables.Count > 1)
                cd = ComboDisplay.Build(d.Tables[1]);
            string[] ExceptionList = null;
            if (d.Tables.Count > 2)
                ExceptionList = EditableDashboardDisplay.SetExceptionList(d.Tables[2]);            

            new EditableDashboardDisplay(d.Tables[0].AsDataView()[0], 
                Title + " - New Record", 
                ExceptionList, 
                cd?.ToArray(), 
                db: db, 
                Accept:_Insert
                ).ShowDialog();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            DataRowView r = DashboardData.SelectedItem as DataRowView;
            if (r == null)
                return;
            DataSet d = null;
            try
            {
                using (SqlConnection c = new SqlConnection(db.ConnectionString))
                {
                    c.Open();
                    createInfo.Connection = c;
                    createInfo.Parameters.Clear();
                    SqlCommandBuilder.DeriveParameters(createInfo);
                    if (createInfo.Parameters.Contains(INSERT_PARM))
                        createInfo.Parameters[INSERT_PARM].Value = false;
                    c.Close();
                }
                foreach (DataColumn c in (DashboardData.ItemsSource as DataView).Table.Columns)
                {
                    if (createInfo.Parameters.Contains("@" + c.ColumnName))
                    {
                        createInfo.Parameters["@" + c.ColumnName].Value = r[c.ColumnName];
                    }
                }
                d = db.RunCommand(createInfo);                                    
            }
            catch(Exception ex)
            {
                Handle(ex, "Unable to set up for editing Record");
                return;
            }

            if (d == null || d.Tables.Count == 0 || d.Tables[0].Rows.Count == 0)
            {
                new Alert("No Creation info available from procedure.", Choice: false).ShowDialog();
                return;
            }


            List<ComboDisplay> cd = null;
            if (d.Tables.Count > 1)
                cd = ComboDisplay.Build(d.Tables[1]);
            string[] ExceptionList = null;
            if (d.Tables.Count > 2)
                ExceptionList = EditableDashboardDisplay.SetExceptionList(d.Tables[2]);

            new EditableDashboardDisplay(d.Tables[0].AsDataView()[0],
                Title + " - Edit Record",
                ExceptionList,
                cd?.ToArray(),
                db: db,
                Accept: _Update
                ).ShowDialog();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            DataRowView vr = DashboardData.SelectedItem as DataRowView;
            if (vr == null)
                return;
            var cols = (from DataColumn col in vr.DataView.Table.Columns
                        select col.ColumnName).ToArray();
            SqlCommand delete = null;
            try
            {
                delete = new SqlCommand(_delete) { CommandType = CommandType.StoredProcedure };
                using (SqlConnection c = new SqlConnection(db.ConnectionString))
                {
                    c.Open();
                    delete.Connection = c;
                    SqlCommandBuilder.DeriveParameters(delete);
                    foreach (var col in cols)
                    {
                        string parmName = "@" + col;
                        if (delete.Parameters.Contains(parmName))
                        {
                            //c.Parameters.AddWithValue(parmName, kv.Value);
                            delete.Parameters[parmName].Value = vr[col];
                        }
                        else if (delete.Parameters.Contains(col))
                        {
                            delete.Parameters[col].Value = vr[col];
                        }
                    }
                    delete.ExecuteNonQuery();
                    c.Close();
                }
            }
            catch(Exception ex)
            {
                new Alert(ex.Message, Choice: false).ShowDialog();
            }
            finally
            {
                if(delete != null)
                    delete.Dispose();
            }
        }
        public int PageSize {
            get { return _pageSize; }
            set {
                if (value < 50)
                    _pageSize = 50;
                else
                    _pageSize = value;
            }
        }
        int _CurrentPage = 0;
        int _pageSize = 200;
        int _PageCount = 0;
        DataTable _SourceData;
        private void SetupPaging(DataTable dt)
        {            
            _PageCount = dt.Rows.Count / _pageSize;
            if (dt.Rows.Count % _pageSize == 0)
                _PageCount--; //Subtract one due to zero based pages and not having leftover records
            PagePicker.Maximum = _PageCount; //1 through count, access pagePicker value - 1
            if (_PageCount > 1)
            {
                PagePicker.Visibility = Visibility.Visible;
                GoToPage.Visibility = Visibility.Visible;
            }
            else
            {
                PagePicker.Visibility = Visibility.Hidden;
                GoToPage.Visibility = Visibility.Hidden;
            }            
            DashboardData.Visibility = Visibility.Visible;

            if (_SourceData != null)
            {
                _SourceData.Clear();
                _SourceData.Dispose();
            }
            _SourceData = dt;
            SetPage(0);
        }
        private void SetPage(int x)
        {
            Cursor = Cursors.Wait;
            DashboardData.ItemsSource = null;
            this.IsEnabled = false;
            try
            {
                _CurrentPage = x;
                PagePicker.Value = x + 1; //Page picking is 1 based
                var d = _SourceData.Rows.Cast<DataRow>();
                if (_CurrentPage == 0)
                    DashboardData.ItemsSource = d.Take(_pageSize).CopyToDataTable().AsDataView();
                else
                    DashboardData.ItemsSource = d.Skip(_CurrentPage * _pageSize).Take(_pageSize).CopyToDataTable().AsDataView();
                /*MonitorData.ItemsSource = _SourceData.Rows.Cast<DataRow>()
                                                .Skip((_CurrentPage - 1) * _pageSize)
                                                .Take(_pageSize)
                                                .CopyToDataTable()
                                                .AsDataView();
                                                */
            }
            catch (Exception ex)
            {
                new Alert("Unable to set page to " + (x + 1) + ": " + ex.Message, Choice: false).ShowDialog();                
                return;
            }
            finally
            {
                this.Cursor = Cursors.Arrow;
                this.IsEnabled = true;
            }
            PageNumber.Content = $"Page # {x + 1}/{_PageCount + 1}";
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

        private void DashboardData_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EditableDashboardDisplay edd = new EditableDashboardDisplay(
                DashboardData.SelectedItem as DataRowView, 
                "CRUD Detail View", 
                readOnlyMode: true);
            edd.ShowDialog();
        }
    }
}
