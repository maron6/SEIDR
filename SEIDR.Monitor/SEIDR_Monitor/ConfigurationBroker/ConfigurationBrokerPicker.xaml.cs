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

namespace SEIDR.WindowMonitor.ConfigurationBroker
{
    /// <summary>
    /// Interaction logic for ConfigurationBrokerPicker.xaml
    /// </summary>
    public partial class ConfigurationBrokerPicker : Window
    {
        public ConfigurationListBrokerMetaData Picked { get; private set; }        
        public readonly bool Choice = false;
        public ConfigurationBrokerPicker(IEnumerable<ConfigurationListBrokerMetaData> BrokerInfo)
        {
            InitializeComponent();
            if (BrokerInfo.HasMinimumCount(1))
                Choice = true;                
        }
    }
}
