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
    /// Interaction logic for ContextMapping.xaml
    /// </summary>
    public partial class ContextMapping : SessionWindow
    {
        public List<string> myList;
        public ContextMapping(List<string> starter = null)
        {
            InitializeComponent();
            myList = starter ?? new List<string>();
            if (starter != null)
            {
                ListBox.SetupList(starter);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            myList = ListBox.MyList;
            this.DialogResult = true;
            this.Close();
        }
    }
}
