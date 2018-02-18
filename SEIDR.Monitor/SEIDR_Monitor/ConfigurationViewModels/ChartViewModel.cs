using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.WindowMonitor.ConfigurationViewModels
{
    public class QueryChartModel
    {        
        public string Header { get; set; }
        public string Description { get; set; }
        public string SubTitle { get; set; }        
        public string Column { get; set; }
        public string SubColumn { get; set; }
        public QueryBasicChartType ChartType { get; set; }
    }
    public enum QueryBasicChartType
    {
        PieChart,
        Aggregate,
        FrequencyAgg,
        PreAggregateBar
    }

}
