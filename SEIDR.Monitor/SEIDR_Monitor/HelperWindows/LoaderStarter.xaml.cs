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
using System.ServiceProcess;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for LoaderStarter.xaml
    /// </summary>
    public partial class LoaderStarter : Window
    {

        int ThreadCount = 4;
        public LoaderStarter()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            /*
            try
            {
                int x = Convert.ToInt32(ThreadCounter.Text.Trim());
                ThreadCount = x;
            }
            catch
            {
                ThreadCounter.Text = ThreadCount.ToString();
            }*/
        }

        private void ThreadCounter_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if(ThreadCounter.Value.HasValue)
            {
                ThreadCount = (int)ThreadCounter.Value.Value;
            }
        }
    }
}
