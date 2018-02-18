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
using SEIDR;

namespace SEIDR.Dynamics.Configurations.DynamicEditor
{
    /// <summary>
    /// Interaction logic for FlagPicker.xaml
    /// </summary>
    public partial class FlagPicker : iNotifyControl
    {
        Enum flag;
        public Enum FlagValue { get { return flag; } set { flag = value; InvokeChanged(); } }
        string typeName = nameof(Enum);
        public string TypeName { get { return typeName; } set { typeName = value; InvokeChanged(); } }
        List<FlagObject> sourceData { get; set; }
        bool reqOptions = false;
        public bool RequireOptions
        {
            get { return reqOptions; }
            set { reqOptions = value;
                InvokeChanged();
                InvokeChanged(nameof(HasOptions));
            }
        }
        public Visibility HasOptions
        {
            get
            {
                if (sourceData.HasMinimumCount(1) || !reqOptions)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
            set { InvokeChanged(); } }
        public FlagPicker(Enum Flag)
            :this()
        {            
            Configure(Flag, 0);
        }        
        public FlagPicker()
        {
            sourceData = new List<FlagObject>();
            InitializeComponent();
            DataContext = this;
            FlagList.DataContext = sourceData;
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox s = sender as CheckBox;
            if (s == null)
                return;
            dynamic t = (Enum)s.Tag; //Items source data template: Tag -> FlagObject.value
            if (s.IsChecked ?? false)
            {
                FlagValue &= ~t;
            }
            else
            {                
                FlagValue |= t;
            }
            FlagChanged.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }

        #region configurations
        public void Configure<e>(e Flag, e filter)
        {
            var t = typeof(e);
            if (!t.IsEnum)
                throw new InvalidOperationException("'" + t.Name + "' is not an Enum");
            basicConfigure(Flag as Enum);
            foreach(Enum f in Enum.GetValues(t))
            {
                if (f.Equals(0))
                    continue;
                if (!filter.Equals(0) && f.HasFlag(filter as Enum))
                    continue;
                sourceData.Add(new FlagObject(f, Flag as Enum));
            }
            InvokeChanged(nameof(sourceData));
            InvokeChanged(nameof(HasOptions));
        }
        public void Configure(Enum Flag, int Filter = 0)
        {
            var t = basicConfigure(Flag);
            foreach (Enum f in Enum.GetValues(t))
            {
                if ((Filter & Convert.ToInt32(f)) != 0)
                    continue;                
                sourceData.Add(new FlagObject(f, Flag));
            }
            InvokeChanged(nameof(sourceData));
            InvokeChanged(nameof(HasOptions));
        }    
        public void Configure(Enum Flag, long filter = 0)
        {
            var t = basicConfigure(Flag);
            foreach (Enum f in Enum.GetValues(t))
            {
                if ((filter & Convert.ToInt64(f)) != 0)
                    continue;
                sourceData.Add(new FlagObject(f, Flag));
            }
            InvokeChanged(nameof(sourceData));
            InvokeChanged(nameof(HasOptions));
        }           
        public void Configure(Enum Flag, short Filter = 0)
        {
            var t = basicConfigure(Flag);
            foreach (Enum f in Enum.GetValues(t))
            {
                if ((Filter & Convert.ToInt16(f)) != 0)
                    continue;
                sourceData.Add(new FlagObject(f, Flag));
            }
            InvokeChanged(nameof(sourceData));
            InvokeChanged(nameof(HasOptions));
        }
        Type basicConfigure(Enum flag)
        {
            if (flag == null)
                throw new ArgumentNullException(nameof(flag));
            Type t = flag.GetType();            
            if (!t.GetCustomAttributes(typeof(FlagsAttribute), false).HasMinimumCount(1))
                throw new ArgumentException("'" + t.Name + "' is not a flag", nameof(flag));
            this.flag = flag; //use the field here so that initial set up doesn't mess with things?
            TypeName = Windows.EditableObjectHelper.FriendifyLabel(t.Name);
            sourceData.Clear();
            return t;
        }
        #endregion
        public event EventHandler FlagChanged;
    }
    public class FlagObject
    {
        public string Key { get; set; }
        public Enum value { get; set; }
        public bool Checked { get; set; }
        public FlagObject(Enum me, Enum init)
        {
            Key = me.GetDescription();
            value = me;
            Checked = me.HasFlag(init);
        }
    }
}
