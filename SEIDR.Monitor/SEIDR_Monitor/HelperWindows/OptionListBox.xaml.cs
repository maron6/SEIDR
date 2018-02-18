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
    /// Interaction logic for OptionListBox.xaml
    /// </summary>
    public partial class OptionListBox : UserControl
    {
        public List<string> MyList;
        public bool NumericOnly = false;
        public OptionListBox()
        {
            InitializeComponent();
            this.DataContext = this;
            MyList = new List<string>();
            this.LocalWidth = this.Width;
        }
        double LocalWidth;
        public double BoxWidth
        {
            get { return LocalWidth; }
            set
            {
                this.Width = value;
                this.LocalWidth = value;
                this.ContentLabelBox.Width = value - 100;
            }
        }
        /// <summary>
        /// Delegate to a function taking a string and returning a boolean indicating whether or not it's valid.
        /// </summary>
        public Func<string, bool> Validate = null;
        /// <summary>
        /// Validation message to display when Validate returns false
        /// </summary>
        public string Validation = null;
        public void SetupList(List<string> toSetup)
        {
            foreach (string s in toSetup)
            {
                MyList.Add(s);
                ContentList.Items.Add(s);
            }
        }
        public string Label
        {
            get { return ContentLabelBox.Label; }
            set { ContentLabelBox.Label = value; }
        }
        public string TextBoxText
        {
            get { return ContentLabelBox.Text; }
            set { ContentLabelBox.Text = value; }
        }
        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            string s = this.ContentLabelBox.Text.Trim();
            int x;
            if (Validate != null && !Validate(s))
            {
                Alert a = new Alert(Validation ?? "Invalid entry", Choice: false);
                a.ShowDialog();
                return;
            }
            if (NumericOnly && !Int32.TryParse(s, out x))
            {
                Alert a = new Alert("You cannot use a non integer", Choice: false);
                a.ShowDialog();
                return;
            }
            MyList.Add(s);
            ContentList.Items.Add(s);
            ContentLabelBox.Text = "";
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (ContentList.SelectedIndex < 0)
                return;
            MyList.Remove(ContentList.SelectedItem as string);
            ContentList.Items.Remove(ContentList.SelectedItem);
        }
    }
}
