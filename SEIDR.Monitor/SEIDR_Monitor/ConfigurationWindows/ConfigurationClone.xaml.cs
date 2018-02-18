using SEIDR.Dynamics;
using SEIDR.Dynamics.Configurations;
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

namespace SEIDR.WindowMonitor.ConfigurationWindows
{
    /// <summary>
    /// Interaction logic for ConfigurationClone.xaml
    /// </summary>
    public partial class ConfigurationClone : SessionWindow
    {
        const int TEXT_FOCUSED_HEIGHT = 45;
        public iWindowConfiguration cloned { get; private set; }
        public ConfigurationClone(iWindowConfiguration clone)
            : base(true, MultiUserAccess)
        {
            InitializeComponent();
            //toDo: get the scope clones
            cloned = clone.DClone();
            var props = clone.GetType().GetProperties();
            int rowCount = 0;
            int colCount = 0;
            DataContext = this;
            foreach(var prop in props)
            {
                if (colCount >= 3)
                {
                    rowCount++;
                    colCount = 0;
                    RowDefinition r = new RowDefinition()
                    { Height = new GridLength(TEXT_FOCUSED_HEIGHT) };
                    ContentGrid.RowDefinitions.Add(r);
                }

                var atts = prop.GetCustomAttributes(typeof(LookupSourceAttribute), false)
                    .Where(l => (l as LookupSourceAttribute).ForCloning);
                if (atts.HasMinimumCount(1))
                {
                    bool req = prop.GetCustomAttributes(typeof(CloneLookupSourceRequiredAttribute), true).HasMinimumCount(1);
                    IEnumerable<iWindowConfiguration> lookups = new iWindowConfiguration[0];
                    foreach(LookupSourceAttribute att in atts)
                    {                        
                        lookups = lookups.Union(MyBroker.GetLookup(att.LookupScope));
                    }
                    ConfigurationParentPicker cpp = 
                        new ConfigurationParentPicker(lookups, null, clone.MyScope, req);
                    Action<object, EventArgs> a = (sender, e) =>
                    {
                        ConfigurationParentPicker p = sender as ConfigurationParentPicker;
                        if (p != null)
                        {
                            prop.SetValue(cloned, p.Picked);                            
                        }
                    };
                    cpp.ParentPick_Changed += new EventHandler(a);
                    Grid.SetColumn(cpp, colCount++);                    
                    Grid.SetRow(cpp, rowCount);
                    ContentGrid.Children.Add(cpp);
                }
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
