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
    public partial class AggBarChartDisplay : SessionWindow
    {        
        PreAggChartViewModel pcdm;
        public AggBarChartDisplay(string callerProc, DataRowView r, string subtitle = "")
        {
            InitializeComponent();
            if(r == null)
            {
                CanShowWindow = false;
                return;
            }
            DisplayChart.ChartTitle = FriendifyProcedureName(callerProc);
            DisplayChart.ChartSubTitle = subtitle;
            pcdm = new PreAggChartViewModel(r);            
            DataContext = pcdm;
        }        
    }
   
}
