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

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for ValidationBox.xaml
    /// </summary>
    public partial class ValidationBox : UserControl
    {
        public ValidationBox(string label, string valueType="STRING")
        {
            InitializeComponent();
            myType = valueType.ToUpper();
            baseValue = null;
            baseDate = null;
            BoxLabel.Content = label;
            if (myType == "DATE")
            {
                DateType.Visibility = System.Windows.Visibility.Visible;
                NonDateType.Visibility = System.Windows.Visibility.Hidden;
                BitType.Visibility = System.Windows.Visibility.Hidden;
            }
            else if (myType == "BOOL")
            {
                BitType.Visibility = System.Windows.Visibility.Visible;
                DateType.Visibility = System.Windows.Visibility.Hidden;
                NonDateType.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                NonDateType.Visibility = System.Windows.Visibility.Visible;
                DateType.Visibility = System.Windows.Visibility.Hidden;
                BitType.Visibility = System.Windows.Visibility.Hidden;
            }
        }
        string baseValue;
        DateTime? baseDate;
        bool? baseBit;
        string myType = "STRING";
        public int? IntValue()
        {
            int x;
            if (int.TryParse(baseValue, out x))
                return x;
            return null;
        }
        public double? DoubleValue()
        {
            double x;
            if (double.TryParse(baseValue, out x))
                return x;
            return null;
        }
        public decimal? DecimalValue()
        {
            Decimal x;
            if (Decimal.TryParse(baseValue, out x))
                return x;
            return null;
        }
        public DateTime? DateValue()
        {
            return baseDate;
        }
        public bool? BoolValue()
        {
            return baseBit;
        }
        public object ValueToSet()
        {
            switch (myType)
            {
                case "INT":
                    return IntValue();
                case "DOUBLE":
                    return DoubleValue();
                case "DECIMAL":
                    return DecimalValue();
                case "DATE":
                    return DateValue();
                case "BOOL":
                    return baseBit;
                default:
                    return baseValue;
            }
        }
        public string MyValue
        {
            get
            {
                switch (myType)
                {
                    case "DATE":
                        {
                            return baseDate.HasValue ? baseDate.Value.ToString("MM/dd/yyyy") : null;
                        }
                    case "INT":
                        {
                            return IntValue().ToString();
                        }
                    case "DOUBLE":
                        {
                            return DoubleValue().ToString();
                        }
                    case "BOOL":
                        {
                            return baseBit.HasValue ? baseBit.Value.ToString() : null;
                        }
                    default:
                        return baseValue;
                }
            }
            set
            {
                switch (myType)
                {
                    case "DATE":
                        {
                            if (value == null)
                            {
                                baseDate = null;
                                DateType.SelectedDate = null;
                                return;
                            }
                            DateTime x;
                            if (DateTime.TryParse(value, new System.Globalization.CultureInfo("EN-US"), System.Globalization.DateTimeStyles.None,  out x))
                            {
                                baseDate = x;
                                DateType.SelectedDate = x;
                            }
                            else
                            {
                                DateType.SelectedDate = baseDate;
                            }
                            break;
                        }
                    case "INT":
                        {
                            if (value == null)
                            {
                                baseValue = null;
                                NonDateType.Text = null;
                                return;
                            }
                            int x;
                            if (int.TryParse(value.Trim(), out x))
                            {
                                baseValue = x.ToString();
                                NonDateType.Text = x.ToString();
                            }
                            else
                            {
                                NonDateType.Text = baseValue;
                            }
                            break;
                        }
                    case "DOUBLE":
                        {
                            if (value == null)
                            {
                                baseValue = null;
                                NonDateType.Text = null;
                                return;
                            }
                            double x;
                            if (double.TryParse(value.Trim(), out x))
                            {
                                baseValue = x.ToString();
                                NonDateType.Text = x.ToString();
                            }
                            else
                            {
                                NonDateType.Text = baseValue;
                            }
                            break;                            
                        }
                    case "DECIMAL":
                        {
                            if (value == null)
                            {
                                baseValue = null;
                                NonDateType.Text = null;
                                return;
                            }
                            decimal x;
                            if (decimal.TryParse(value.Trim(), out x))
                            {
                                baseValue = x.ToString();
                                NonDateType.Text = x.ToString();
                            }
                            else
                            {
                                NonDateType.Text = baseValue;
                            }
                            break;
                        }
                    case "BOOL":
                        {
                            if (value == null)
                            {
                                baseBit = null;
                                BitType.IsChecked = null;
                                return;
                            }
                            bool x;
                            if (bool.TryParse(value.Trim(), out x))
                            {
                                baseBit = x;
                                BitType.IsChecked = x;
                            }
                            else
                            {
                                BitType.IsChecked = baseBit;
                            }
                            break;
                        }
                    default:
                        {
                            baseValue = value;
                            NonDateType.Text = value;
                            break;
                        }

                }
            }
        }

        private void NonDateType_TextChanged(object sender, TextChangedEventArgs e)
        {
            //if (DateType.SelectedDate != baseDate && DateType.SelectedDate.HasValue)
            if(NonDateType.Text != baseValue)
                MyValue = NonDateType.Text;            
        }

        private void DateType_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if(DateType.SelectedDate != baseDate && DateType.SelectedDate.HasValue)
                MyValue = DateType.SelectedDate.Value.ToString("MM/dd/yyyy");
            else if(!DateType.SelectedDate.HasValue)
            {
                MyValue = null;
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (BitType.IsChecked != baseBit && BitType.IsChecked.HasValue)
                MyValue = BitType.IsChecked.ToString();
            else if (!BitType.IsChecked.HasValue)
            {
                MyValue = null;
            }
        }
    }
}
