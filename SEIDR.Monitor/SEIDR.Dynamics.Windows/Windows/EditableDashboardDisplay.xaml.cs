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
using SEIDR.DataBase;
using SEIDR;
//using SEIDR.Processing.Data.DBObjects;
//using SEIDR.Processing;
using MahApps.Metro.Controls;
//using SEIDR.Extensions;
using SEIDR.Dynamics.Configurations;

//Don't care about CS0414 in this file.. restore at end of page
#pragma warning disable 414 
namespace SEIDR.Dynamics.Windows
{
    /// <summary>
    /// Interaction logic for DashboardDisplay.xaml
    /// <para>
    /// Special Column naming:     
    /// </para>
    /// <para>
    /// Start with 'hdn' : Hidden column, can be passed to the save parameter, but will not be shown to users
    /// </para>
    /// <para>
    /// Start with 'dtl_' : Detail column, will not be passed to save parameters. Is shown to users as readonly
    /// </para>
    /// </summary>
    public sealed partial class EditableDashboardDisplay : BasicSessionWindow
    {
        int myColumnCount = 3;
        /// <summary>
        /// The datatable edited on this page.
        /// </summary>
        public DataTable myData;
        /// <summary>
        /// Returns the record passed or last edited on this page (depending on set up)
        /// </summary>
        public DataRowView myDataRowView
        {
            get
            {
                return myData.AsDataView()[myPage];
            }
        }
        //DBTable myData;
        bool CanChangePage = false; //NYI. If changing pages, should only be used when passed a datatable, just need to update the PAGE variable and call set up..
        SqlCommand refreshCmd;
        DatabaseConnection db;
        Dictionary<string, object> AcceptClickParams;
        Dictionary<string, Type> ClickTypes;
        string AcceptProc;
        bool ReadOnlyMode = false;
        string myName;
        private bool DefaultEnable { get { return AcceptProc != null; } }
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
        bool _KeepOriginalName = false;
        /// <summary>
        /// Base constructor. private
        /// </summary>
        /// <param name="Title"></param>
        /// <param name="ExcludeColumns"></param>
        /// <param name="extraChoice"></param>
        /// <param name="readOnlyMode"></param>
        private EditableDashboardDisplay(string Title, string[] ExcludeColumns, 
            ComboDisplay[] extraChoice, bool readOnlyMode, bool MaintainOriginalName)
            :base(false)
        {

            InitializeComponent();
            if (readOnlyMode)
                Save.Visibility = Visibility.Collapsed;
            ReadOnlyMode = readOnlyMode;
            ClickTypes = new Dictionary<string, Type>();
            AcceptClickParams = new Dictionary<string, object>();
            myName = Title;
            this.Title = Title.ToUpper() + (readOnlyMode ? " - [READ ONLY MODE]" : "");
            myColumnCount = DashboardData.ColumnDefinitions.Count;
            _KeepOriginalName = MaintainOriginalName;
            DataContext = this;
            excludeList = ExcludeColumns;
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
                    AcceptClickParams.Add(d.Name, o);
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
            //SetColumnCount(3);
        }

