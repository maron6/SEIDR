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
    /// Interaction logic for Alert.xaml
    /// </summary>
    public partial class Alert : Window
    {        
        public Alert(string Message, bool UserActionPending = false, bool Choice = true, bool Confirmation = false)
        {
            InitializeComponent();
            Warning.Text = Message;
            if (!UserActionPending) {
                UserWarning.Text = "";
                UserWarning.Visibility = System.Windows.Visibility.Collapsed;
            }
            if (!Choice)
            {
                Abort.Visibility = System.Windows.Visibility.Hidden;
            }
            if (Confirmation)
            {
                AlertLabel.Text = "Confirmation:";
                AlertLabel.Foreground = (Brush)new BrushConverter().ConvertFromString("Green");
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void Abort_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
