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
using SEIDR.Dynamics.Windows;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for GetNumeric.xaml
    /// </summary>
    public partial class GetNumeric : Window
    {
        public int value;
        bool AllowNegative = true;
        public GetNumeric(bool acceptNegative = false)
        {
            InitializeComponent();
            AllowNegative = acceptNegative;
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int temp = Convert.ToInt32(Numeric.Text);
                if (!AllowNegative && temp < 0)
                {
                    Numeric.Text = value.ToString();
                    Alert noNegative = new Alert("You cannot use a negative value.", Choice: false);
                    noNegative.ShowDialog();
                }
                else
                {
                    value = temp;
                }
            }
            catch
            {
                Numeric.Text = value.ToString();
                Alert a = new Alert("Try assigning a non numeric value.\r\nThis must be an integer value.", true, false);
                a.ShowDialog();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
