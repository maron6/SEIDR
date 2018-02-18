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
using System.Windows.Navigation;
using System.Windows.Shapes;
using SEIDR.Dynamics.Windows;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for EditDictionaryControl.xaml
    /// </summary>
    public partial class EditDictionaryControl : UserControl
    {
        const int cb_string = 0;
        const int cb_Int = 1;
        const int cb_Decimal=2;
        const int cb_Date=3;



        public Dictionary<string, object> MyValues;
        public EditDictionaryControl()
        {
            InitializeComponent();
            MyValues = new Dictionary<string, object>();
        }
        public void SetSource(Dictionary<string, object> Initialize)
        {
            MyDictionaryRepresentation.Items.Clear();
            MyValues = new Dictionary<string, object>(Initialize);
            foreach (var kv in Initialize)
            {
                MyDictionaryRepresentation.Items.Add(kv.Key + ":" + kv.Value.ToString());
            }
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            try {
                if (this.MyName.Text.Contains(":"))
                    throw new Exception("Parameter Name cannot contain ':'");
                string myName = MyName.Text.Trim();
                if (myName[0] == '@')
                    myName = myName.Substring(myName.LastIndexOf('@')+1);
                string value = myName + ":" + this.Value.Text;
                if (MyDictionaryRepresentation.Items.Contains(value))
                    throw new Exception("Value has already been added.");
                switch (DataType.SelectedIndex)
                {
                    case cb_string:
                        {
                            MyValues.Add(myName, this.Value.Text);
                            break;
                        }
                    case cb_Int:
                        {
                            MyValues.Add(myName, Convert.ToInt32(this.Value.Text.Trim()));
                            break;
                        }
                    case cb_Date:
                        {
                            MyValues.Add(myName, DateTime.Parse(this.Value.Text));
                            break;
                        }
                    case cb_Decimal:
                        {
                            MyValues.Add(myName, Convert.ToDecimal(this.Value.Text.Trim()));
                            break;
                        }
                }                
                MyDictionaryRepresentation.Items.Add(value);
                this.MyName.Text = "";
                this.Value.Text = "";
            }
            catch(Exception ex)
            {
                new Alert(ex.Message, false, false).ShowDialog();                
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            string x = MyDictionaryRepresentation.SelectedItem as string;
            if (x == null)
                return;
            MyValues.Remove(x.Split(':')[0]);
            MyDictionaryRepresentation.Items.Remove(x);
        }

    }
}
