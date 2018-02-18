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

namespace SEIDR.Dynamics.Windows
{
    /// <summary>
    /// Interaction logic for SelectorWindow.xaml
    /// </summary>
    public partial class SelectorWindow : BasicSessionWindow
    {
        public object Selection { get;  set; }
        
        public SelectorWindow(string TitleDescription, object[] collection)
            :base(true)
        {
            InitializeComponent();
            if(collection == null || collection.Length == 0)
            {
                CanShowWindow = false;
                return;
            }
            Data.ItemsSource = collection;
            Title = TitleDescription;
            DataContext = this;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Finish();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Finish(false);
        }

        private void Data_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Finish();
            return;
            /*
            ListViewItem tempS = sender as ListViewItem;
            if (tempS != null) {
                Selection = tempS.Content;
                e.Handled = true;
                //if(_Dialog)
                //    DialogResult = true;
                //Close();
                Finish();
            }        
            //*/    
        }
        
        private void Data_Selected(object sender, RoutedEventArgs e)
        {
            ListViewItem tempSelect = sender as ListViewItem;
            if(tempSelect != null)
            {
                Selection = tempSelect.Content;
            }
        }
    }
}
