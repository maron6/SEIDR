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
using System.Data;
using System.Data.SqlClient;
//using SEIDR.Processing.Data.DBObjects;
using System.Reflection;
using System.ComponentModel;
//using SEIDR.Processing;
using MahApps.Metro.Controls;
//using SEIDR.Extensions;
using SEIDR;
using SEIDR.DataBase;
using static SEIDR.Dynamics.Windows.EditableObjectHelper;

namespace SEIDR.Dynamics.Windows
{
    
    /// <summary>
    /// Interaction logic for DashboardDisplay.xaml
    /// </summary>
    public partial class EditableObjectDisplay : Configurations.BasicSessionWindow
    {
        EditableObjectValidator myValidator = null;
        public void SetValidator(EditableObjectValidator validCheck)
        {
            myValidator = validCheck;
        }
        enum PropControlType
        {
            Progress,
            NumPicker,
            DatePicker,
            EnumPicker,
            CheckBox,
            Text,
            Password
        }
        struct PropControlInfo
        {
            public readonly string ElementName;
            public readonly PropControlType ControlType;
            public PropControlInfo(string name, PropControlType type)
            {
                ElementName = name;
                ControlType = type;
            }
        }
        int myColumnCount = 3;
        /// <summary>
        /// The datatable edited on this page.
        /// </summary>
        public object myData;
        public const int DefaultColumnCount = 3;        
        bool ReadOnlyMode = false;
        string myName;        
        private string[] combo_ExcludeNormalList;
        private string[] excludeList;
        /// <summary>
        /// Sets up the page with 'count' columns.
        /// </summary>
        /// <param name="count"></param>
        public void SetColumnCount(int count)
        {
            DashboardData.ColumnDefinitions.Clear();
            for (int i = 0; i < count; i++)
            {
                DashboardData.ColumnDefinitions.Add(new ColumnDefinition());
            }
            myColumnCount = count;
            SetupPage();
        }
        readonly string myDataTypeName;
        /// <summary>
        /// Base constructor. private
        /// </summary>
        /// <param name="Title"></param>
        /// <param name="ExcludeColumns"></param>
        /// <param name="extraChoice"></param>
        /// <param name="readOnlyMode"></param>
        private EditableObjectDisplay(string Title, string[] ExcludeProperties = null, ComboDisplay[] extraChoice = null, bool readOnlyMode =false)
        {

            InitializeComponent();
            if (readOnlyMode)
                Save.Visibility = Visibility.Collapsed; //Technically replace with setting the Rollback variable after inheriting the private constructor
            ReadOnlyMode = readOnlyMode;
            myName = Title;
            this.Title = Title + (readOnlyMode ? " - [READ ONLY MODE]" : "");
            myColumnCount = DashboardData.ColumnDefinitions.Count;
            DataContext = this;
            excludeList = ExcludeProperties;
            if (myName.ToUpper().StartsWith("D_"))
                myName = myName.Substring(2);            
            if(extraChoice != null)
            {
                foreach(var d in extraChoice)
                {
                    object o = null;
                    if (d.SelectedIndex >= 0)
                        o = d.Items[d.SelectedIndex];
                    //AcceptClickParams                    
                    ComboBox cb = d.AsComboBox();                    
                    cb.SelectionChanged += Cb_SelectionChanged;
                    if (ReadOnlyMode)
                        cb.IsEnabled = false;
                    selections.Add(cb);
                }
                combo_ExcludeNormalList = (from res in extraChoice
                                           select res.Name).ToArray();
            }
            else
            {
                combo_ExcludeNormalList = new string[0];
            }
            DashboardName.Text = myName;
            TypeMapping = new Dictionary<string, PropControlInfo>();
            //SetColumnCount(3);
        }
        bool _Rollback = false;
        public bool CanRollback {
            get { return _Rollback && !ReadOnlyMode; }
            set
            {                
                _Rollback = value;
                Save.Visibility = _Rollback && !ReadOnlyMode ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        /// <summary>
        /// Checks if the value can be set on myData and does so if possible
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="value"></param>
        /// <returns>True if the value has been changed, false otherwise</returns>
        private bool CheckNullableAndSet(PropertyInfo prop, object value)
        {
            if (!prop.CanWrite)
                return false;
            Type underlying = Nullable.GetUnderlyingType(prop.PropertyType);
            if(value != null)
            {
                prop.SetValue(myData, Convert.ChangeType(value, prop.PropertyType));
                return true;
            }
            else if(!prop.PropertyType.IsValueType || underlying != null)
            {
                prop.SetValue(myData, Convert.ChangeType(value, prop.PropertyType)); 
                //Converts either to a reference type or the underlying type via use of GetType
                return true;
            }
            return value != null;

        }
        private void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb == null)
                return;
            string name = (string)cb.Tag;
            var i = (cb.SelectedValue as ComboDisplayItem);
            PropertyInfo prop = myData.GetType().GetProperty(name);
            if (i == null || i.value == null)
            {
                CheckNullableAndSet(prop, null);
                if (myValidator != null)
                    Save.IsEnabled = myValidator.CheckValid(myData);
                return;
            }
            else
            {
                CheckNullableAndSet(prop, i.value);
                if (myValidator != null)
                    Save.IsEnabled = myValidator.CheckValid(myData);
            }            

        }

