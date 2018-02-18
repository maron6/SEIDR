using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for LabelBox.xaml
    /// </summary>
    public partial class LabelBox : UserControl
    {
        bool _Warn = false;
        public bool WarnEmpty
        {
            get { return _Warn; }
            set
            {
                _Warn = value;
                if (!value)
                    ContentBox.ClearValue(TextBox.BorderBrushProperty);
                else if (textValue.Trim() == "")
                    Warn();
            }
        }
        private void Warn()
        {
            ContentBox.BorderBrush = Brushes.Red;
        }

        public LabelBox()
        {
            InitializeComponent();
            this.DataContext = this;
        }
        string labelValue = "";
        string textValue = "";
        public string Label
        {
            get { return labelValue; }
            set { labelValue = value; MyLabel.Content = value; }
        }
        public string Text
        {
            get { return textValue; }
            set 
            { 
                textValue = value; ContentBox.Text = value;
                IsEmpty = value.nTrim() == "";
                if (_Warn) 
                {                    
                    if (value.nTrim() == "")
                    {
                        Warn();                        
                    }
                    else
                    {
                        ContentBox.ClearValue(TextBox.BorderBrushProperty);
                    }
                }
            }
        }
        /// <summary>
        /// Returns true if the text content is white space or empty
        /// </summary>
        public bool IsEmpty { get; private set; }
        public event EventHandler<TextChangedEventArgs> TextChanged;        
        private void ContentBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Text = ContentBox.Text;
            TextChanged?.Invoke(this, e);
        }
    }
}
