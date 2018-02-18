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
    public partial class FrequencyDonutChartDisplay : SessionWindow
    {
        public FrequencyDonutChartDisplay(string callerProc, string userProcDescription, Dictionary<string, ObservableCollection<ChartDisplayData>> source)
        {
            InitializeComponent();
            if (source == null || source.Keys.Count == 0)
            {
                Handle("No source data to be aggregated.");
                CanShowWindow = false;
                return;
            }
            DisplayChart.ChartTitle = FriendifyProcedureName(callerProc); //Friendify the procedure name?
            DisplayChart.ChartSubTitle = userProcDescription; //Generic description of what the page is?            
            foreach (var kv in source)
            {
                FrequencyChartDisplayModel m = new FrequencyChartDisplayModel(kv.Key, kv.Value);
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
    }
    class FrequencyChartDisplayModel
    {
        public string Category { get; private set; }
        public ObservableCollection<ChartDisplayData> dataCollection { get; private set; }
        public FrequencyChartDisplayModel(string Category, ObservableCollection<ChartDisplayData> data)
        {
            dataCollection = data;
            this.Category = Category;
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
