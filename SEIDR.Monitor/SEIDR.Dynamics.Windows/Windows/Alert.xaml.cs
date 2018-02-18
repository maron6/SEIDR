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

namespace SEIDR.Dynamics.Windows
{
    public enum AlertMode
    {        
        Confirmation,
        Alert,
        Message
    }
    /// <summary>
    /// Interaction logic for Alert.xaml
    /// </summary>
    public partial class Alert : BasicSessionWindow //Window
    {        

        /// <summary>
        /// Creates an alert to show the user
        /// </summary>
        /// <param name="Message">Description of alert/error/confirmation</param>
        /// <param name="UserActionPending">If true, prepends with letting user know they're about to do something</param>
        /// <param name="Choice">If true, enables cancel button and indicates you want to know whether the user wants to go forward</param>
        /// <param name="mode">Determines the display theme of the message.</param>
        // <param name="mode">Friendly message, changes text color and to 'Confirmation' instead of 'ALERT!' as the header</param>
        public Alert(string Message, bool UserActionPending = false, bool Choice = true, AlertMode mode = AlertMode.Alert)
            :base(true)
        {
            InitializeComponent();
            Warning.Text = Message;
            this.Title = mode.ToString();
            //this.Title = "ALERT";
            
            if (mode == AlertMode.Confirmation)
            {
                AlertLabel.Text = "Confirmation:";
                AlertLabel.Foreground = (Brush)new BrushConverter().ConvertFromString("Green");
                //this.Title = "CONFIRMATION";
            }
            else if(mode == AlertMode.Message)
            {
                AlertLabel.Visibility = Visibility.Collapsed;
                Choice = false;
                UserActionPending = false;
            }


            if (!UserActionPending)
            {
                UserWarning.Text = "";
                UserWarning.Visibility = Visibility.Collapsed;
            }
            if (!Choice)
            {
                Abort.Visibility = Visibility.Hidden;
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
