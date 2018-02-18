using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using SEIDR.Dynamics;
using SEIDR.Dynamics.Configurations;
using SEIDR.WindowMonitor.MonitorConfigurationHelpers;
using System.Data;
using static SEIDR.WindowMonitor.SettingManager;

using SEIDR.Dynamics.Windows;

namespace SEIDR.WindowMonitor
{
    class MetaDataDescriber
    {
        public MetaDataDescriber(SEIDR_WindowMenuAddOn_MetaData c) { content = c;  Singleton = c.Singleton; }
        public SEIDR_WindowMenuAddOn_MetaData content { get; private set; }
        public bool NeedsMapping { get { return content.HasParameterMapping; } }
        public bool Singleton { get; private set; }
        public override string ToString()
        {
            return content.Name;
        }
    }
    /// <summary>
    /// Interaction logic for ContextMenuEditor.xaml
    /// </summary>
    public partial class AddonEditor : SessionWindow
    {
        public SEIDR_MenuAddOnConfiguration myItem;
        public bool OkToContinue = false;
        bool NewRecord = false;
        SEIDR_WindowMenuAddOn_MetaData[] metadata;
        MetaDataDescriber currentAddon = null;
        public AddonEditor(SEIDR_MenuAddOnConfiguration config = null)
        {
            /*
                This page should be used for choosing the Display names, database connection, addon, etc for the Addon. 
                An 'Edit specifics' button should open EDD to set up the additional parameters from the dictionary.
                Saving should set details on the addon config from the MetaData. Maybe change the addon configuration classes to 
                grab the meta data instead of storing information in the configuration? In order to reduce chance of  incorrect data being used due to 
                being out of date.
            */
            InitializeComponent();
            UseSessionColor = false;

            if (config == null)
            {
                NewRecord = true;
                myItem = new SEIDR_MenuAddOnConfiguration();
            }
            else
                myItem = config.DClone(); //Safe to use this deep clone with
            DataContext = myItem;
            metadata = myMenuLibrary.GetMetaData(MyCurrentUser.AsBasicUser());

            DB_List.ItemsSource = myConnections.DBConns;

            //OwnerList = ContextMenuHelper.GetList(iConfigManager.myContextMenus, iConfigManager.myQueries);                                    

            //List<string> PossibleOwners = iConfigManager.myQueries.GetNameList();
            //PossibleOwners.AddRange(iConfigManager.myContextMenus.GetNameList());
            //PossibleOwners.AddRange(iConfigManager.myContextMenus.GetDashboardList());
            if (config != null)
            {
                MyName.Text = config.Name;                
                MyName.IsEnabled = false;
                
                var b = myMenuLibrary.GetMetaData(MyCurrentUser.AsBasicUser(), config.AddonName);
                if(b == null)
                {
                    Exception log = new Exception("User:" + MyCurrentUser.AsBasicUser().SerializeToXML());
                    log = new Exception("Addon Information:" + config.SerializeToXML(), log);
                    log = new Exception("Basic user information and Addon information have been recorded to log", log);
                    Handle(log, "Unable to Load Plugin Metadata for editing", SEIDR.Dynamics.ExceptionLevel.UI_Basic);
                    return;
                }
                currentAddon = new MetaDataDescriber(b);
                AddonCombo.Items.Add(currentAddon);
                AddonCombo.SelectedIndex = 0;
                AddonCombo.IsEnabled = false;                
                Display.Text = config.DisplayName;
                DB_List.SelectedIndex = SettingManager.myConnections.GetIndex(config.DBConnection);
                //MultiSelect.IsChecked = cmi.MultiSelect;
                //MultiSelect_Copy.IsChecked = cmi.MultiSelect;
                //Dashboard.Text = cmi.Dashboard ?? "";
                //if(cmi.AddOn != null)
                //    Addon_Box.SelectedIndex = Addon_Box.Items.IndexOf(cmi.AddOn);
                ////this.HardDataMapping(cmi.Mappings);
                ////this.DataRowMapping.SetupList(cmi.DataRowMappings); //bindings?
                ////this.Owner. = cmi.owner;
                ////OpenCol.Text = cmi.ColumnOpen;
                //Procedure.Text = cmi.Procedure;
                //ProcID.Value = cmi.ProcID;
                //AddonProcID.Value = cmi.ProcID;
                //AddonProcString.Text = cmi.ProcIDParameterName;
                //ProcIDName.Text = cmi.ProcIDParameterName;
                ////PossibleOwners.Remove(cmi.Name); //Cannot own self
                //SingleDetail.IsChecked = cmi.SingelDetail;
                //Display.Text = cmi.DisplayName;
                //Display_Copy.Text = cmi.DisplayName;
                //DetailAccept.Text = cmi.DetailChangeProc;
                Accept.IsEnabled = true;
            }
            else
            {
                List<MetaDataDescriber> src = new List<MetaDataDescriber>();
                foreach (var md in metadata)
                {
                    src.Add(new MetaDataDescriber(md));
                }
                AddonCombo.ItemsSource = src;
                ParameterMapping.IsEnabled = false;
                Accept.IsEnabled = false;                
            }
            OkToContinue = true;
        }

