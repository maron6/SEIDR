using SEIDR.Dynamics.Configurations;
using SEIDR.Dynamics.Windows;
//using SEIDR.Extensions;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for QueryAdder.xaml
    /// </summary>
    public partial class QueryEditor : SessionWindow
    {
        public Query result = null;
        bool NewRecord = false;
        Func<bool> CheckNeedRebuild = null;
        public QueryEditor(Query myQuery = null)
        {
            InitializeComponent();
            UseSessionColor = false;
            if (myQuery == null)
            {
                NewRecord = true;
                result = new Query();
            }
            else
                result = SEIDR.Dynamics.Extensions.DClone(myQuery);            
            DBConn.ItemsSource = SettingManager.myConnections.DBConns;
            List<string> categoryList = new List<string>(SettingManager.myQueries.GetCategories());
            categoryList.Insert(0, "");
            Category.ItemsSource = categoryList;            
            SubCategory.IsEnabled = false;
            if (myQuery != null)
            {
                result.Name = myQuery.Name;
                Procedure.Text = myQuery.ProcedureCall;
                //DBConn.Text = myQuery.DBConnectionName;
                DBConn.SelectedIndex = SettingManager.myConnections.GetIndex(myQuery.DBConnectionName); //DBConn.Items.IndexOf(myQuery.DBConnectionName);                
                //Category.Text = myQuery.Category;
                Category.SelectedIndex = categoryList.IndexOf(myQuery.Category);
                if (myQuery.Category != null)
                {
                    SubCategory.Text = myQuery.SubCategory;
                    SubCategory.IsEnabled = true;
                }                
                AutoRefreshTime.Value = myQuery.RefreshTime;
                PieChartEnable.IsChecked = myQuery.EnablePieChart;
                AggChartEnable.IsChecked = myQuery.EnableAggregateChart;
                FrequencyChartEnable.IsChecked = myQuery.EnableFrequencyChart;
                if(myQuery.ExcludedResultColumns != null)
                    excluded.ItemsSource = new List<string>(myQuery.ExcludedResultColumns);
                if(myQuery.GroupedResultColumns != null)
                    grouping.ItemsSource = new List<string>(myQuery.GroupedResultColumns);
                if(myQuery.Parameters != null)                    
                    result.Parameters = new List<QueryParameter>(myQuery.Parameters).ToArray(); //this one actually shouldn't be necessary...due to clone.
                //Queryname.Text = myQuery.Name; //Name is ID name, not user managed anymore.
                //Queryname.IsEnabled = false;
                DisplayName.Text = myQuery.DisplayName;
                CheckNeedRebuild = () =>
                {
                    bool x = false;
                    if (DBConn.SelectedItem.ToString() != myQuery.DBConnectionName
                        || Procedure.Text != myQuery.ProcedureCall)
                    {
                        x = true;
                        needBuildParams = true;
                    }
                    else if (!HaveRebuilt)
                        needBuildParams = false;
                    return x;
                };
            }
            initFinished = true;
            needBuildParams = NewRecord;
            Check_BuildStatus();
        }
        bool initFinished = false;
        bool needBuildParams = false;
        bool HaveRebuilt = false;
        private void Check_BuildStatus()
        {
            if (!initFinished)
                return;
            CheckNeedRebuild?.Invoke();
            bool b = Procedure.Text.ntLength() > 0 && DBConn.SelectedIndex >= 0;
            BuildParams.IsEnabled = b;
            ChartTab.IsEnabled = b;            
            if(b && needBuildParams)
            {
                result.Parameters = null;
            }            
            else if (b)
            {
                OK.IsEnabled = !DisplayName.IsEmpty;
            }
            else
            {
                OK.IsEnabled = false;
                QueryTab.IsSelected = true; //shouldn't really need to worry about because procedure and DBConn only change from the query tab..
            }
        }
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            string nm = ConfigListHelper.GetIDName(ConfigListHelper.Scope.Q);/*
            string nm =this.Queryname.Text.Trim();
            if(nm == ""){
                Alert a = new Alert("Create a query with no Name.\r\nNames are required.", UserActionPending:true, Choice:false);
                a.ShowDialog();
                return;
            }*/
            while(NewRecord && SettingManager.myQueries[nm] != null)
            {
                nm = ConfigListHelper.GetIDName(ConfigListHelper.Scope.Q);
            }
            if (DBConn.SelectedIndex < 0)
            {
                new Alert("Create a query with no Database Connection\r\nA Connection is required", Choice: false, UserActionPending:true).ShowDialog();
                return;
            }
            /*
            if (NewRecord && SettingManager.myQueries[nm] != null)
            {
                Handle($"Query named '{nm}' already exists.", Ryan_UtilityCode.Dynamics.ExceptionLevel.UI_Basic);
                return;
            } */
            string pc = Procedure.Text.nTrim(true);
            string dn = DisplayName.Text.nTrim(true);
            string DBC = DBConn.SelectedItem.ToString();
            List<string> groupingCols = new List<string>();
            List<string> excludedCols = new List<string>();
            foreach(object o in grouping.Items)
            {
                if (o as string == null)
                    continue;
                groupingCols.Add(o as string);
            }
            foreach(object o in excluded.Items)
            {
                if (o as string == null)
                    continue;
                excludedCols.Add(o as string);
            }
            string cat = Category.Text.nTrim(true);
            string sCat = SubCategory.Text.nTrim(true);

            if(NewRecord)
                result.Name = nm; //Else name is already set
            result.DisplayName = dn;
            result.Category = cat.nString();
            result.ProcedureCall = pc.nString();
            result.DBConnectionName = DBC;
            result.EnablePieChart = PieChartEnable.IsChecked ?? false;
            result.EnableAggregateChart = AggChartEnable.IsChecked ?? false;
            result.EnableFrequencyChart = FrequencyChartEnable.IsChecked ?? false;
            result.GroupedResultColumns = groupingCols.ToArray();
            result.ExcludedResultColumns = excludedCols.ToArray();
            result.RefreshTime = (short?)AutoRefreshTime.Value;            


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
            else Check_BuildStatus();
        }        
        /*
        private void Queryname_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Queryname.Text.ToUpper().StartsWith("Q_"))
                Queryname.Text = "Q_" + Queryname.Text;
            else Check_BuildStatus();
        }*/

        private void DBConn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Check_BuildStatus();
            if (DBConn.SelectedIndex < 0)
                return;
            DBConnection db = DBConn.SelectedItem as DBConnection;
            if (db != null)
            {
                DBConn.ToolTip = db.Description;
                if (db.Color != "Default")
                    DBConnLabel.Background = (Brush)new BrushConverter().ConvertFromString(db.Color); //Maybe disable this...? might affect all of the items..
                else
                    DBConnLabel.ClearValue(BackgroundProperty);
                if (db.TextColor.ToUpper() != "DEFAULT")
                    DBConnLabel.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(db.TextColor);
                else
                    DBConnLabel.ClearValue(ForegroundProperty);
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
        
        private void BuildParams_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var work = SettingManager.myConnections[DBConn.SelectedItem.ToString()].InternalDBConn;
                using (SqlConnection c = new SqlConnection(work.ConnectionString))
                {
                    c.Open();
                    SqlCommand cmd = new SqlCommand(Procedure.Text, c) { CommandType = System.Data.CommandType.StoredProcedure };
                    SqlCommandBuilder.DeriveParameters(cmd);
                    SqlParameter[] temp = new SqlParameter[cmd.Parameters.Count];
                    int idx = 0;
                    foreach(SqlParameter param in cmd.Parameters)
                    {                        
                        temp[idx++] = param;
                    }
                    var paramList = QueryParameter.ConvertParameters(temp);
                    if (paramList.Length == 0)
                    {
                        result.Parameters = null;
                        needBuildParams = false;
                        OK.IsEnabled = true;
                        if (CheckNeedRebuild?.Invoke() ?? false)
                            HaveRebuilt = true; //Does nothing if we aren't using CheckNeedRebuild, so coalesce to false
                        string dbc = DBConn.SelectedItem.ToString();
                        string sproc = Procedure.Text; 
                        //Update the function using the current information as a closure.
                        CheckNeedRebuild = () =>
                        {
                            bool tempRebuild = false;
                            if (DBConn.SelectedItem.ToString() != dbc
                                || Procedure.Text != sproc)
                            {
                                tempRebuild = true;
                                needBuildParams = true;
                            }
                            else if (!HaveRebuilt)
                                needBuildParams = false;
                            return tempRebuild;
                        };
                    }
                    else
                    {
                        if(result.Parameters != null)
                        {
                            foreach( var p in paramList)
                            {
                                p.ParameterValue = (from op in result.Parameters
                                         where op.ParameterName == p.ParameterName
                                         && op.ParameterType == p.ParameterType
                                         select op.ParameterValue).FirstOrDefault() 
                                         ?? DBNull.Value; //Default being null/DBNull.Value ...                                
                            }
                        }
                        int x = EditableDashboardDisplay.ColCount;
                        EditableDashboardDisplay.ColCount = 2;
                        EditableDashboardDisplay edd = new EditableDashboardDisplay(QueryParameter.ConvertList(paramList, false), 
                            "Edit Query Parameters");
                        EditableDashboardDisplay.ColCount = x;
                        var res = edd.ShowDialog() ?? false;
                        if (res)
                        {
                            QueryParameter.MapDataRow(edd.myDataRowView.Row, paramList);
                            this.result.Parameters = paramList;
                            needBuildParams = false;
                            OK.IsEnabled = true;
                            if (CheckNeedRebuild?.Invoke() ?? false)
                                HaveRebuilt = true; //Does nothing if we aren't using CheckNeedRebuild, so coalesce to false
                            string dbc = DBConn.SelectedItem.ToString();
                            string sproc = Procedure.Text;
                            CheckNeedRebuild = () =>
                            {
                                bool tempRebuild = false;
                                if (DBConn.SelectedItem.ToString() != dbc
                                    || Procedure.Text != sproc)
                                {
                                    tempRebuild = true;
                                    needBuildParams = true;
                                }
                                else if (!HaveRebuilt)
                                    needBuildParams = false;
                                return tempRebuild;
                            };
                        }
                    }
                    cmd.Dispose();
                    c.Close();
                    c.Dispose();
                }
            }
            catch(Exception ex)
            {
                Handle(ex, "Unable to Build parameters: " + ex.Message);
                return;
            }
        }

        private void AddExclude_Click(object sender, RoutedEventArgs e)
        {
            string s = ColumnName.Text.nTrim();
            if (s == null)
                return;            
            excluded.Items.Add(s);
            ColumnName.Text = string.Empty;
        }

        private void RemoveExclude_Click(object sender, RoutedEventArgs e)
        {
            string s = excluded.SelectedItem as string;
            if (s == null)
                return;            
            excluded.Items.Remove(s);
            if (ColumnName.Text.nTrim() == null)
                ColumnName.Text = s;
        }

        private void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            string s = ColumnName.Text.nTrim();
            if (s == null)
                return;
            grouping.Items.Add(s);
            ColumnName.Text = string.Empty;
        }

        private void RemoveGroup_Click(object sender, RoutedEventArgs e)
        {
            string s = grouping.SelectedItem as string;
            if (s == null)
                return;
            grouping.Items.Remove(s);
            if (ColumnName.Text.nTrim() == null)
                ColumnName.Text = s;
        }

        private void Procedure_TextChanged(object sender, TextChangedEventArgs e)
        {
            Check_BuildStatus();
        }
        
    }
}