        private List<ComboBox> selections = new List<ComboBox>();
        /// <summary>
        /// Create editable dashboard to modify a dashboard at the specified record
        /// </summary>
        /// <param name="source"></param>
        /// <param name="Title"></param>
        /// <param name="ExcludeColumns"></param>
        /// <param name="page"></param>
        /// <param name="extraChoice"></param>
        /// <param name="Accept"></param>
        /// <param name="db"></param>
        /// <param name="readOnly">True if this should just be a display of values</param>
        /// <param name="ManagedSaving">If true, show the save button - caller is managing the version of the object passed and will save as needed. 
        /// Note that if false, the dialog result will also be true if the 'Close' button is pressed.</param>
        /// <param name="ExcludeProperties"></param>
        public EditableObjectDisplay(object source, string Title, string[] ExcludeProperties = null,
            ComboDisplay[] extraChoice = null, bool readOnly = false, bool ManagedSaving = false)
            :this(Title, ExcludeProperties, extraChoice)
        {
            Type t = source.GetType();
            CanRollback = ManagedSaving;
            myDataTypeName = t.Name; 
            this.Title = $"Edit '{myDataTypeName}'";
            //myData = Activator.CreateInstance(t);
            myData = source;    // source.DClone();  
            SetColumnCount(ColCount);
        }
        
        public static int ColCount = 3;
        
