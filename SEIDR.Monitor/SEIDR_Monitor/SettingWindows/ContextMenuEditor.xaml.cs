using SEIDR.Dynamics.Windows;
using SEIDR;
using System;
using System.Data;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using SEIDR.Dynamics;
using SEIDR.Dynamics.Configurations;
using static SEIDR.Dynamics.Configurations.ConfigListHelper;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for ContextMenuEditor.xaml
    /// </summary>
    public partial class ContextMenuEditor : SessionWindow
    {
        bool NewRecord = false;
        public ContextMenuItem myItem;        
        List<ContextMenuHelper> OwnerList;
        List<ContextMenuHelper> SwitchList;
        public ContextMenuEditor(ContextMenuItem cmi = null)
            :base(true)
        {
            InitializeComponent();
            UseSessionColor = false;
            if (cmi == null)
            {
                NewRecord = true;
                /*
                cmi = new ContextMenuItem
                {
                    DataRowMappings = new List<string>()
                };*/
                myItem = new ContextMenuItem();
            }
            else
            {
                if (cmi.AddOn != null && SettingManager.myLibrary?.IsValidState != true)
                {
                    CanShowWindow = false;
                    Handle("Cannot show context editor when the addon Library is in an invalid state.", ExceptionLevel.UI_Basic);
                    return;
                }
                if (cmi.IsSwitch)
                {
                    CanShowWindow = false;
                    Addon.IsEnabled = false;
                    Standard.IsEnabled = false;
                    Handle("Cannot edit switches. Delete and create a new switch.", ExceptionLevel.UI_Basic);
                    CanShowWindow = false;
                    return;
                }
                else
                    Switch.IsEnabled = false;
                
                myItem = cmi.XClone();                
            }            
            DataContext = myItem;
            OwnerList = ContextMenuHelper.GetList(SettingManager.myContextMenus, SettingManager.myQueries, SettingManager.myAddons);
            SwitchList = ContextMenuHelper.GetSubList(OwnerList, Scope.Q);
            Addon_Box.ItemsSource = SettingManager.myLibrary?.GetAppInfoMini(); //.GetAddonList();
            if (!SettingManager._ME.CanUseAddons || !SettingManager._ME.CanEditContextAddons) //If can't edit context addons, hide the tab.
            {
                Addon.Visibility = Visibility.Collapsed;
                if(!string.IsNullOrWhiteSpace(cmi.AddOn))
                {
                    Handle("Not allowed to Edit Addon Context Menu.");
                    CanShowWindow = false;
                    return;
                }
            }
            else if (myItem.AddOn != null)
                ContextTabs.SelectedIndex = 1; //Don't worry about permission here because they have to have seen the addon from the context display first
            if (Addon_Box.Items.Count == 0)
                Addon.IsEnabled = false;

            //List<string> PossibleOwners = iConfigManager.myQueries.GetNameList();
            //PossibleOwners.AddRange(iConfigManager.myContextMenus.GetNameList());
            //PossibleOwners.AddRange(iConfigManager.myContextMenus.GetDashboardList());
            if (cmi != null)
            {
                //MyName.Text = cmi.Name;
                //MyName_Copy.Text = cmi.Name;
                //MyName.IsEnabled = false;
                //MyName_Copy.IsEnabled = false;
                myItem.Name = cmi.Name;
                MultiSelect.IsChecked = cmi.MultiSelect;
                MultiSelect_Copy.IsChecked = cmi.MultiSelect;
                Dashboard.Text = cmi.Dashboard ?? "";
                if (cmi.AddOn != null)
                {
                    Addon_Box.SelectedIndex = Addon_Box.Items.IndexOf(cmi.AddOn);
                    ParameterMapper.Visibility = cmi.ParameterInfo == null ? Visibility.Hidden : Visibility.Visible;
                }
                //this.HardDataMapping(cmi.Mappings);
                //this.DataRowMapping.SetupList(cmi.DataRowMappings); //bindings?
                //this.Owner. = cmi.owner;
                //OpenCol.Text = cmi.ColumnOpen;
                

                Procedure.Text = cmi.Procedure;
                ProcID.Value = cmi.ProcID;
                //AddonProcID.Value = cmi.ProcID;
                //AddonProcString.Text = cmi.ProcIDParameterName;
                ProcIDName.Text = cmi.ProcIDParameterName;
                //PossibleOwners.Remove(cmi.Name); //Cannot own self
                SingleDetail.IsChecked = cmi.SingleDetail;
                Display.Text = cmi.DisplayName;
                Display_Copy.Text = cmi.DisplayName;
                DetailAccept.Text = cmi.DetailChangeProc;
                int x = -1; 
                bool foundOwner = false; 
                bool foundSelf = false;
                
                for (int i = 0; i < OwnerList.Count; i++)
                {
                    var h = OwnerList[i];
                    if (h.Name == cmi.Name)
                    {
                        foundSelf = true;
                        OwnerList.RemoveAt(i); //Self is not eligible
                        if (foundOwner)
                            break;
                        i--;
                        continue;
                    }
                    if (h.Name == cmi.owner)
                    {
                        x = i;
                        foundOwner = true;
                        if (foundSelf)
                            break;
                    }
                }
                //this.OwnerBox.ItemsSource = PossibleOwners;
                OwnerBox.ItemsSource = OwnerList;
                OwnerBox_Copy.ItemsSource = OwnerList;                
                //int x = PossibleOwners.IndexOf(cmi.owner);
                if (cmi.owner != null && x >= 0)
                {
                    OwnerBox.SelectedIndex = x;
                    OwnerBox_Copy.SelectedIndex = x;
                }
            }
            else
            {
                Source.ItemsSource = SwitchList;
                Target.ItemsSource = SwitchList; //Only needs to be in the newRecord area..
                OwnerBox.ItemsSource = OwnerList;
                Accept.IsEnabled = false;
                Accept1.IsEnabled = false;
                OwnerBox_Copy.ItemsSource = OwnerList;
            }
            
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            if (Display.Text.nTrim(true) == null)
            {
                new Alert("Display Name cannot be empty").ShowDialog();
                return;
            }
            string n = NewRecord ? GetIDName(Scope.CM) : myItem.Name; //If existing record, get the cloned name.
            while (NewRecord && SettingManager.myContextMenus[n] != null)
            {
                n = GetIDName(Scope.CM);
            }
            /*
            string n = MyName.Text.nTrim(true);
            if (n != null && !n.ToUpper().StartsWith("CM_"))
                MyName.Text = "CM_" + n;
            if(n == null || !n.ToUpper().StartsWith("CM_")){
                new Alert("Invalid Name: " + n ?? "(NULL)", Choice: false).ShowDialog();
                return;
            }
            if(NewRecord && SettingManager.myContextMenus[n] != null)
            {
                Handle($"Context menu named '{n}' already exists.", ExceptionLevel.UI_Basic);
                return;
            }*/
            string x = Dashboard.Text.nTrim(true);
            //if (x != null && !x.ToUpper().StartsWith("D_"))
            //    x = "D_" + x;
            if(x != null && (MultiSelect.IsChecked??false))
            {
                var r = new Alert("Really enable MultiSelect for a context menu with a Dashboard? The dashboard may be opened once for each row.", true, mode: AlertMode.Confirmation).ShowDialog();
                if (! r ?? false) //Abort, return.
                    return;                
            }
            else if(x == null && ( SingleDetail.IsChecked?? false))
            {
                new Alert("Uncheck single Detail or Name the Dashboard", Choice: false).ShowDialog();
                return;
            }
            try
            {
                myItem.Name = n;
                myItem.MultiSelect = MultiSelect.IsChecked ?? false;
                myItem.SingleDetail = SingleDetail.IsChecked?? false;
                myItem.Procedure = Procedure.Text.nTrim(true);
                //myItem.ColumnOpen = OpenCol.Text.nTrim(true);   //Replace with an Addon.              
                myItem.Dashboard = x;
                myItem.ProcID = (int?)ProcID.Value;
                myItem.ProcIDParameterName = ProcIDName.Text.nTrim(true);
                myItem.DetailChangeProc = DetailAccept.Text.nTrim(true);
                myItem.DisplayName = Display.Text;
                myItem.AddOn = null;
                myItem.Target = null;
                myItem.UseQueue = x == null && (UseQueue.IsChecked ?? false);
                //myItem.Mappings = GetHardCodedMapping(); 
                //HardDataMapping.Items as Dictionary<string, object>;//new Dictionary<string,object>(HardDataMapping.MyValues);
                //myItem.DataRowMappings = new List<string>(DataRowMapping.MyList); //Binding-ed
                //myItem.owner = this.Owner.Text;


                //myItem.owner = OwnerBox.SelectedItem.ToString(); //ToString = Name
                ContextMenuHelper cm = OwnerBox.SelectedItem as ContextMenuHelper;
                myItem.owner = cm.Name;
                myItem.OwnerScope = cm.myScope;
            }
            catch (Exception ex)
            {
                new Alert(ex.Message, false, false).ShowDialog();
                return;
            }
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        /*
         //Replace by doing CommandBuilder and populating parameters from the datarow. Straight mappings first, then procID,
         //then hdn columns, then dtl_ columns
         <Button Name="DataRowMapping" Grid.Row="4" Grid.Column="1" Click="DataRowMapping_Click"
                            ToolTip="Map Column Names to stored procedure Parameters"
                            Margin="5,5" Height ="35" HorizontalAlignment="Left">Edit Data Row - Parameter Mappings</Button>
        private void DataRowMapping_Click(object sender, RoutedEventArgs e)
        {
            ContextMapping cm = new ContextMapping(myItem.DataRowMappings);
            var r = cm.ShowDialog();
            if (r.Value)
            {
                myItem.DataRowMappings = cm.myList;
            }
        }*/

        private void SingleDetail_Checked(object sender, RoutedEventArgs e)
        {
            if (SingleDetail.IsChecked.HasValue && SingleDetail.IsChecked.Value)
                MultiSelect.IsChecked = false;
        }

        private void MultiSelect_Checked(object sender, RoutedEventArgs e)
        {
            if (MultiSelect.IsChecked.HasValue && MultiSelect.IsChecked.Value)
                SingleDetail.IsChecked = false;
        }
        #region scoping
        private void Scope_ALL_Click(object sender, RoutedEventArgs e)
        {
            
            OwnerBox.ItemsSource = OwnerList;
            OwnerBox.SelectedIndex = ContextMenuHelper.GetOwnerIndex(OwnerList, myItem.owner);
            OwnerBox_Copy.ItemsSource = OwnerList;
            OwnerBox_Copy.SelectedIndex = ContextMenuHelper.GetOwnerIndex(OwnerList, myItem.owner);
        }
        private void Scope_A_Click(object sender, RoutedEventArgs e)
        {
            SetScope(Scope.A);
        }

        private void Scope_Q_Click(object sender, RoutedEventArgs e)
        {
            SetScope(Scope.Q);
        }

        private void Scope_D_Click(object sender, RoutedEventArgs e)
        {
            SetScope(Scope.D);
        }

        private void Scope_C_Click(object sender, RoutedEventArgs e)
        {
            SetScope(Scope.CM);
        }
        private void SetScope(Scope newScope)
        {
            var c = ContextMenuHelper.GetSubList(OwnerList, newScope);
            OwnerBox.ItemsSource = c;
            OwnerBox.SelectedIndex = ContextMenuHelper.GetOwnerIndex(c, myItem.owner);
            OwnerBox_Copy.ItemsSource = c;
            OwnerBox_Copy.SelectedIndex = ContextMenuHelper.GetOwnerIndex(c, myItem.owner);
        }
        #endregion
        private void OwnerBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Action<ComboBox, bool> check =((r, b) => 
            {
                if (r == OwnerBox)
                    Accept.IsEnabled = b;
                else if(r == Source || r == Target)
                {
                    if (b)
                    {

                        b = Source.SelectedIndex >= 0 && Target.SelectedIndex >= 0;                        
                        if(b 
                            && (Source.SelectedItem as ContextMenuHelper).Name 
                                == (Target.SelectedItem as ContextMenuHelper).Name
                            )
                        {
                            if (r == Source)
                                Target.SelectedIndex = -1;
                            else
                                Source.SelectedIndex = -1;
                            b = false;
                        }                        
                    }                    
                    SW_accept.IsEnabled = b;
                }                    
                else
                    Accept1.IsEnabled = b;
            });
            var ob = sender as ComboBox;
            if (ob == null)
                return;
            if (ob.SelectedIndex < 0)
            {
                check(ob, false);
                return;
            }
            check(ob, true);

            ContextMenuHelper c = ob.SelectedItem as ContextMenuHelper;
            if (c == null)
                return;
            ob.ToolTip = c.Description;
        }

        private void Accept1_Click(object sender, RoutedEventArgs e)
        {
            if (Display_Copy.Text.nTrim(true) == null)
            {
                new Alert("Display Name cannot be empty").ShowDialog();
                return;
            }
            if (myItem.AddOn == null) //Populated via combo box change
            {
                new Alert("Choose an addon to load with this context menu.", Choice: false).ShowDialog();
                return;
            }
            //string n = MyName_Copy.Text.nTrim(true);
            //if (n != null && !n.ToUpper().StartsWith("CM_"))
            //    MyName.Text = "CM_" + n;
            /*
            if (n == null || !n.ToUpper().StartsWith("CM_"))
            {
                new Alert("Invalid Name: " + n ?? "(NULL)", Choice: false).ShowDialog();
                return;
            }*/
            string n = NewRecord ? GetIDName(Scope.CM) : myItem.Name; //If existing record, get the cloned name.
            while(NewRecord && SettingManager.myContextMenus[n] != null)
            {
                n = GetIDName(Scope.CM);
            }
            /*
            if (NewRecord && SettingManager.myContextMenus[n] != null)
            {
                Handle($"Context menu named '{n}' already exists.", ExceptionLevel.UI_Basic);
                return;
            }*/
            
            try
            {
                if(NewRecord)
                    myItem.Name = n;
                myItem.MultiSelect = MultiSelect_Copy.IsChecked.Value;
                myItem.SingleDetail = false;
                myItem.Procedure = null;
                myItem.Target = null;
                myItem.UseQueue = false;
                //myItem.ColumnOpen = null;
                myItem.Dashboard = null;
                
                //myItem.ProcID = (int?) AddonProcID.Value;
                //myItem.ProcIDParameterName = AddonProcString.Text.nTrim(true);
                myItem.DisplayName = Display_Copy.Text;
                ContextMenuHelper cm = OwnerBox_Copy.SelectedItem as ContextMenuHelper;
                myItem.owner = cm.Name;
                myItem.OwnerScope = cm.myScope;
            }
            catch (Exception ex)
            {
                //new Alert(ex.Message, false, false).ShowDialog();
                Handle(ex, "Unable to complete Menu Set up..." + ex.Message);
                return;
            }
            DialogResult = true;
            Close();
        }

        private void Addon_Box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var info = Addon_Box.SelectedItem as MiniAddonMetaData;
            if(info == null)
            {
                myItem.AddOn = null;
                return;
            }
            if (myItem.AddOn != info.Name)
                myItem.ParameterInfo = null; //changing addon, reset the parameter info stored
            myItem.AddOn = info.Name;
            Addon_Box.ToolTip = info.Description;
            var addon = SettingManager.myLibrary.GetAppInfo(info.Name, info.ID, MyBasicUser);
            /*
            if(addon.IDValueParameterName != null)
            {
                AddonProcIDName.Content = addon.IDValueParameterName;
                AddonProcID.Visibility = Visibility.Visible;
                AddonProcIDName.Visibility = Visibility.Visible;
                AddonProcID.ToolTip = addon.IDValueParameterTooltip;
            }
            else
            {
                AddonProcID.Visibility = Visibility.Collapsed;
                AddonProcIDName.Visibility = Visibility.Collapsed;
            }
            if(addon.IDNameParameterName != null)
            {
                AddonProcStringName.Content = addon.IDNameParameterName;
                AddonProcStringName.Visibility = Visibility.Visible;
                AddonProcString.Visibility = Visibility.Visible;
                AddonProcString.ToolTip = addon.IDNameParameterTooltip;
            }
            else
            {
                AddonProcString.Visibility = Visibility.Collapsed;
                AddonProcStringName.Visibility = Visibility.Collapsed;
            } */
            if (addon.NeedParameterMapping)
            {
                ParameterMapper.Visibility = Visibility.Visible;
            }
            else
            {
                ParameterMapper.Visibility = Visibility.Hidden;
            }
            if (addon.MultiSelect)
            {
                MultiSelect_Copy.IsEnabled = true;
                MultiSelect_Copy.IsChecked = true;
                MultiSelect_Copy.ToolTip = "All rows selected will be passed to addon.";
                MultiSelect_Copy.Visibility = Visibility.Visible;
            }
            else
            {
                MultiSelect_Copy.IsChecked = false;
                MultiSelect_Copy.IsEnabled = false;
                MultiSelect_Copy.ToolTip = "No more than one row will be passed to addon";
            }
        }

        private void Display_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Dashboard_Loaded(object sender, RoutedEventArgs e)
        {

        }

        DataRowView row = null;
        private void ParameterMapper_Click(object sender, RoutedEventArgs e)
        {
            if (row == null)
            {
                
                var wa = SettingManager.myLibrary.GetApp( myItem.AddOn, "", MyBasicUser );
                if(wa != null)
                {
                    row = wa.GetParameterInfo();
                    if(myItem.ParameterInfo != null)
                    {
                        foreach(var info in myItem.ParameterInfo)
                        {
                            row.Row[info.Key] = info.Value ?? DBNull.Value; 
                        }
                    }
                }
                if (row == null)
                {
                    Handle("Context Addon parameter info not provided!");
                    return;
                }
            }
            EditableDashboardDisplay edd = new EditableDashboardDisplay(row, "Set Parameter mapping");
            var b = edd.ShowDialog();
            if( b ?? false)
            {
                row = edd.myDataRowView;

            }
        }

        private void SW_accept_Click(object sender, RoutedEventArgs e)
        {
            
            ContextMenuHelper s = Source.SelectedItem as ContextMenuHelper;
            ContextMenuHelper t = Target.SelectedItem as ContextMenuHelper;
            
            string Message = string.Empty;
            if(s == null )            
                Message = "Missing Source";                            
            if (t == null)
            {
                if (s == null)
                    Message += Environment.NewLine;
                Message += "Missing Target";
            }
            if(Message != string.Empty)
            {
                Handle(Message, ExceptionLevel.UI_Basic);
                return;
            }
            
            myItem.owner = s.Name;
            myItem.Target = t.Name;
            myItem.DisplayName = $"SWITCH {t.DisplayName}({DateTime.Now.ToString("MMM dd, yyyy")})";
            string check = $"SW_{s.Name}_{t.Name}";
            if (SettingManager.myContextMenus[check] != null)
            {
                SettingManager.myContextMenus[check].DisplayName = myItem.DisplayName;
                Handle("Silently updating Switch Display name - already exists", ExceptionLevel.Background);
                DialogResult = false;
                Close();
                return;
            }
            myItem.Name = check;
            myItem.OwnerScope = Scope.SW; //Parent is the special 'Switches' menu item
            DialogResult = true;
            Close();
        }

        private void Switch_Selected(object sender, RoutedEventArgs e)
        {
            this.ContextMenu.IsEnabled = false; 
            // Better way might be to switch to a view model that can have a property
            //implementing INotifyPropertyChanged
        }

        private void Switch_Unselected(object sender, RoutedEventArgs e)
        {
            this.ContextMenu.IsEnabled = true;
        }

        private void DetailAccept_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsActive)
                return;
            if(DetailAccept.Text.Trim() == "")
            {
                UseQueue.IsEnabled = true;
            }
            else
            {
                UseQueue.IsChecked = false;
                UseQueue.IsEnabled = false;
            }
        }

        private void Dashboard_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(Dashboard.Text.Trim() == "" && IsActive)
            {
                DetailAccept.Text = "";
                SingleDetail.IsChecked = false;
            }
        }
    }
}
