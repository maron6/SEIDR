using SEIDR.Dynamics.Configurations;
using SEIDR.Dynamics.Windows;

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for QueryAdder.xaml
    /// </summary>
    public partial class QueryAdder : SessionWindow
    {
        public Query result = null;
        public QueryAdder(Query myQuery = null)
        {
            InitializeComponent();
            UseSessionColor = false;
            result = myQuery;
            DBConn.ItemsSource = SettingManager.myConnections.DBConns;
            List<string> categoryList = new List<string>(SettingManager.myQueries.GetCategories());
            categoryList.Insert(0, "");
            Category.ItemsSource = categoryList;            
            SubCategory.IsEnabled = false;
            if (myQuery != null)
            {
                Procedure.Text = myQuery.ProcedureCall;
                //DBConn.Text = myQuery.DBConnectionName;
                DBConn.SelectedIndex = SettingManager.myConnections.GetIndex(myQuery.DBConnectionName); //DBConn.Items.IndexOf(myQuery.DBConnectionName);
                /*
                FromDate.Text = myQuery.FromDateParam;
                ThroughDate.Text = myQuery.ThroughDateParam;
                Active.Text = myQuery.ActiveParam;
                ExtraParamName.Text = myQuery.ExtraFilter;
                */
                //Category.Text = myQuery.Category;
                Category.SelectedIndex = categoryList.IndexOf(myQuery.Category);
                if (myQuery.Category != null)
                {
                    SubCategory.Text = myQuery.SubCategory;
                    SubCategory.IsEnabled = true;
                }
                AutoRefreshTime.Value = myQuery.RefreshTime;
                //IntParam1.Text = myQuery.IntParam1;
                Queryname.Text = myQuery.Name;
                Queryname.IsEnabled = false;
                DisplayName.Text = myQuery.DisplayName;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            string nm =this.Queryname.Text.Trim();
            if(nm == ""){
                Alert a = new Alert("Create a query with no Name.\r\nNames are required.", UserActionPending:true, Choice:false);
                a.ShowDialog();
                return;
            }
            if (DBConn.SelectedIndex < 0)
            {
                new Alert("Create a query with no Database Connection\r\nA Connection is required", Choice: false, UserActionPending:true).ShowDialog();
                return;
            }
            string pc = Procedure.Text.nTrim(true);
            string dn = DisplayName.Text.nTrim(true);
            string DBC = DBConn.SelectedItem.ToString();
            string fd = FromDate.Text.nTrim(true);
            string td = ThroughDate.Text.nTrim(true);
            string ap = Active.Text.nTrim(true);
            string ef = ExtraParamName.Text.nTrim(true);
            string i1 = IntParam1.Text.nTrim(true);
            string i2 = IntParam2.Text.nTrim(true);
            string cat = Category.Text.nTrim(true);
            string sCat = SubCategory.Text.nTrim(true);
            result = new Query
            {
                Name = nm,
                DisplayName = dn,
                Category = cat.nString(),
                SubCategory = sCat.nString(),
                ProcedureCall = pc.nString(),                
                DBConnectionName = DBC,/*
                FromDateParam = fd.nString(),
                ThroughDateParam = td.nString(),
                ActiveParam = ap.nString(),
                ExtraFilter = ef.nString(),
                IntParam1 = i1.nString(),
                IntParam2 = i2.nString(),
                */
                RefreshTime = (short?) AutoRefreshTime.Value
            };
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void DBConn_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DBConn.Text.Trim() == "")
                DBConn.Text = "Default";
        }

        private void ExtraParamName_Loaded(object sender, RoutedEventArgs e)
        {

        }


        private void Queryname_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Queryname.Text.ToUpper().StartsWith("Q_"))
                Queryname.Text = "Q_" + Queryname.Text;
        }

        private void DBConn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DBConn.SelectedIndex < 0)
                return;
            DBConnection db = DBConn.SelectedItem as DBConnection;
            if (db != null)
            {
                DBConn.ToolTip = db.Description;
            }
                
        }

        private void Category_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckSubCat();
            tempCat = Category.Text;
        }
        private void CheckSubCat()
        {
            if (string.IsNullOrWhiteSpace(Category.Text))
            {
                SubCategory.IsEnabled = false;
                SubCategory.SelectedValue = -1;
                SubCategory.Text = "";
                return;
            }
            string[] data = SettingManager.myQueries.GetSubCategories(Category.Text.ToString());
            if (data == null)
            {
                SubCategory.IsEnabled = false;
                SubCategory.SelectedIndex = -1;
                SubCategory.Text = "";
                return;
            }
            SubCategory.IsEnabled = true;
            SubCategory.ItemsSource = data;
        }
        string tempCat = null;
        private void Category_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tempCat  != Category.Text)
            {
                CheckSubCat();
                tempCat = Category.Text;
            }
        }
    }
}