        public void AddButtons()
        {
            if (!ReadOnlyMode)
            {
                Type t = this.myData.GetType();
                MethodInfo[] methodList = t.GetMethods();
                foreach (var method in methodList)
                {
                    EditableObjectMethodAttribute[] atts = method.GetCustomAttributes(typeof(EditableObjectMethodAttribute), true) as EditableObjectMethodAttribute[];
                    if (atts != null)
                    {
                        foreach (var ButtonMethod in atts)
                        {
                            Button b = new Button
                            {
                                Name = "btn_" + method.Name + "_" + GET_WPF_NAME(ButtonMethod.ButtonName),
                                Content = ButtonMethod.ButtonName
                            };
                            //if (ButtonMethod.RefreshAfter)
                            {
                                b.Click += (sender, args) =>
                                {
                                    method.Invoke(myData, ButtonMethod.MethodParameters);
                                    if (ButtonMethod.RefreshAfter)
                                        SetWindowValues();
                                };

                            }/*
                        else
                        {
                            b.Click += (sender, args) =>
                            {
                                method.Invoke(myData, ButtonMethod.MethodParameters);
                            };
                        }*/
                            ButtonStack.Children.Add(b);
                        }
                    }
                }
            }
            if (ButtonStack.Children.Count == 0)
                ButtonStack.Visibility = Visibility.Collapsed;
        }
        private void SetWindowValues()
        {
            PropertyInfo[] props = myData.GetType().GetProperties();
            foreach(var prop in props)
            {
                SetContent(prop.Name, prop.GetValue(myData));
            }
        }
        /// <summary>
        /// Maps a property to a control and sets its value. Note that existence of the control does not need to be checked, 
        /// <para>
        /// because properties are added to the TypeMapping dictionary if they have a control added.
        /// </para>
        /// </summary>
        /// <param name="ControlName"></param>
        /// <param name="Value"></param>
        private void SetContent(string property, object Value)
        {
            if (!TypeMapping.ContainsKey(property))
                return;
            PropControlInfo valueControlInfo = TypeMapping[property];            
            switch (valueControlInfo.ControlType)
            {
                case PropControlType.Password:
                    {
                        PasswordBox pb = FindName(valueControlInfo.ElementName) as PasswordBox;
                        if (pb == null)
                            return;
                        Password pw = Value as Password;
                        if (pw == null)
                            pb.Password = string.Empty;
                        else
                            pb.Password = pw.value;
                        break;
                    }
                case PropControlType.EnumPicker:
                    {
                        ComboBox cb = FindName(valueControlInfo.ElementName) as ComboBox;
                        if (cb == null)
                            return;
                        cb.SelectedIndex = cb.Items.IndexOf(Value.ToString());
                        break;
                    }
                case PropControlType.CheckBox:
                    {
                        CheckBox cb = FindName(valueControlInfo.ElementName) as CheckBox;
                        if (cb == null)
                            return;
                        if (Value == null)
                            cb.IsChecked = null;

                        break;

                    }
            }
        }
        //Track type mapping for use in SetWindowValues and passing to SetContent
        Dictionary<string, PropControlInfo> TypeMapping;
        readonly DateTime maxDate = new DateTime(2900, 12, 1); //Set min/max based on SQL dates
        readonly DateTime minDate = new DateTime(1900, 1, 1);
        private void SetupPage()
        {
            if (myData == null)
            {
                Handle("No Object passed for editing", ExceptionLevel.UI);
                //new Alert("No object passed to edit.", Choice: false).ShowDialog();
                DialogResult = false;
                Close();
                return;
            } 
            Height = 150;
            DashboardData.RowDefinitions.Clear();            
            DashboardData.Children.Clear();
            
            //DashboardName.Text = myData.GetType().Name;
            var props = myData.GetType().GetProperties();

            int currentCol = 0;
            int currentRow = 0;
            bool AddHandler; //!string.IsNullOrWhiteSpace(AcceptProc);
            foreach(var item in props)
            {
                if (!item.CanRead)
                    continue;
                AddHandler = item.CanWrite.And(!ReadOnlyMode);
                bool idProp = item.Name.ToUpper() == myDataTypeName.ToUpper() + "ID";
                idProp = idProp || item.Name.Replace("_", "").ToUpper().In("ROWVERSION", "RV", "VERSION", "VERSIONID", "RECORDVERSION", "RECORDID");
                Action < object, RoutedPropertyChangedEventArgs<double?>> nc_Valuechanged = (sender, e) =>
                {
                    NumericUpDown nc = e.Source as NumericUpDown;
                    if (nc == null)
                        return;
                    item.SetValue(myData, Convert.ChangeType(nc.Value, item.PropertyType));
                    if (myValidator != null)
                        Save.IsEnabled = myValidator.CheckValid(myData);
                    e.Handled = true;
                };
                object o = item.GetValue(myData);
                if (o == DBNull.Value)
                    o = null;
                
                
                if (item.Name.ToUpper() == "COLOR")
                {
                    try
                    {
                        DashboardName.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString(o as string);
                    }
                    catch { }
                    continue;
                }
                if (combo_ExcludeNormalList.Contains(item.Name)) //prevent column names from showing up more than once due to combo 
                {
                    var q = (from cb in selections
                             where (string)cb.Tag == item.Name
                             select cb).First();
                    var selection = (from object i in q.Items
                                     let cdi = i as ComboDisplayItem
                                     where cdi.value == o
                                     select i).First();
                    q.SelectedIndex = (q.Items.IndexOf(selection)); //Reset selected index on corresponding combo box
                    continue;
                }
                
                bool tempEnable = true;
                if (item.Name.In(excludeList)) //Attribute should handle making readonly, not the exclude list.
                    continue;
                if (ReadOnlyMode || !AddHandler )
                    tempEnable = false;
                EditableObjectInfoAttribute info;
                {
                    object[] tempInfo = item.GetCustomAttributes(typeof(EditableObjectInfoAttribute), false);
                    if (tempInfo != null && tempInfo.Length > 0)
                    {
                        info = tempInfo[0] as EditableObjectInfoAttribute;
                        if (!info.CanUpdate)
                        {
                            AddHandler = false;
                            tempEnable = false;
                        }                        
                    }
                    else
                    {
                        info = new EditableObjectInfoAttribute(tempEnable); //Ensure we have an Info object for below
                    }
                }
                string tooltip = null;
                {
                    DescriptionAttribute tempInfo = item.GetCustomAttribute(typeof(DescriptionAttribute), false) as DescriptionAttribute;
                    if (tempInfo != null)
                        tooltip = tempInfo.Description;
                }
                if (item.Name.ToUpper().In(/*"LU",*/ "UID", "LMUID"))//Moved from top of loop so that UID/LMUID columns can be added to parameters dictionary...
                    continue;
                if (item.Name.ToUpper().StartsWith("HDN"))
                    continue;

                if (currentCol % myColumnCount == 0) //Can probably change to just add a row whenever it is zero exactly...
                    AddRow();
                
                Type t = Nullable.GetUnderlyingType(item.PropertyType) ?? item.PropertyType; 
                if (t == typeof(DateTime))
                {
                    Label l = new Label()
                    {
                        Content = FriendifyLabel( item.Name),
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);
                    //Add date picker, set value to o as datetime
                    //dataType = "DATE";
                    DatePicker dp = new DatePicker()
                    {                        
                        SelectedDate = o as DateTime?,                        
                        DisplayDateEnd = info.MaxDate?? maxDate,
                        DisplayDateStart = info.MinDate?? minDate,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Name="DateP_" + GET_WPF_NAME(item.Name),
                        Margin = new Thickness(5, 5, 5, 5),
                        IsEnabled = tempEnable,
                        ToolTip = tooltip
                    };
                    if (item.Name.ToUpper().In("DC", "LU", "DateCreated", "LastUpdated"))
                    {
                        dp.IsEnabled = false;
                    }
                    else if (AddHandler)
                    {
                        Action<object, SelectionChangedEventArgs> a = (sender, e) =>
                        {
                            DatePicker innerDP = sender as DatePicker;
                            if (innerDP == null)
                                return;
                            if(innerDP.SelectedDate == null)
                            {
                                if(t == item.PropertyType)
                                {
                                    item.SetValue(myData, null);
                                }
                            }else
                            {
                                item.SetValue(myData, (DateTime)innerDP.SelectedDate);
                            }
                            if (myValidator != null)
                                Save.IsEnabled = myValidator.CheckValid(myData);
                            e.Handled = true;
                        };
                        dp.SelectedDateChanged += new EventHandler<SelectionChangedEventArgs>(a);
                    }
                    DashboardData.Children.Add(dp);
                    TypeMapping.Add(item.Name, new PropControlInfo(dp.Name, PropControlType.DatePicker));
                    Grid.SetRow(dp, currentRow);
                    Grid.SetColumn(dp, currentCol);
                }
                #region enum control
                else if (t.IsEnum)
                {
                    Label l = new Label()
                    {
                        Content = FriendifyLabel(item.Name),
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);

                    ComboBox lb = new ComboBox {
                        Tag = item.Name, IsEnabled = tempEnable,
                        IsReadOnly = true,
                        Name="enumCB_" + GET_WPF_NAME(item.Name),
                        Margin = new Thickness(5, 5, 5, 5),
                        VerticalAlignment = VerticalAlignment.Bottom };
                    string[] nameList = Enum.GetNames(t);
                    foreach(string n in nameList)
                    {
                        var memInfo = t.GetMember(n)?[0]; //Get description? Meh...?
                        var hidden = memInfo?.GetCustomAttribute(typeof(EditableObjectHiddenEnumValueAttribute),
                            false);
                        if (hidden != null)
                            continue;
                                                
                        lb.Items.Add(n);/*
                            new ComboBoxItem
                        {                         
                            Content = n
                        });*/
                    }
                    if (o != null)
                    {
                        lb.SelectedIndex = lb.Items.IndexOf(o.ToString());
                    }                    
                    Action<object, SelectionChangedEventArgs> a = (sender, e) =>
                    {
                        ComboBox innerLb = sender as ComboBox;
                        if(innerLb != null)
                        {
                            string innerTemp = innerLb.SelectedItem as string;
                            if (innerTemp == null)
                            {
                                if(t != item.PropertyType) //Nullable enum
                                {
                                    item.SetValue(myData, null);
                                }
                            }
                            else
                            {
                                item.SetValue(myData, Enum.Parse(t, innerTemp));
                            }
                            if (myValidator != null)
                                Save.IsEnabled = myValidator.CheckValid(myData);
                            e.Handled = true;
                        }
                    };
                    lb.SelectionChanged += new SelectionChangedEventHandler(a);
                    DashboardData.Children.Add(lb);
                    TypeMapping.Add(item.Name, new PropControlInfo(lb.Name, PropControlType.EnumPicker));
                    Grid.SetRow(lb, currentRow);
                    Grid.SetColumn(lb, currentCol);
                    
                }
                #endregion
                else if (item.Name.Like("%Progress").And(
                    t == typeof(double) || t == typeof(short) || t == typeof(int) 
                    || t == typeof(decimal) || t== typeof(ushort) || t == typeof(uint) || t == typeof(float))
                    .And(!info.CanUpdate)//Only display progress if it can't be updated...
                    )
                {                    
                    double prog = (o == null? Convert.ToDouble(0) : Convert.ToDouble(o));
                    if( prog > 100 )
                        prog = 100;
                    else if(prog < 0)
                        prog = 0;
                    Brush barColor;
                    if (prog == 0)
                        barColor = Brushes.DarkRed;
                    else if (prog < 15)
                        barColor = Brushes.Red;
                    else if (prog < 30)
                        barColor = Brushes.OrangeRed;
                    else if (prog < 40)
                        barColor = Brushes.Orange;
                    else if (prog < 60)
                        barColor = Brushes.YellowGreen;
                    else if (prog < 80)
                        barColor = Brushes.GreenYellow;
                    else if (prog < 100)
                        barColor = Brushes.Green;
                    else
                        barColor = Brushes.DarkGreen;
                    Label l = new Label()
                    {
                        Content = FriendifyLabel(item.Name) + " (" + prog.ToString("F") + "%)",
                        VerticalAlignment = VerticalAlignment.Top,                        
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);

                    MetroProgressBar p = new MetroProgressBar()
                    {
                        Name = item.Name,
                        Value = prog,
                        Margin = new Thickness(5,5,5,5),
                        IsEnabled = false,
                        ToolTip = "0 to 100, percent based progress",
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Height = 25,
                        Foreground = barColor
                    };
                    DashboardData.Children.Add(p);
                    TypeMapping.Add(item.Name, new PropControlInfo(p.Name, PropControlType.Progress));
                    Grid.SetRow(p, currentRow);
                    Grid.SetColumn(p, currentCol);
                }
                else if (t == typeof(short) || t == typeof(ushort))
                {
                    Label l = new Label()
                    {
                        Content = FriendifyLabel(item.Name),
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);
                    NumericUpDown nc = new NumericUpDown()
                    {
                        Name = GET_WPF_NAME( item.Name),
                        Value = (o == null ? null as Double? : Convert.ToInt16(o)),
                        HasDecimals = false,
                        Maximum = info.MaxSize?? Int16.MaxValue,
                        Minimum = info.MinSize?? (t == typeof(ushort)? 0: Int16.MinValue),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(5, 5, 5, 5),
                        IsEnabled = tempEnable,
                        ToolTip = tooltip
                    };
                    if (item.Name.ToUpper() == "RV" || idProp)
                    {
                        nc.IsEnabled = false;
                    }
                    else if (AddHandler)
                        nc.ValueChanged += new RoutedPropertyChangedEventHandler<double?>(nc_Valuechanged);
                    DashboardData.Children.Add(nc);
                    TypeMapping.Add(item.Name, new PropControlInfo(nc.Name, PropControlType.NumPicker));
                    Grid.SetRow(nc, currentRow);
                    Grid.SetColumn(nc, currentCol);
                }
                else if (t == typeof(int) || t == typeof(uint))
                {
                    //dataType = "INT";
                    Label l = new Label()
                    {
                        Content = FriendifyLabel(item.Name),
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);
                    NumericUpDown nc = new NumericUpDown()
                    {
                        Name = GET_WPF_NAME( item.Name),
                        Value = (o == null? null as Double?: Convert.ToInt32(o)),
                        HasDecimals= false, 
                        Maximum = info.MaxSize?? Int32.MaxValue,
                        Minimum = info.MinSize ?? (t == typeof(uint)? 0: Int32.MinValue),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(5, 5, 5, 5),
                        IsEnabled = tempEnable,
                        ToolTip = tooltip
                    };

                    if (item.Name.ToUpper() == "RV" || idProp)
                    {
                        nc.IsEnabled = false;
                    }
                    else if (AddHandler)
                        nc.ValueChanged += new RoutedPropertyChangedEventHandler<double?>(nc_Valuechanged);
                    DashboardData.Children.Add(nc);
                    TypeMapping.Add(item.Name, new PropControlInfo(nc.Name, PropControlType.NumPicker));
                    Grid.SetRow(nc, currentRow);
                    Grid.SetColumn(nc, currentCol);
                }
                else if (t == typeof(double))
                {
                    //dataType = "DOUBLE";
                    Label l = new Label()
                    {
                        Content = FriendifyLabel(item.Name),
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);
                    NumericUpDown nc = new NumericUpDown()
                    {
                        Name = GET_WPF_NAME( item.Name),
                        Value = (o == null? null as Double?: Convert.ToDouble(o)),
                        HasDecimals = true,
                        Maximum = info.MaxSize?? double.MaxValue,
                        Minimum = info.MinSize?? double.MinValue,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(5, 5, 5, 5),
                        IsEnabled = tempEnable,
                        ToolTip = tooltip
                    };
                    if (item.Name.ToUpper() == "RV" || idProp)
                    {
                        nc.IsEnabled = false;
                    }
                    else if (AddHandler)
                        nc.ValueChanged += new RoutedPropertyChangedEventHandler<double?>(nc_Valuechanged);
                    DashboardData.Children.Add(nc);
                    TypeMapping.Add(item.Name, new PropControlInfo(nc.Name, PropControlType.NumPicker));
                    Grid.SetRow(nc, currentRow);
                    Grid.SetColumn(nc, currentCol);
                }
                else if (t == typeof(decimal))
                {
                    //dataType = "DECIMAL";
                    Label l = new Label()
                    {
                        Content = FriendifyLabel(item.Name),
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);
                    NumericUpDown nc = new NumericUpDown()
                    {
                        Name = GET_WPF_NAME(item.Name),
                        Value = (o == null? null as Double?: Convert.ToDouble(o)),
                        HasDecimals = true,
                        Maximum = info.MaxSize?? Convert.ToDouble(Decimal.MaxValue),
                        Minimum = info.MinSize?? Convert.ToDouble(Decimal.MinValue),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(5, 5, 5, 5),
                        IsEnabled = tempEnable,
                        ToolTip = tooltip
                    };
                    if (item.Name.ToUpper() == "RV" || idProp)
                    {
                        nc.IsEnabled = false;
                    }
                    else if (AddHandler)
                        nc.ValueChanged += new RoutedPropertyChangedEventHandler<double?>(nc_Valuechanged);                    
                    DashboardData.Children.Add(nc);
                    TypeMapping.Add(item.Name, new PropControlInfo(nc.Name, PropControlType.NumPicker));
                    Grid.SetRow(nc, currentRow);
                    Grid.SetColumn(nc, currentCol);
                }
                else if (t == typeof(bool))
                {
                    //dataType = "BOOL";
                    CheckBox cb = new CheckBox()
                    {
                        Name = GET_WPF_NAME( item.Name),
                        IsChecked = (o == null ? null as bool? : Convert.ToBoolean(o)),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Content = FriendifyLabel(item.Name),
                        Margin = new Thickness(5, 5, 5, 5),
                        IsEnabled = tempEnable,
                        ToolTip = tooltip
                    };
                    Action<object, RoutedEventArgs > cb_Checked = (sender, e) =>
                     {
                         CheckBox innerCB = e.Source as CheckBox;
                         if (innerCB != null)
                         {
                             CheckNullableAndSet(item, innerCB.IsChecked);
                             e.Handled = true;
                         }
                         if (myValidator != null)
                             Save.IsEnabled = myValidator.CheckValid(myData);
                     };

                    if (AddHandler)
                        cb.Checked += new RoutedEventHandler(cb_Checked); 
                    DashboardData.Children.Add(cb);
                    TypeMapping.Add(item.Name, new PropControlInfo(cb.Name, PropControlType.CheckBox));
                    Grid.SetRow(cb, currentRow);
                    Grid.SetColumn(cb, currentCol);
                }
                else if(t == typeof(Password))
                {
                    //masked.
                    //TODO:
                    Label l = new Label()
                    {
                        Content = FriendifyLabel(item.Name),
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);
                    PasswordBox pb = new PasswordBox
                    {
                        Name = GET_WPF_NAME(item.Name),
                        Password = item.GetValue(myData).ToString(),
                        Margin = new Thickness(5,5,5,5),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        IsEnabled = tempEnable,
                        ToolTip = tooltip
                    };
                    pb.PasswordChanged += (sender, e) => 
                    {
                        PasswordBox temp = sender as PasswordBox;
                        if (temp == null)
                            return;
                        item.SetValue(myData, new Password(temp.Password));
                        if (myValidator != null)
                            Save.IsEnabled = myValidator.CheckValid(myData);
                        e.Handled = true;
                    };
                    DashboardData.Children.Add(pb); //If remove sv, revert to using tb
                    TypeMapping.Add(item.Name, new PropControlInfo(pb.Name, PropControlType.Password));
                    Grid.SetRow(pb, currentRow);
                    Grid.SetColumn(pb, currentCol);
                }
                else if (t == typeof(string) || t == typeof(string))
                {
                    //string
                    Label l = new Label()
                    {
                        Content = FriendifyLabel(item.Name),
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);
                    TextBox tb = new TextBox()
                    {
                        Name = GET_WPF_NAME( item.Name ),
                        TextWrapping = TextWrapping.Wrap,
                        IsUndoEnabled = true,
                        IsInactiveSelectionHighlightEnabled = true,
                        Text = o.ToString(),
                        Margin = new Thickness(5, 5, 5, 5),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        IsEnabled = tempEnable,
                        ToolTip = tooltip
                    };
                    ScrollViewer sv = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                         VerticalAlignment = VerticalAlignment.Bottom //Match text box to be safe?
                    };
                    sv.Content = tb;


                    if (item.Name.In("CreatedBy", "UpdatedBy"))
                    {
                        tb.IsEnabled = false;
                    }
                    else {
                        tb.GotFocus += Tb_GotFocus;
                        tb.LostFocus += Tb_LostFocus;
                        Action<object, TextChangedEventArgs> tb_Changed = (sender, e) =>{
                            TextBox innerTb = e.Source as TextBox;
                            if (tb == null)
                                return;
                            item.SetValue(myData, innerTb.Text);
                            if (myValidator != null)
                                Save.IsEnabled = myValidator.CheckValid(myData);
                        };
                        if (AddHandler)
                            tb.TextChanged += new TextChangedEventHandler(tb_Changed);
                    }
                    DashboardData.Children.Add(sv); //If remove sv, revert to using tb
                    TypeMapping.Add(item.Name, new PropControlInfo(sv.Name, PropControlType.Text));
                    Grid.SetRow(sv, currentRow);
                    Grid.SetColumn(sv, currentCol);
                }
                else
                {
                    continue; //Unsupported data type. don't update positions
                }
                currentCol++;
                
                if(currentCol >= myColumnCount)
                {
                    currentRow++;
                    currentCol = 0;
                }
            }
            foreach(ComboBox cb in selections)
            {
                string Param = cb.Tag as string;
                if (currentCol % myColumnCount == 0) //Can probably change to just add a row whenever it is zero exactly...
                    AddRow();
                ComboDisplayItem cdi = cb.SelectedItem as ComboDisplayItem;                
                
                Label l = new Label()
                {
                    Content = FriendifyLabel( Param),
                    VerticalAlignment = VerticalAlignment.Top
                };
                DashboardData.Children.Add(l); 
                Grid.SetRow(l, currentRow);
                Grid.SetColumn(l, currentCol);
                Grid.SetRow(cb, currentRow);
                Grid.SetColumn(cb, currentCol);
                DashboardData.Children.Add(cb);
                cb.VerticalAlignment = VerticalAlignment.Bottom;

                currentCol++;
                if (currentCol >= myColumnCount)
                {
                    currentRow++;
                    currentCol = 0;
                }
            }
            AddButtons();
        }

