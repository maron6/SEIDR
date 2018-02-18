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
using De.TorstenMandelkow.MetroChart;
using System.Data;
using System.Collections.ObjectModel;
using static SEIDR.WindowMonitor.Dashboards.ChartHelper;

namespace SEIDR.WindowMonitor.Dashboards
{
    /// <summary>
    /// Interaction logic for ChartDisplay.xaml
    /// </summary>
    public partial class StackedBarChartDisplay : SessionWindow
    {
        public StackedBarChartDisplay(string callerProc, string userProcDescription, Dictionary<string, ObservableCollection<ChartDisplayData>> source)            
        {
            InitializeComponent();
            if (source == null || source.Keys.Count == 0)
            {
                Handle("No source data to be aggregated.");
                CanShowWindow = false;
                return;
            }
            DisplayChart.ChartTitle = FriendifyProcedureName(callerProc); //Friendify the procedure name?
            DisplayChart.ChartSubTitle = userProcDescription + " - Chart displaying numeric aggregates of columns"; //Generic description of what the page is?            
            foreach (var kv in source)
            {
                //if (kv.Key == null || kv.Value == null)
                //    continue;
                StackedBarChartDisplayModel m = new StackedBarChartDisplayModel(kv.Key, kv.Value);
                DisplayChart.Series.Add(new ChartSeries
                {
                    SeriesTitle = kv.Key,
                    DisplayMember = "Description",
                    ValueMember = "value",
                    ItemsSource = m.dataCollection,                    
                    DataContext = m
                });
            }
        }
        public StackedBarChartDisplay(string callerProc, string userProcDescription, string CategoryColumn, DataRowView[] r)
        {
            InitializeComponent();
            string Message = null;

            if (r == null || r.Length == 0)            
                Message = ("Empty Row set");             
            else if( CategoryColumn == null)
                Message = ("Column Name not provided");             
            else if (!r[0].DataView.Table.Columns.Contains(CategoryColumn))            
                Message = ("Category column not included in table.");                            
            if(Message != null)
            {
                Handle(Message);
                CanShowWindow = false;
                return;
            }
            DisplayChart.ChartTitle = FriendifyProcedureName(callerProc); //Friendify the procedure name?
            DisplayChart.ChartSubTitle = userProcDescription + " - Chart displaying numeric information for a given category"; //Generic description of what the page is?
            var mList = StackedBarChartDisplayModel.GetModels(CategoryColumn, r);
            foreach(var m in mList)
            {
                DisplayChart.Series.Add(new ChartSeries
                {
                    DisplayMember = "Description",
                    ValueMember = "value",
                    ItemsSource = m.dataCollection,
                    DataContext = m
                });
            }

        }    
    }
    class StackedBarChartDisplayModel
    {
        public string Category { get; private set; }
        public ObservableCollection<ChartDisplayData> dataCollection { get; private set; }
        public StackedBarChartDisplayModel(string Category, ObservableCollection<ChartDisplayData> data)
        {
            dataCollection = data;
            this.Category = Category;
        }
        public StackedBarChartDisplayModel(string CategoryCol, DataRow r)
        {
            Category = r[CategoryCol].ToString();
            dataCollection = new ObservableCollection<ChartDisplayData>();
            
            foreach (DataColumn c in r.Table.Columns)
            {
                if (c.ColumnName.ToUpper().StartsWith("HDN"))
                    continue;
                if (CanUseColumn(c.DataType))
                {
                    string FriendlyName = c.ColumnName;
                    if (FriendlyName.ToUpper().StartsWith("DTL_"))
                        FriendlyName = FriendlyName.Substring(4);
                    FriendlyName = GetFriendlyName(FriendlyName);
                    if (c.AllowDBNull && r[c.ColumnName] == DBNull.Value)
                        r[c.ColumnName] = 0;
                    dataCollection.Add(new ChartDisplayData() { Description = FriendlyName, value = Convert.ToInt32(r[c.ColumnName]) });
                }                

            }
        }        
        
        public static StackedBarChartDisplayModel[] GetModels(string CategoryColumn, DataRowView[] rList)
        {            
            var rList2 = GroupAndAgg(rList, CategoryColumn);
            var ModelList = new StackedBarChartDisplayModel[rList2.Length];
            for(int i= 0; i < ModelList.Length; i++)
            {
                ModelList[i] = new StackedBarChartDisplayModel(CategoryColumn, rList2[i]);
            }
            return ModelList;
        }
        private static DataRow[] GroupAndAgg(DataRowView[] rList, string Column)
        {
            var r = (from row in rList
                     select row[Column].ToString()).Distinct().ToArray();
            var rv = new DataRow[r.Length];
            var cols = rList[0].DataView.Table.Columns;
            Dictionary<string, int> map = new Dictionary<string, int>();
            for(int i= 0; i < r.Length; i++)
            {
                rv[i] = rList[0].Row.Table.NewRow();
                map[r[i]] = i;
                foreach(DataColumn c in cols)
                {
                    if (CanUseColumn(c.DataType))
                    {
                        rv[i][c.ColumnName] = 0;
                    }
                }
            }            
            for(int i = 0; i < rList.Length; i++)
            {
                foreach(DataColumn c in cols)
                {
                    if (CanUseColumn(c.DataType))
                    {
                        int val = 0;
                        if(!c.AllowDBNull && rList[i][c.ColumnName] != DBNull.Value)
                        {
                            val = Convert.ToInt32(rList[i][c.ColumnName]);
                        }
                        rv[map[r[i]]][c.ColumnName] = Convert.ToInt32(rv[map[r[i]]][c.ColumnName]) + val;
                    }
                }
            }
            return rv;
        }
        private object selectedItem = null;
        public object SelectedItem
        {
            get
            {
                return selectedItem;
            }
            set
            {
                // selected item has changed
                selectedItem = value;
            }
        }
    }
}
