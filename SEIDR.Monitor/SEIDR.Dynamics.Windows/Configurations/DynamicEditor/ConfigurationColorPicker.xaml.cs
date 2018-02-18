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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SEIDR.Dynamics.Configurations.DynamicEditor
{
    /// <summary>
    /// Interaction logic for ConfigurationColorPicker.xaml
    /// </summary>
    public partial class ConfigurationColorPicker : UserControl
    {
        public string myColor { get; set; }
        //public ConfigurationColor myColor { get; private set; } = ConfigurationColor.LightSteelBlue;
        public ConfigurationColorPicker
            (
            //ConfigurationColor initialColor = ConfigurationColor.LightSteelBlue
            string initialColor = "LightSteelBlue"
            )
        {
            InitializeComponent();
            myColor = initialColor;
            //ColorList.ItemsSource = Enum.GetValues(typeof(ConfigurationColor));
            ColorList.ItemsSource = typeof(Colors).GetProperties().Select(p => p.Name); //? Use enum             
            
        }
    }
}