        private void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb == null)
                return;
            string name = (string)cb.Tag;
            var i = (cb.SelectedValue as ComboDisplayItem);
            AcceptClickParams[name] = i == null ? null : i.value;            
        }

        private List<ComboBox> selections = new List<ComboBox>();
        /// <summary>
        /// Create editable dashboard to modify a dashboard at the specified record. 
        /// <para>Will modify the provided source table.</para>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="Title"></param>
        /// <param name="ExcludeColumns"></param>
        /// <param name="page"></param>
        /// <param name="extraChoice"></param>
        /// <param name="Accept"></param>
        /// <param name="db"></param>
        /// <param name="readOnly"></param>
        public EditableDashboardDisplay(DataTable source, string Title, string[] ExcludeColumns = null, int page = 0, 
            ComboDisplay[] extraChoice = null, bool readOnly = false, DatabaseConnection db = null, string Accept = null, bool MaintainOriginalName = false)
            :this(Title, ExcludeColumns, extraChoice, readOnly && Accept == null, MaintainOriginalName)
        {                        
            refreshCmd = null;
            AcceptProc = readOnly ? null : Accept.nTrim();
            this.db = readOnly ? null : db;
            //TODO: Add a button for updating number
            myPage = page;
            myData = source; //Okay to use the actual table because we still need to save to actually update any of the rows. 
            
            SetColumnCount(ColCount);
        }
        
        /// <summary>
        /// Default column count for new Editable dashboard displays
        /// </summary>
        public static int ColCount = 3;
        /// <summary>
        /// Setup an editable dashboard for the row. Does not change the passed row, and instead creates a copy of it.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="Title"></param>
        /// <param name="ExcludedCols"></param>
        /// <param name="extraChoice"></param>
        /// <param name="readOnlyMode"></param>
        /// <param name="db"></param>
        /// <param name="Accept"></param>
        public EditableDashboardDisplay(DataRowView row, string Title, string[] ExcludedCols = null,
            ComboDisplay[] extraChoice = null, bool readOnlyMode = false, 
            DatabaseConnection db = null, string Accept = null, bool MaintainOriginalName = false) 
            :this(Title, ExcludedCols, extraChoice, readOnlyMode && Accept == null, MaintainOriginalName)
        {
            refreshCmd = null;
            AcceptProc = readOnlyMode? null: Accept.nTrim();
            this.db = readOnlyMode? null : db;
            myPage = 0;
            myData = row.Row.Table.Clone();
            DataRow r = myData.NewRow();
            foreach(DataColumn col in myData.Columns)
            {
                r[col.ColumnName] = row[col.ColumnName];
            }
            myData.Rows.Add(r);
            myPage = 0;
            SetColumnCount(ColCount);
            CanChangePage = false;
        }
        /// <summary>
        /// Create editable dashboard to view the top record of a procedure.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="db"></param>
        /// <param name="Title"></param>
        /// <param name="Accept"></param>
        /// <param name="ExcludeColumns">
        /// </param>
        /// <param name="extraChoice"></param>
        public EditableDashboardDisplay(SqlCommand cmd, DatabaseConnection db, string Title, string Accept = null, string[] ExcludeColumns = null,
            ComboDisplay[] extraChoice = null, bool MaintainOriginalName = false)
            :this(Title, ExcludeColumns, extraChoice, readOnlyMode: Accept == null, MaintainOriginalName:MaintainOriginalName)
        {
            /*
            myName = Title;
            InitializeComponent();
            myColumnCount = DashboardData.ColumnDefinitions.Count;
            DataContext = this;*/
            refreshCmd = cmd;            
            AcceptProc = Accept;
            if (Accept != null && string.IsNullOrWhiteSpace(Accept))
                AcceptProc = null;
            this.db = db;
            //excludeList = ExcludeColumns;

            //d = new Dashboard(sourceCommand, connection);
            //current = d.RefreshList();
            //this.Title = Title;
            
            
            //if (dt == null)
            //    throw new Exception("Cannot Create an Editable Dashboard Display without a DBTable to edit.");
            //myData = dt;
            try
            {
                DataSet ds = db.RunCommand(cmd);
            }
            catch(Exception e)
            {
                new Alert(e.Message, Choice: false).ShowDialog();
                this.DialogResult = false;
                this.Close();
                return;
            }
            //SetupPage();
            SetColumnCount(ColCount);
        }
        /// <summary>
        /// Finalizer.... 
        /// </summary>
        ~EditableDashboardDisplay()
        {
            if(refreshCmd != null)
                refreshCmd.Dispose();
        }
        private string WPF_NAME(string name)
        {
            return System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");
        }
        private void setToolTip(FrameworkElement fe, string tooltip)
        {
            if (string.IsNullOrWhiteSpace(tooltip))
                return;            
            fe.ToolTip = tooltip;
        }
        private string FRIENDLY_NAME(string name) => EditableObjectHelper.FriendifyLabel(name);
        readonly DateTime maxDate = new DateTime(2900, 12, 1); //Set min/max based on SQL dates
        readonly DateTime minDate = new DateTime(1900, 1, 1);        
        private void SetupPage(int page = 0)
        {
            if (myData.Rows.Count == 0)
            {
                //new Alert("No Data results found.", Choice: false).ShowDialog();                
                Handle("No Data results found.", ExceptionLevel.UI_Basic);
                if (Registered)
                {
                    this.DialogResult = false;
                    this.Close();
                }
                else
                    CanShowWindow = false;
                return;
            }
            if(page >= myData.Rows.Count)
            {
                Handle("Page is out of range", ExceptionLevel.UI_Basic);
                //new Alert("Page out of range", Choice: false).ShowDialog();                
                return;
            }
            Height = 150;
            DashboardData.RowDefinitions.Clear();
            AcceptClickParams.Clear();            
            ClickTypes.Clear();
            DashboardData.Children.Clear();
            DataRow r = myData.Rows[page];
            myPage = page;
            //DashboardName.Text = myData.GetType().Name;
            var props = myData.GetType().GetProperties();
            int currentCol = 0;
            int currentRow = 0;
            bool AddHandler = !ReadOnlyMode; //!string.IsNullOrWhiteSpace(AcceptProc);
            foreach (DataColumn item in myData.Columns)
            {
                object o = r[item];
                if (o == DBNull.Value)
                    o = null;
                if (item.ColumnName.ToUpper() == "COLOR")
                {
                    try
                    {
                        DashboardName.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString(o as string);
                    }
                    catch { }
                    continue;
                }
                if (combo_ExcludeNormalList.Contains(item.ColumnName)) //prevent column names from showing up more than once due to combo 
                {
                    var q = (from cb in selections
                             where (string)cb.Tag == item.ColumnName
                             select cb).First();
                    var selection = (from object i in q.Items
                                     let cdi = i as ComboDisplayItem
                                     where cdi.value == o
                                     select i).First();
                    q.SelectedIndex = (q.Items.IndexOf(selection)); //Reset selected index on corresponding combo box
                    continue;
                }
                string tempName = WPF_NAME(item.ColumnName);
                bool tempEnable = DefaultEnable;                
                Action<FrameworkElement> addTooltip = (fe) => setToolTip(fe, item.Caption == item.ColumnName? null: item.Caption);
                                
                string labelContent = FRIENDLY_NAME(item.ColumnName);

                bool DETAIL_ONLY = item.ColumnName.ToUpper().StartsWith("DTL_");
                if (ReadOnlyMode || item.ColumnName.In(excludeList) || DETAIL_ONLY)
                    tempEnable = false;
                if (!DETAIL_ONLY)
                {
                    AcceptClickParams.Add(item.ColumnName, o); //Hdn can be passed to parameter, but DTL_ records should be read only and not affect any state.
                    ClickTypes.Add(item.ColumnName, item.DataType);
                }
                else
                    labelContent = FRIENDLY_NAME(item.ColumnName.Substring(4)); //Remove "DTL_"                                    

                if (_KeepOriginalName || labelContent[0] == '@') //If a parameter......Leave it alone.
                    labelContent = item.ColumnName;


                if (item.ColumnName.ToUpper().In(/*"LU",*/ "UID", "LMUID"))//Moved from top of loop so that UID/LMUID columns can be added to parameters dictionary...
                    continue;
                if (item.ColumnName.ToUpper().StartsWith("HDN"))
                    continue;

                if (currentCol % myColumnCount == 0) //Can probably change to just add a row whenever it is zero exactly...
                    AddRow();
                //Grid.SetR
                //string dataType = "STRING";
                Type t = item.DataType; //Note: For dataTable, don't need to check nullable. For object editor, will need to get underlying type for comparison
                if (t == typeof(DateTime))
                {
                    Label l = new Label()
                    {
                        Content = labelContent, // item.ColumnName,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);
                    //Add date picker, set value to o as datetime
                    //dataType = "DATE";
                    DatePicker dp = new DatePicker()
                    {
                        //Name = item.ColumnName.Replace,
                        Name = tempName,
                        Tag = item.ColumnName,
                        SelectedDate = o as DateTime?,
                        DisplayDateEnd = maxDate,
                        DisplayDateStart = minDate,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(5, 5, 5, 5),
                        IsEnabled = tempEnable
                    };
                    addTooltip(dp);
                    if (item.ColumnName.ToUpper().In("DC", "LU", "DATECREATED", "LASTUPDATED"))
                    {
                        dp.IsEnabled = false;
                    }
                    else if (AddHandler && tempEnable) //Note that if tempEnable turns it off, the handler will never be fired anyway 
                        dp.SelectedDateChanged += dp_SelectedDateChanged;
                    DashboardData.Children.Add(dp);
                    Grid.SetRow(dp, currentRow);
                    Grid.SetColumn(dp, currentCol);
                }
                else if(t.IsEnum && t.GetCustomAttributes(typeof(FlagsAttribute), false).HasMinimumCount(1))
                {
                    Label l = new Label()
                    {
                        Content = labelContent, //item.ColumnName,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);
                    var fp = new Configurations.DynamicEditor.FlagPicker(o as Enum)
                    {
                        Tag = item.ColumnName
                    };
                    Action<object, EventArgs> a = (sender, e) =>
                    {
                        var flagp = sender as Configurations.DynamicEditor.FlagPicker;                         
                        if (flagp != null)
                        {
                            AcceptClickParams[item.ColumnName] = flagp.FlagValue; // Enum.Parse(t, flagp.FlagValue.ToString());
                        }
                    };
                    if (AddHandler && tempEnable) //Note that if tempEnable turns it off, the handler will never be fired anyway
                        fp.FlagChanged += new EventHandler(a);
                    Grid.SetRow(fp, currentRow);
                    Grid.SetColumn(fp, currentCol);
                }
                else if (t.IsEnum)
                {
                    Label l = new Label()
                    {
                        Content = labelContent, //item.ColumnName,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);

                    ListBox lb = new ListBox { Name = tempName, Tag = item.ColumnName, IsEnabled = tempEnable };
                    string[] nameList = Enum.GetNames(t);
                    foreach (string n in nameList)
                    {
                        lb.Items.Add(n);
                    }
                    if (o != null)
                    {
                        lb.SelectedIndex = lb.Items.IndexOf(o.ToString());
                    }
                    Action<object, SelectionChangedEventArgs> a = (sender, e) =>
                    {
                        ListBox innerLb = sender as ListBox;
                        if (innerLb != null)
                        {
                            string innerTemp = innerLb.SelectedItem as string;
                            if (innerTemp == null)
                                AcceptClickParams[innerLb.Name] = null;
                            else {
                                AcceptClickParams[innerLb.Name] = Enum.Parse(t, innerTemp);
                            }
                        }
                    };
                    if (AddHandler && tempEnable) //Note that if tempEnable turns it off, the handler will never be fired anyway
                        lb.SelectionChanged += new SelectionChangedEventHandler(a);
                    Grid.SetRow(lb, currentRow);
                    Grid.SetColumn(lb, currentCol);
                }
                /*
                else if (t == typeof(TimeSpan))
                {
                    //? ummm... just... cast as time span in the click event I guess?
                }*/
                else if (item.ColumnName.Like("%Progress").And(
                    t == typeof(double) || t == typeof(short) || t == typeof(int) || t== typeof(byte)
                    || t == typeof(decimal) || t== typeof(ushort) || t == typeof(uint) || t == typeof(float))
                    )
                {
                    BrushConverter bc = new BrushConverter();
                    double prog = (o == null? Convert.ToDouble(0) : Convert.ToDouble(o));
                    if( prog > 100 )
                        prog = 100;
                    else if(prog < 0)
                        prog = 0;
                    Brush barColor;
                    if (prog == 0)
                        barColor = Brushes.DarkRed;  //(SolidColorBrush)bc.ConvertFromString("DarkRed");
                    else if (prog < 15)
                        barColor = Brushes.Red;// (SolidColorBrush)bc.ConvertFromString("LightRed");
                    else if (prog < 40)
                        barColor = Brushes.Orange; // (SolidColorBrush)bc.ConvertFromString("LightOrange");
                    else if (prog < 60)
                        barColor = Brushes.YellowGreen; // (SolidColorBrush)bc.ConvertFromString("Yellow");
                    else if (prog < 80)
                        barColor = Brushes.GreenYellow; // (SolidColorBrush)bc.ConvertFromString("YellowGreen");
                    else if (prog < 100)
                        barColor = Brushes.Green;  //(SolidColorBrush)bc.ConvertFromString("DarkGreen");
                    else
                        barColor = Brushes.DarkGreen; //(SolidColorBrush)new BrushConverter().ConvertFromString("Green");
                    Label l = new Label()
                    {
                        Content = labelContent //item.ColumnName 
                                    + " (" + prog.ToString("F") + "%)",
                        VerticalAlignment = VerticalAlignment.Top,                        
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);

                    MetroProgressBar p = new MetroProgressBar()
                    {
                        Name = tempName,
                        Tag = item.ColumnName,
                        Value = prog,
                        Margin = new Thickness(5,5,5,5),
                        IsEnabled = false,
                        ToolTip = "0 to 100, percent based progress",
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Height = 25,
                        Foreground = barColor
                    };
                    addTooltip(p);
                    DashboardData.Children.Add(p);
                    Grid.SetRow(p, currentRow);
                    Grid.SetColumn(p, currentCol);
                }
                else if (t == typeof(short) || t == typeof(ushort))
                {
                    Label l = new Label()
                    {
                        Content = labelContent, //item.ColumnName,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);
                    NumericUpDown nc = new NumericUpDown()
                    {
                        Name = tempName,
                        Tag = item.ColumnName,
                        Value = (o == null ? null as Double? : Convert.ToInt16(o)),
                        HasDecimals = false,
                        Maximum = Int16.MaxValue,
                        Minimum = t == typeof(ushort)? 0: Int16.MinValue,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(5, 5, 5, 5),
                        IsEnabled = tempEnable
                    };
                    addTooltip(nc);
                    if (item.ColumnName.ToUpper() == "RV" 
                        || item.ColumnName.Like("%ID").And(item.Ordinal == 0))
                    {
                        nc.IsEnabled = false;
                    }
                    else if (AddHandler)
                        nc.ValueChanged += nc_ValueChanged;
                    DashboardData.Children.Add(nc);
                    Grid.SetRow(nc, currentRow);
                    Grid.SetColumn(nc, currentCol);
                }
                else if (t == typeof(byte) )
                {
                    Label l = new Label()
                    {
                        Content = labelContent, //item.ColumnName,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);
                    NumericUpDown nc = new NumericUpDown()
                    {
                        Name = tempName,
                        Tag = item.ColumnName,
                        Value = (o == null ? null as Double? : Convert.ToInt16(o)),
                        HasDecimals = false,
                        Maximum = Byte.MaxValue,
                        Minimum = Byte.MinValue,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(5, 5, 5, 5),
                        IsEnabled = tempEnable
                    };
                    addTooltip(nc);
                    if (item.ColumnName.ToUpper() == "RV" || item.ColumnName.Like("%ID").And(item.Ordinal == 0))
                    {
                        nc.IsEnabled = false;
                    }
                    else if (AddHandler)
                        nc.ValueChanged += nc_ValueChanged;
                    DashboardData.Children.Add(nc);
                    Grid.SetRow(nc, currentRow);
                    Grid.SetColumn(nc, currentCol);
                }
                else if (t == typeof(int) || t == typeof(uint))
                {
                    //dataType = "INT";
                    Label l = new Label()
                    {
                        Content = labelContent, //item.ColumnName,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);
                    NumericUpDown nc = new NumericUpDown()
                    {
                        Name = tempName,
                        Tag = item.ColumnName,
                        Value = (o == null? null as Double?: Convert.ToInt32(o)),
                        HasDecimals= false, 
                        Maximum = Int32.MaxValue,
                        Minimum = t == typeof(uint)? 0: Int32.MinValue,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(5, 5, 5, 5),
                        IsEnabled = tempEnable
                    };
                    addTooltip(nc);
                    if (item.ColumnName.ToUpper() == "RV" || item.ColumnName.Like("%ID").And(item.Ordinal == 0))
                    {
                        nc.IsEnabled = false;
                    }
                    else if (AddHandler)
                        nc.ValueChanged += nc_ValueChanged;
                    DashboardData.Children.Add(nc);
                    Grid.SetRow(nc, currentRow);
                    Grid.SetColumn(nc, currentCol);
                }
                else if (t == typeof(double))
                {
                    //dataType = "DOUBLE";
                    Label l = new Label()
                    {
                        Content = labelContent, //item.ColumnName,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);
                    NumericUpDown nc = new NumericUpDown()
                    {
                        Name = tempName,
                        Tag = item.ColumnName,
                        Value = (o == null? null as Double?: Convert.ToDouble(o)),
                        HasDecimals = true,
                        Maximum = double.MaxValue,
                        Minimum = double.MinValue,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(5, 5, 5, 5),
                        IsEnabled = tempEnable
                    };
                    addTooltip(nc);
                    if (item.ColumnName.ToUpper() == "RV" || item.ColumnName.Like("%ID").And(item.Ordinal == 0)) //Disable the FIRST id only
                    {
                        nc.IsEnabled = false;
                    }
                    else if (AddHandler)
                        nc.ValueChanged += nc_ValueChanged;
                    DashboardData.Children.Add(nc);
                    Grid.SetRow(nc, currentRow);
                    Grid.SetColumn(nc, currentCol);
                }
                else if (t == typeof(decimal))
                {
                    //dataType = "DECIMAL";
                    Label l = new Label()
                    {
                        Content = labelContent, //item.ColumnName,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);
                    NumericUpDown nc = new NumericUpDown()
                    {
                        Name = tempName,
                        Tag = item.ColumnName,
                        Value = (o == null? null as Double?: Convert.ToDouble(o)),
                        HasDecimals = true,
                        Maximum = Convert.ToDouble(Decimal.MaxValue),
                        Minimum = Convert.ToDouble(Decimal.MinValue),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(5, 5, 5, 5),
                        IsEnabled = tempEnable
                    };
                    addTooltip(nc);
                    if (item.ColumnName.ToUpper() == "RV" 
                        || item.ColumnName.ToUpper() == "RecordVersion"
                        || item.ColumnName.Like("%ID").And(item.Ordinal == 0))
                    {
                        nc.IsEnabled = false;
                    }
                    else if (AddHandler)
                        nc.ValueChanged += nc_ValueChanged;                    
                    DashboardData.Children.Add(nc);
                    Grid.SetRow(nc, currentRow);
                    Grid.SetColumn(nc, currentCol);
                }
                else if (t == typeof(bool))
                {
                    //dataType = "BOOL";
                    CheckBox cb = new CheckBox()
                    {
                        Name = tempName,
                        Tag = item.ColumnName,
                        IsChecked = (o == null ? null as bool? : Convert.ToBoolean(o)),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Content = labelContent, //item.ColumnName,
                        Margin = new Thickness(5, 5, 5, 5),
                        IsEnabled = tempEnable
                    };
                    addTooltip(cb);
                    if (AddHandler)
                        cb.Checked += cb_Checked; 
                    DashboardData.Children.Add(cb);
                    Grid.SetRow(cb, currentRow);
                    Grid.SetColumn(cb, currentCol);
                }
                else if(t == typeof(Password))
                {
                    //masked text box....Will be mroe important for Editable object display
                }
                else
                {
                    //string
                    Label l = new Label()
                    {
                        Content = labelContent, //item.ColumnName,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    DashboardData.Children.Add(l);
                    Grid.SetRow(l, currentRow);
                    Grid.SetColumn(l, currentCol);
                    TextBox tb = new TextBox()
                    {
                        Name = tempName,
                        Tag = item.ColumnName,
                        TextWrapping = TextWrapping.Wrap,
                        IsUndoEnabled = true,
                        IsInactiveSelectionHighlightEnabled = true,
                        Text = o == null? "" : o.ToString(),
                        Margin = new Thickness(5, 5, 5, 5),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        IsEnabled = tempEnable
                    };
                    addTooltip(tb);
                    ScrollViewer sv = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                         VerticalAlignment = VerticalAlignment.Bottom //Match text box to be safe?
                    };
                    sv.Content = tb;
                    if (item.ColumnName.ToUpper().In("CREATEDBY", "UPDATEDBY", "CB", "UB"))
                    {
                        tb.IsEnabled = false;
                    }
                    else {
                        tb.GotFocus += Tb_GotFocus;
                        tb.LostFocus += Tb_LostFocus;
                        if (AddHandler && tempEnable)
                            tb.TextChanged += tb_TextChanged;
                    }
                    DashboardData.Children.Add(sv); //If remove sv, revert to using tb
                    Grid.SetRow(sv, currentRow);
                    Grid.SetColumn(sv, currentCol);
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
                //AcceptClickParams
                if(!AcceptClickParams.ContainsKey(Param))
                    AcceptClickParams.Add(Param, cdi == null? null : cdi.value);
                Label l = new Label()
                {
                    Content = FRIENDLY_NAME(Param),
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
        }
         

        const double TEXT_FOCUSED_HEIGHT = 85;
        const double TEXT_NOFOCUS_HEIGHT = 25;

        private void Tb_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if(tb != null)
            {
                tb.Height = TEXT_NOFOCUS_HEIGHT;
            }
        }

        private void Tb_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if(tb != null)
            {
                tb.Height = TEXT_FOCUSED_HEIGHT;
            }
        }

        void dp_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            DatePicker dp = e.Source as DatePicker;
            if (dp != null)
            {
                AcceptClickParams[dp.Tag.ToString()] = dp.SelectedDate;
            }
        }

        void tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = e.Source as TextBox;
            if (tb != null)
            {
                Type t = ClickTypes[tb.Tag.ToString()];
                if (t != typeof(string))
                {
                    try
                    {
                        AcceptClickParams[tb.Tag.ToString()] = Convert.ChangeType(tb.Text, ClickTypes[tb.Tag.ToString()]);
                    }
                    catch { }
                }
                else
                {
                    AcceptClickParams[tb.Tag.ToString()] = tb.Text;
                }
            }
        }

        void cb_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = e.Source as CheckBox;
            if (cb != null)
            {
                AcceptClickParams[cb.Tag.ToString()] = cb.IsChecked;
            }
        }

        void nc_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            NumericUpDown nc = e.Source as NumericUpDown;
            if (nc == null)
                return;
            Type t = ClickTypes[nc.Tag.ToString()];
            AcceptClickParams[nc.Tag.ToString()] = nc.Value.HasValue? Convert.ChangeType(nc.Value.Value, t) as object: DBNull.Value;
        }
        private void AddRow()
        {
            RowDefinition r = new RowDefinition() { Height = new GridLength(TEXT_FOCUSED_HEIGHT) };
            DashboardData.RowDefinitions.Add(r);
            if (Height < 900)
                Height += TEXT_FOCUSED_HEIGHT + 5;
            /*
            if (DashboardData.Height < 900)
                DashboardData.Height += 60;
                
            */
        }
        /// <summary>
        /// Save the changes using accept proc, or else by updating the current datarow in myData
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            //SetupPage();
            
            //myData.InsertUpdate();
            if (!string.IsNullOrWhiteSpace(AcceptProc))
            {
                if (!RunAcceptProc())
                    return; // Should alert user and wait for user interaction..
            }
            else
            {
                foreach (var kv in AcceptClickParams)
                {
                    if (!myData.Columns.Contains(kv.Key)) //Has to match tags on the objects to set this correctly
                        continue; //possible with combo boxes technically
                    myData.Rows[myPage][kv.Key] = kv.Value ?? DBNull.Value;
                }                
            }
            //ToDo: Move to a close button, allow users to choose another page?
            if (CanChangePage) //Currently always false - if and when a change page is added, it should make use of this variable
                return; 
            if(_Dialog)
                DialogResult = true;            
            Close();
        }
        private int myPage = 0;
        private bool RunAcceptProc()
        {            
            bool x = false;
            try
            {
                using (SqlConnection conn = new SqlConnection(db.ConnectionString))
                {
                    using (SqlCommand c = new SqlCommand(AcceptProc))
                    {
                        conn.Open();
                        c.Connection = conn;
                        //c.CommandTimeout = /*Proc*/.TimeOut; //Set by connection string actually...
                        c.CommandType = CommandType.StoredProcedure;
                        SqlCommandBuilder.DeriveParameters(c);
                        foreach (var kv in AcceptClickParams)
                        {
                            string parmName = "@" + kv.Key;
                            if (c.Parameters.Contains(parmName))
                            {
                                //c.Parameters.AddWithValue(parmName, kv.Value);
                                c.Parameters[parmName].Value = kv.Value;
                            }
                            else if (c.Parameters.Contains(kv.Key))
                            {
                                x = true;                                
                                c.Parameters[kv.Key].Value = kv.Value;
                            }
                        }
                        c.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch(Exception ex)
            {
                Handle(ex, "Unable to save", ExceptionLevel.UI_Basic);
                return false;
            }
            finally
            {
                if(x)
                    new Alert("Tell Ryan to remove the '@'!").ShowDialog();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void RefreshContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (refreshCmd == null || db == null)
            {
                new Alert("Refresh command or database connection not provided.", Choice: false).ShowDialog();
                return;
            }
            DataSet ds = db.RunCommand(refreshCmd);
            myData = ds.Tables[0];
            if(ds.Tables.Count >= 3)
            {
                excludeList = SetExceptionList(ds.Tables[2]);
            }
            SetupPage();
        }
        public static string[] SetExceptionList(DataTable dt)
        {            
            if (!dt.Columns.Contains("Name"))
                return null;            
            List<string> temp = new List<string>();
            foreach(DataRow r in dt.Rows)
            {
                if (dt.Columns.Contains("Exclude"))
                {
                    try
                    {
                        bool b = (bool)r["Exclude"];
                        if (!b)
                            continue;
                    }catch { }
                }
                string t = r["Name"] as string;
                if (t == null)
                    continue;
                temp.Add(t);
            }
            return temp.ToArray();
            //populate with dt row records if exclude is one if the column exists
        }
    }
}
#pragma warning restore 414