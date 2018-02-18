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

namespace SEIDR.WindowMonitor.SettingWindows
{
    /// <summary>
    /// Interaction logic for CloneNameConfirm.xaml
    /// </summary>
    public partial class CloneNameConfirm : SessionWindow
    {
        public string cloneName { get; private set; }
        public CloneNameConfirm(string original)
        {
            InitializeComponent();
            UseSessionColor = false;
            Original.Text = original;
            
        }
        
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            cloneName = Clone.Text;
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
