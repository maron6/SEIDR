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
using MahApps.Metro;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for QueryParameterWindow.xaml
    /// </summary>
    public partial class QueryParameterWindow : SessionWindow// Window
    {
        public MainQueryParamSetup tempSetup;
        /*
        public string ExtraParam = null;
        public DateTime? FromParam = null;
        public DateTime? ThroughParam = null;
        public bool? ActiveParam = null;
         */
        /// <summary>
        /// Names of parameters to populate. If null, should be hidden in the window
        /// </summary>
        /// <param name="FromDate"></param>
        /// <param name="ThroughDate"></param>
        /// <param name="Active"></param>
        public QueryParameterWindow(string FromDate, string ThroughDate, string Active, string extra, string paramInt1, string paramInt2)
        {
            tempSetup = new MainQueryParamSetup();
            InitializeComponent();
            UseSessionColor = false;
            double Height = 180;
            if (FromDate != null)
            {
                this.FromDateLabel.Content = FromDate;
                Height += 15;
                this.FromDate.Visibility = System.Windows.Visibility.Visible;
            }
            if (ThroughDate != null)
            {
                Height += 15;
                this.ThroughDatelabel.Content = ThroughDate;
                this.ThroughDate.Visibility = System.Windows.Visibility.Visible;                
            }
            if (Active != null)
            {
                Height += 15;
                this.ActiveFilterCheck.Content = Active;
                this.ActiveFilter.Visibility = System.Windows.Visibility.Visible;
                tempSetup.ActiveParam = true;
            }
            if(extra != null)
            {
                Height += 15;
                this.ExtraParm.Text = extra;
                this.ExtraParm.Visibility = System.Windows.Visibility.Visible;                
            }
            if(paramInt1 != null){
                Height += 15;
                this.IntParam1.ToolTip = paramInt1;
                IntParam1.Visibility = System.Windows.Visibility.Visible;
            }
            if (paramInt2 != null)
            {
                Height += 15;
                this.IntParam2.ToolTip = paramInt2;
                IntParam2.Visibility = System.Windows.Visibility.Visible;
            }

        }

        private void ThroughDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            tempSetup.ThroughDateParam = ThroughDatePicker.SelectedDate;
            //ThroughParam = ThroughDatePicker.SelectedDate;
        }

        private void FromDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            //FromParam = FromDatePicker.SelectedDate;
            tempSetup.FromDateParam = FromDatePicker.SelectedDate;
        }

        private void ActiveFilterCheck_Checked(object sender, RoutedEventArgs e)
        {
            //ActiveParam = ActiveFilterCheck.IsChecked;
            tempSetup.ActiveParam = ActiveFilterCheck.IsChecked;
        }

        private void Query_Click(object sender, RoutedEventArgs e)
        {
            
            tempSetup.ExtraFilter = ExtraParm.Text;
            tempSetup.IntParam1 = (int)IntParam1.Value;
            tempSetup.IntParam2 = (int)IntParam2.Value;
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            tempSetup = null;
            this.DialogResult = false;
            this.Close();
        }
    }
}
