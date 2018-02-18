using SEIDR.Dynamics.Configurations;
using SEIDR.Dynamics.Windows;
using SEIDR.WindowMonitor.MonitorConfigurationHelpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;

namespace SEIDR.WindowMonitor.SettingWindows
{
    /// <summary>
    /// Interaction logic for DefaultConfig_Edit.xaml
    /// </summary>
    public partial class DefaultConfig_Edit : SessionWindow
    {
        //ToDo: Copy this window and configure for the new classes/inheritance hierarchy..
        iConfigList left;
        iConfigList right;
        iConfigListType myTypes;
        string TeamEditing;
        public bool OkContinue = false;
        bool IsAdminMode { get; set; }
        /// <summary>
        /// Open editor for modifying default settings using your own settings. Note: this editor will do the actual updating.
        /// <para>Also note, to get to this page, you must have permissions to edit your own configs, at least.</para>
        /// </summary>
        /// <param name="myList"></param>
        /// <param name="defaultList"></param>
        /// <param name="ConfigType">In case reloading is necessary... should only be the default that might need reloading.</param>
        public DefaultConfig_Edit(iConfigList myList, iConfigListType type, bool AdminMode = true, string Team = null)
        {
            IsAdminMode = AdminMode;
            TeamEditing = Team;
            InitializeComponent();
            DataContext = this;
            left = myList;
            right = SettingManager.LoadDefault(type, Team);
            if(right == null)
            {
                OkContinue = false;
                CanShowWindow = false;
                return;
            }            
            myTypes = type;
            DefaultSetting.ItemsSource = right.MyData.DefaultView;
            MySetting.ItemsSource = left.MyData.DefaultView;
            if(DefaultSetting.Items.Count == 0)
            {
                DefaultSetting.ContextMenu.IsEnabled = false;
            }
            if (MySetting.Items.Count == 0)
                MySetting.ContextMenu.IsEnabled = false;
            if (IsAdminMode)
            {
                Default_Add.Visibility = Visibility.Visible;
                Default_Remove.Visibility = Visibility.Visible;
                Default_UndoRemove.Visibility = Visibility.Visible;
            }
            else
            {
                Default_Add.Visibility = Visibility.Collapsed;
                Default_Remove.Visibility = Visibility.Collapsed;
                Default_UndoRemove.Visibility = Visibility.Collapsed;
            }/*
            Action<DependencyObject> SetVis = (i => {
                i.SetValue(IsVisibleProperty, IsAdminMode);
                BindingOperations.SetBinding(i, IsEnabledProperty, new Binding
                {
                    Path = new PropertyPath("IsAdminMode")
                });
            });
            SetVis(Default_Add);
            SetVis(Default_Remove);*/
            OkContinue = true;
        }
        private string[] GetSelectedNameRight(bool remove = false)
        {
            //var dg = sender as DataGrid;
            var dg = DefaultSetting;
            if (dg.Items.Count == 0)
                return new string[0];
            var q = from DataRowView row in dg.SelectedItems
                        //select row;
                    select row["Name"].ToString();
            var results = q.ToArray();
            if (q.Count() == 0 && dg.SelectedItem != null)
            {
                q = new string[] { ((DataRowView)dg.SelectedItem)["Name"].ToString() };
            }
            if (remove)
            {
                foreach (var record in results)
                {
                    right.Remove(record);
                }
                if (DefaultSetting.Items.Count == 0)                
                    DefaultSetting.ContextMenu.IsEnabled = false;                
            }
            return results;
            /*
            string[] results = new string[ q.Count()];
            int i = 0;
            foreach(var record in q)
            {
                var name = record["Name"].ToString();
                if (remove)
                {
                    //dg.Items.Remove(record);
                    right.Remove(name);
                }
                results[i++] = name;
            }
            return results; /*
            if (dg == null || dg.SelectedIndex < 0)
                return null;
            var dt = ((System.Data.DataView)dg.ItemsSource).ToTable();
            string name = dt.Rows[dg.SelectedIndex]["NAME"] as string;
            if (remove)
            {
                dg.Items.RemoveAt(dg.SelectedIndex);                
            }
            return name; */
        }
        private string[] GetSelectedLeft(bool remove = false)
        {
            //var dg = sender as DataGrid;
            var dg = MySetting;
            if (dg.Items.Count == 0)
                return new string[0];
            var q = from DataRowView row in dg.SelectedItems                    
                    select row["Name"].ToString();
            //return  q.ToArray();
            var results = q.ToArray();
            if (q.Count() == 0  && dg.SelectedItem != null)
            {                
                q = new string[] { ((DataRowView)dg.CurrentItem)["Name"].ToString() };                
            }
            if (remove)
            {
                
                foreach(var record in results)
                {
                    left.Remove(record);
                }                
                if (MySetting.Items.Count == 0)
                    MySetting.ContextMenu.IsEnabled = false;
            }
            return results;
        }
        private void Default_Add_Click(object sender, RoutedEventArgs e)
        {
            string[] selected = GetSelectedLeft();
            if (selected == null || selected.Length==0)
                return;
            var dt = ((DataView)DefaultSetting.ItemsSource).ToTable();
            switch (myTypes)
            {
                case iConfigListType.Query:
                    {
                        Queries q = left as Queries;
                        Queries a = right as Queries;
                        foreach (var name in selected)
                        {
                            a[name] = q[name];
                        }
                        break;
                    }
                case iConfigListType.DatabaseConnection:
                    {
                        DBConnections q = left as DBConnections;
                        DBConnections a = right as DBConnections;
                        foreach(var name in selected)
                        {
                            a[name] = q[name];
                        }
                        break;
                    }
                case iConfigListType.ContextMenu:
                    {
                        ContextMenuItems q = left as ContextMenuItems;
                        ContextMenuItems a = right as ContextMenuItems;
                        foreach(var name in selected) { a[name] = q[name]; }
                        break;
                    }
            }
            DefaultSetting.ItemsSource = right.MyData.AsDataView();
            DefaultSetting.ContextMenu.IsEnabled = true;
            e.Handled = true;
        }
        private bool Check(List<string> nameList, string nameCheck)
        {
            if (!nameList.Contains(nameCheck))
                return true;
            var check = new Alert($"Internal name '{nameCheck}' is already in use in your settings. Continue to Update it?", mode: AlertMode.Confirmation);
            return (check.ShowDialog() ?? false);                                        
        }        
        private void MySetting_Add_Click(object sender, RoutedEventArgs e)
        {
            string[] selected = GetSelectedNameRight();
            if (selected == null || selected.Length == 0)
                return;
            var dt = ((DataView)DefaultSetting.ItemsSource).ToTable(); // ???? i don't remember what this is for...?
            var temp = left.GetNameList();
            switch (myTypes)
            {
                case iConfigListType.Query:
                    {
                        
                        Queries q = left as Queries;                        
                        Queries a = right as Queries;
                        foreach (var name in selected)
                        {
                            if(Check(temp, name))
                                q[name] = a[name];
                        }
                        break;
                    }
                case iConfigListType.DatabaseConnection:
                    {
                        DBConnections q = left as DBConnections;                        
                        DBConnections a = right as DBConnections;
                        foreach (var name in selected)
                        {
                            if(Check(temp, name))
                                q[name] = a[name];
                        }
                        break;
                    }
                case iConfigListType.ContextMenu:
                    {
                        ContextMenuItems q = left as ContextMenuItems;
                        ContextMenuItems a = right as ContextMenuItems;
                        foreach (var name in selected)
                        {
                            if (Check(temp, name))
                                q[name] = a[name];
                        }
                        break;
                    }
                case iConfigListType.SEIDR_MenuAddOn:
                    {
                        SEIDR_MenuAddOnConfigs q = left as SEIDR_MenuAddOnConfigs;
                        SEIDR_MenuAddOnConfigs a = right as SEIDR_MenuAddOnConfigs;
                        foreach(var name in selected)
                        {
                            if (Check(temp, name))
                                q[name] = a[name];
                        }
                        break;
                    }
            }
            MySetting.ItemsSource = left.MyData.AsDataView();
            MySetting.ContextMenu.IsEnabled = true;
            e.Handled = true;
        }

