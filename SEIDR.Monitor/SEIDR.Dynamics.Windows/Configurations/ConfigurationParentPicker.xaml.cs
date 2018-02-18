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

namespace SEIDR.Dynamics.Configurations
{
    /// <summary>
    /// Interaction logic for ConfigurationParentPicker.xaml
    /// </summary>
    public partial class ConfigurationParentPicker : iNotifyControl
    {
        public bool Required { get; private set; }
        iWindowConfiguration picked = null;
        public iWindowConfiguration Picked
        {
            get { return picked; }
            set { picked = value; InvokeChanged(); }
        }
        WindowConfigurationScope[] scopeList;
        
        public WindowConfigurationScope[] ScopeList { get { return scopeList; } set { scopeList = value; InvokeChanged(); } }
        WindowConfigurationScope currentScope;
        public WindowConfigurationScope CurrentScope { get { return currentScope; } set { currentScope = value; InvokeChanged(); } }

        bool reqOptions = true;
        public bool RequireOptions
        {
            get { return reqOptions; }
            set
            {
                reqOptions = value;
                InvokeChanged();
                InvokeChanged(nameof(HasOptions));
            }
        }        
        public Visibility HasOptions
        {
            get
            {
                if (FullSource.HasMinimumCount(1) || !reqOptions)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
            set { InvokeChanged(); }
        }
        IEnumerable<iWindowConfiguration> FullSource { get; set; }
        //IEnumerable<iWindowConfiguration> FilteredSource { get; set; }
        public ConfigurationParentPicker(IEnumerable<iWindowConfiguration> parents,
            int? CurrentID, WindowConfigurationScope currentScope, bool Required)
            : this(parents, null, CurrentID, currentScope, Required) { }
        public ConfigurationParentPicker(IEnumerable<iWindowConfiguration> parents, 
            string Description, int? CurrentID, WindowConfigurationScope currentScope, bool Required)
            :this()
        {            
            Configure(parents, Description, CurrentID, currentScope, Required);
        }

        public ConfigurationParentPicker()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void Configure(IEnumerable<iWindowConfiguration> parents,
            string Description, int? CurrentID, WindowConfigurationScope currentScope, bool required)
        {
            ScopeList = parents
                .Select(c => c.MyScope)
                .Union(new[] { currentScope, WindowConfigurationScope.UNK })
                .Distinct()
                .ToArray();
            CurrentScope = currentScope;
            if (!Required)
                parents = parents.Union(new[] { new EmptyWindowConfiguration() });
            //If just current scope/unknown, Hide the scope filter because it doesn't matter
            if (ScopeList.Length <= 2)
                ScopeFilter.Visibility = Visibility.Collapsed;
            else if (CurrentID == null)
                CurrentScope = WindowConfigurationScope.UNK; //Not chosen, so use show all scopes

            Required = Required;            
            ParentTypeLabel.Content = Description.nTrim(true) ?? currentScope.GetDescription();

            if (CurrentID == null || CurrentID < 0)
                Picked = null;
            else
                Picked = parents.FirstOrDefault(p => p.ID == CurrentID && p.MyScope == CurrentScope);
            FullSource = parents;
            /*
            FilteredSource = from p in parents
                             where p.MyScope == CurrentScope
                             || CurrentScope == WindowConfigurationScope.UNK
                             select p;   
            */
            ParentListComboBox.ItemsSource = from p in parents
                                             where p.MyScope == CurrentScope
                                             || CurrentScope == WindowConfigurationScope.UNK
                                             select p;
        }
        private void ParentListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ParentPick_Changed(this, new RequiredEventArgs(Picked));
            if (Required && e.AddedItems.Count == 0)
            {
                ParentTypeLabel.BorderBrush = Brushes.Red;
            }
            else
            {
                //ParentTypeLabel.ClearValue(BorderBrushProperty);
                ParentTypeLabel.BorderBrush = Brushes.Black;
                //ParentTypeLabel.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("Black");
            }
            e.Handled = true;
        }
        public event EventHandler ParentPick_Changed;
        private void ScopeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Picked != null 
                && CurrentScope != WindowConfigurationScope.UNK 
                && Picked.MyScope != CurrentScope)
            {
                Picked = null;
                ParentPick_Changed(this, new RequiredEventArgs());
            }
            ParentListComboBox.ItemsSource = from p in FullSource
                                             where p.MyScope == CurrentScope
                                             || CurrentScope == WindowConfigurationScope.UNK
                                             select p;
            e.Handled = true;
        }
    }
}
