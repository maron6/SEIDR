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
using Ryan_UtilityCode.Dynamics.Configurations;

namespace DS_LoaderMonitor
{
    /// <summary>
    /// Interaction logic for QueryDropper.xaml
    /// </summary>
    public partial class QueryDropper : Window
    {
        public string ToRemove = null;
        public QueryDropper(Queries qList)
        {
            InitializeComponent();
            foreach (Query q in qList)
            {
                ComboBoxItem cbi = new ComboBoxItem()
                {
                    Name = q.Name
                };
                this.QueryList.Items.Add(cbi);
            }
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            if (QueryList.SelectedItem != null)
            {
                ToRemove = QueryList.SelectedItem.ToString();
                this.DialogResult = true;
                this.Close();
                return;
            }
            Alert a = new Alert("You must choose a query to remove.", Choice: false);
            a.ShowDialog();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
