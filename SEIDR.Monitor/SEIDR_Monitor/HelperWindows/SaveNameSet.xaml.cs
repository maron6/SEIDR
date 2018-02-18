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

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for SaveNameSet.xaml
    /// </summary>
    public partial class SaveNameSet : Window
    {
        public string PackageName;
        public SaveNameSet( string startName)
        {
            InitializeComponent();
            FileName.Text = startName;
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            PackageName = FileName.Text;
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