        private void Default_Remove_Click(object sender, RoutedEventArgs e)
        {
            rightOld = right.cloneSetup();
            DefaultSetting.ItemsSource = null;
            GetSelectedNameRight(true);
            DefaultSetting.ItemsSource = right.MyData.AsDataView();
            e.Handled = true;
            
        }

        private void RefreshDefaults_Click(object sender, RoutedEventArgs e)
        {
            DefaultSetting.ItemsSource = null;
            right = SettingManager.LoadDefault(myTypes, TeamEditing);
            DefaultSetting.ItemsSource = right.MyData.DefaultView;
            e.Handled = true;
            rightOld = null;
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            string f = SettingManager.myMiscSettings.ErrorLog;
            string errorMessage = $"Unable to save {myTypes.ToString()} Configurations, aborting save.";
            if (f != null)
                errorMessage += Environment.NewLine + "See the error log for more details";
            switch (myTypes)
            {
                case iConfigListType.ContextMenu:
                    {
                        if (IsAdminMode)
                        {
                            try
                            {
                                SettingManager.SaveDefaultContextMenus(right as ContextMenuItems, TeamEditing);
                            }
                            catch(Exception exc)
                            {
                                sExceptionManager.Handle(exc, errorMessage, SEIDR.Dynamics.ExceptionLevel.UI_Basic);
                                return;
                            }
                        }
                        SettingManager.myContextMenus = left as ContextMenuItems;
                        break;
                    }
                case iConfigListType.DatabaseConnection:
                    {
                        if (IsAdminMode)
                        {
                            try
                            { 
                                SettingManager.SaveDefaultDBConnections(right as DBConnections, TeamEditing);
                            }
                            catch (Exception exc)
                            {
                                sExceptionManager.Handle(exc, errorMessage, SEIDR.Dynamics.ExceptionLevel.UI_Basic);
                                return;
                            }
                        }
                        SettingManager.myConnections = left as DBConnections;
                        break;
                    }
                case iConfigListType.Query:
                    {
                        if (IsAdminMode)
                        {
                            try
                            { 
                                SettingManager.SaveDefaultQuery(right as Queries, TeamEditing);
                            }
                            catch (Exception exc)
                            {
                                sExceptionManager.Handle(exc, errorMessage, SEIDR.Dynamics.ExceptionLevel.UI_Basic);
                                return;
                            }
                        }
                        SettingManager.myQueries = left as Queries;
                        break;
                    }
                case iConfigListType.SEIDR_MenuAddOn:
                    {
                        if (IsAdminMode)
                        {
                            try
                            {
                                SettingManager.SaveDefaultAddons(right as SEIDR_MenuAddOnConfigs, TeamEditing);
                            }
                            catch (Exception exc)
                            {
                                sExceptionManager.Handle(exc, errorMessage, SEIDR.Dynamics.ExceptionLevel.UI_Basic);
                                return;
                            }
                        }
                        SettingManager.myAddons = left as SEIDR_MenuAddOnConfigs;
                        break;
                    }
            }
            SettingManager.Save();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        iConfigList leftOld = null;
        iConfigList rightOld = null;
        private void Mine_Remove_Click(object sender, RoutedEventArgs e)
        {

            //Remove the toDeleteFromBindedList object from your ObservableCollection
            leftOld = left.cloneSetup();
            MySetting.ItemsSource = null;
            GetSelectedLeft(true);
            MySetting.ItemsSource = left.MyData.DefaultView;
            e.Handled = true;
        }

        private void Default_UndoRemove_Click(object sender, RoutedEventArgs e)
        {
            DefaultSetting.ItemsSource = null;
            right = rightOld;
            DefaultSetting.ItemsSource = right.MyData.DefaultView;
            rightOld = null;
        }

        private void Mine_UndoRemove_Click(object sender, RoutedEventArgs e)
        {
            MySetting.ItemsSource = null;
            left = leftOld;
            MySetting.ItemsSource = left.MyData.DefaultView;
            leftOld = null;
        }
    }
}