        #region TextBox Handling
        const double TEXT_FOCUSED_HEIGHT = 65;
        const double TEXT_NOFOCUS_HEIGHT = 25;

        private void Tb_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if(tb != null)
            {
                tb.Height = TEXT_NOFOCUS_HEIGHT;
                e.Handled = true;
            }
        }

        private void Tb_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if(tb != null)
            {
                tb.Height = TEXT_FOCUSED_HEIGHT;
                e.Handled = true;
            }
        }
        #endregion                 
        private void AddRow()
        {
            RowDefinition r = new RowDefinition() { Height = new GridLength(TEXT_FOCUSED_HEIGHT) };
            DashboardData.RowDefinitions.Add(r);
            if (Height < 900)
                Height += TEXT_FOCUSED_HEIGHT + 15; //May need to set height on scrollviewer maybe??
            /*
            if (DashboardScroller.Height < 900)
                DashboardScroller.Height += TEXT_FOCUSED_HEIGHT + 5;
            if (DashboardData.Height < 900)
                DashboardData.Height += TEXT_FOCUSED_HEIGHT + 5; 
                */
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = (myData != null); //Should really never happen, but peace of mind
            this.Close();
        }
        

        private void Close_Click(object sender, RoutedEventArgs e)
        { 
            this.DialogResult = !CanRollback; //Return true if there's no save button.
            this.Close();
        }                
    }
}