        private void MapParameter(DataRow r)
        {            
            foreach (DataColumn col in r.Table.Columns)
            {
                //myItem.ParameterInfo[col.ColumnName] = r[col.ColumnName];
                myItem[col.ColumnName] = r[col.ColumnName];
            }
        }
        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            string n = MyName.Text.nTrim(true);
            if (n != null && !n.ToUpper().StartsWith("A_"))
                MyName.Text = "A_" + n;
            if(n == null || !n.ToUpper().StartsWith("A_")){
                new Alert("Invalid Name: " + n ?? "(NULL)", Choice: false).ShowDialog();
                return;
            }
            if (NewRecord && SettingManager.myAddons[n] != null)
            {
                Handle($"Plugin named '{n}' already exists.", SEIDR.Dynamics.ExceptionLevel.UI_Basic);
                return;
            }

            try
            {
                myItem.Name = n;
                myItem.DisplayName = Display.Text;
                myItem.DBConnection = DB_List.SelectedItem.ToString();
                //myItem.IsConfigurationSingleton; //set when choosing the addon combo box item
                //myItem.AddonName;// same as above
                //myItem.ParameterInfo; // Set by the mapping.
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

        
         
        private bool CheckAccept(bool checkMapped = true)
        {
            //if name or display name or maybe some other conditions are empty, return false. otherwise return true.
            if (string.IsNullOrWhiteSpace(MyName.Text))
                return false;
            if (string.IsNullOrWhiteSpace(Display.Text))
                return false;
            if (currentAddon == null || currentAddon.NeedsMapping && myItem.ParameterInfo.Count == 0)
                return false;
            //if (!mapped && checkMapped)
            //    return false;
            if (DB_List.SelectedIndex < 0)
                return false;
            if (AddonCombo.SelectedIndex < 0)
                return false;
            return true;
        }
        private void AddonCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            var ob = sender as ComboBox;
            Accept.IsEnabled = false;
            
            if (ob == null || ob.SelectedIndex < 0)
            {
                //mapped = false;
                return;
            }
            MetaDataDescriber mdd = ob.SelectedItem as MetaDataDescriber;
            currentAddon = mdd;
            if (mdd == null)
            {
                //mapped = false;
                return;
            }

            if (!mdd.content.HasParameterMapping) //Meta data has no parameters, accept may be allowed and we clear the parameter info
            {
                Accept.IsEnabled = CheckAccept(false);
                //mapped = true;
                
                //mapped = CheckAccept(false); //change based on other properties
                myItem.ParameterInfo.Clear();
            }
            else if (mdd.content.Name == myItem.AddonName)
            {
                Accept.IsEnabled = CheckAccept(false); //keep existing parameter info
                //no change
            }
            else
            {
                Accept.IsEnabled = false; //Addon has changed, do not allow accept, clear parameter info
                myItem.ParameterInfo.Clear();
                //mapped = false;
                ParameterMapping.IsEnabled = true;
            }
            myItem.AddonName = mdd.content.Name;
            
        }

        private void ParameterMapping_Click(object sender, RoutedEventArgs e)
        {
            var content = (AddonCombo.SelectedItem as MetaDataDescriber).content;
            DataTable dt = new DataTable();
            var a = myMenuLibrary.GetAddOn(MyCurrentUser.AsBasicUser(), content.Name);
            foreach(var kv in a.GetParameterInfo())
            {
                dt.Columns.Add(new DataColumn(kv.Key, kv.Value));
            }
            var row = dt.NewRow();
            if(myItem.ParameterInfo != null)
            {
                foreach(var kv in myItem.ParameterInfo)
                {
                    if (dt.Columns.Contains(kv.Key))
                    {
                        try { row[kv.Key] = kv.Value; }
                        catch { continue; }
                    }
                }
            }
            dt.Rows.Add(row);
            EditableDashboardDisplay edd = new EditableDashboardDisplay(dt, content.Name + "- Addon Parameters");
            var r = edd.ShowDialog();
            if (r ?? false)
            {
                myItem.ParameterInfo.Clear();
                var drv = edd.myDataRowView;
                var cl = drv.DataView.Table.Columns;
                foreach (DataColumn c in cl)
                {
                    myItem[c.ColumnName] = drv[c.ColumnName];
                }
                //mapped = true;
                Accept.IsEnabled = CheckAccept();
            }
            else if (content.HasParameterMapping && AddonCombo.IsEnabled)
                Accept.IsEnabled = false;
        }

        private void MyName_TextChanged(object sender, TextChangedEventArgs e)
        {
            Accept.IsEnabled = CheckAccept();
        }

        private void Display_TextChanged(object sender, TextChangedEventArgs e)
        {
            Accept.IsEnabled = CheckAccept();
        }
    }
}
