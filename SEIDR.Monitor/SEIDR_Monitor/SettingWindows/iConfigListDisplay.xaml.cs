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
using SEIDR.Dynamics.Configurations;
using SEIDR.WindowMonitor.SettingWindows;
using SEIDR.Dynamics;
using System.Data;
using static SEIDR.WindowMonitor.sExceptionManager;
using SEIDR.WindowMonitor.MonitorConfigurationHelpers;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// List of supported iConfigListTypes in the display window
    /// </summary>
    public enum iConfigListType
    {
        Query,
        DatabaseConnection,
        ContextMenu,
        User,
        SEIDR_MenuAddOn
    }
    /// <summary>
    /// Interaction logic for iConfigListDisplay.xaml
    /// </summary>
    public partial class iConfigListDisplay : SessionWindow
    {
        public bool Refresh = false;
        iConfigList myConfig;
        
        iConfigListType myConfigType;
        public iConfigListDisplay(iConfigList myConfigToEdit)
        {
            UseSessionColor = false;
            InitializeComponent();
            Clone.Visibility = myConfigToEdit.Cloneable ? Visibility.Visible : Visibility.Collapsed;
            string s = myConfigToEdit.GetType().Name;
            Title = s;
            if (s == "Queries")
            {
                myConfigType = iConfigListType.Query;
            }
            else if (s == "DBConnections")
            {
                myConfigType = iConfigListType.DatabaseConnection;
            }
            else if (s == "ContextMenuItems")
            {
                myConfigType = iConfigListType.ContextMenu;
            }
            else if (s == "Users")
            {
                myConfigType = iConfigListType.User;
            }
            else if (s == "SEIDR_MenuAddOnConfigs")
                myConfigType = iConfigListType.SEIDR_MenuAddOn;
            else if(!Enum.TryParse(s, out myConfigType))
                throw new Exception("Unsupported Configuration type.");


            myConfig = myConfigToEdit;
            MyDisplayData.ItemsSource = myConfig.MyData.DefaultView;
        }
        /// <summary>
        /// Get the name of the first selected item in the config manager
        /// </summary>
        /// <param name="remove">If true, also remove it from the GUI's datagrid</param>
        /// <returns></returns>
        private string GetSelectedName(bool remove = false)
        {
            //var dg = sender as DataGrid;
            var dg = MyDisplayData;
            if (dg == null || dg.SelectedIndex < 0)
                return null;
            var dt = ((DataView)dg.ItemsSource).ToTable();
            string name = dt.Rows[dg.SelectedIndex]["NAME"] as string;
            if (remove)
            {
                dt.Rows.RemoveAt(dg.SelectedIndex);
                //dg.Items.RemoveAt(dg.SelectedIndex);
                dg.ItemsSource = null;
                dg.ItemsSource = dt.AsDataView();
            }
            return name;
        }
        private DataRow GetSelectedRow()
        {
            var dg = MyDisplayData;
            if (dg == null || dg.SelectedIndex < 0)
                return null;
            DataRowView drv = dg.SelectedItem as DataRowView;
            return drv.Row;
        }

        private void OpenRecordEditor(object sender, bool TryEdit = true)
        {            
            string x = GetSelectedName(); //sender);
            if (TryEdit && x == null)
                return;
            switch (myConfigType)
            {
                case iConfigListType.Query:
                    {
                        var ql = myConfig as Queries;
                        Query q = TryEdit? ql[x] : null;
                        //QueryAdder qa = new QueryAdder(q);
                        QueryEditor qa = new QueryEditor(q);
                        var r = qa.ShowDialog(false);
                        if (r.HasValue && r.Value)
                        {
                            var newItem = qa.result; 
                            ql[TryEdit ? x: newItem.Name] = newItem;
                            Refresh = true;
                        }
                        break;
                    }
                case iConfigListType.DatabaseConnection:
                    {
                        var dbl = myConfig as DBConnections;
                        DBConnection db = TryEdit ? dbl.Get(x) : null;
                        DBConnectioneditor dbe = new DBConnectioneditor(db);
                        var r = dbe.ShowDialog(false);
                        if (r.HasValue && r.Value)
                        { 
                            if(TryEdit)
                                dbl[x] = dbe.myConnection/*.InternalDBConn*/;
                            else                            
                                dbl.Add(dbe.myConnection); //New needs to add with Name
                            
                            Refresh = true;
                        }
                        break;
                    }
                case iConfigListType.ContextMenu:
                    {
                        var cml = myConfig as ContextMenuItems;
                        ContextMenuItem cm = TryEdit? cml[x]: null;                        
                        ContextMenuEditor cme = new ContextMenuEditor(cm);
                        var r = cme.ShowDialog(false);
                        if (r.HasValue && r.Value) {
                            var newItem = cme.myItem; 
                            cml[TryEdit? x : newItem.Name] = newItem;
                            Refresh = true;
                        }
                        break;
                    }
                case iConfigListType.User:
                    {
                        var ul = myConfig as Users;
                        User u = TryEdit ? ul[x] : null;
                        UserEditor ue = new UserEditor(u);
                        var r = ue.ShowDialog(false);
                        if(r.HasValue && r.Value)
                        {
                            var newItem = ue.item;
                            ul[TryEdit? x: newItem.Name] = newItem;                            
                            SettingManager.SaveUsers(ul); //Because this is always on network, always try to save
                        }
                        break;
                    }
                case iConfigListType.SEIDR_MenuAddOn:
                    {
                        var ul = myConfig as SEIDR_MenuAddOnConfigs;
                        //Idea: have a window for general configuration and that can open editable dashboard to edit the dictionary of parameters
                        SEIDR_MenuAddOnConfiguration c = TryEdit ? ul[x] : null;
                        AddonEditor ae = new AddonEditor(c);
                        if (!ae.OkToContinue)
                            break;
                        var r = ae.ShowDialog(false);
                        //var edd = new Ryan_UtilityCode.Dynamics.Windows.EditableDashboardDisplay(Dynamics.CreateDataRowView(c), "Addons");
                        //var r = edd.ShowDialog();
                        if (r.HasValue && r.Value)
                        {
                            var add = ae.myItem;
                            
                            //DataRow newItem = null;
                            //var add = Dynamics.CreateInstance<SEIDR_MenuAddOnConfiguration>(newItem);
                            ul[TryEdit ? x : add.Name] = add;
                            Refresh = true;
                        }
                        break;
                    }
            }
            MyDisplayData.ItemsSource = null;
            MyDisplayData.ItemsSource = myConfig.MyData.DefaultView;
        }
        private void MyData_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenRecordEditor(sender, true);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Refresh)
                SettingManager.SaveRefresh();
            DialogResult = true;
            this.Close();            
            //iConfig
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            OpenRecordEditor(sender);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            string name = GetSelectedName(remove:true); //sender);
            myConfig.Remove(name);
            
            Refresh = true;
        }        
        private void Create_Click(object sender, RoutedEventArgs e)
        {
            OpenRecordEditor(sender, false);

            //MyDisplayData.ItemsSource = myConfig.MyData.DefaultView; //Reset in the 
        }

        private void Clone_Click(object sender, RoutedEventArgs e)
        {
            DataRow source = GetSelectedRow();
            if (source == null)
                return;
            string name = GetSelectedName();
            if (name.ToUpper().StartsWith("SW_"))
            {
                Handle("Cannot clone Switches.");
                return;
            }
            CloneNameConfirm cnc = new CloneNameConfirm(name);
            var r = cnc.ShowDialog();
            if(r.HasValue && r.Value)
            {
                switch (myConfigType)
                {
                    case iConfigListType.ContextMenu:
                        {
                            ContextMenuItem orig = (myConfig as ContextMenuItems)[name];
                            var cmi = orig.XClone();
                            //ContextMenuItem cmi = Dynamics.CreateInstance<ContextMenuItem>(source);
                            cmi.Name = cnc.cloneName; 
                            (myConfig as ContextMenuItems).Add(cmi);
                            Refresh = true;
                            break;
                        }
                    case iConfigListType.Query:
                        {
                            Query orig = (myConfig as Queries)[name] as Query;
                            //Query q = Dynamics.CreateInstance<Query>(source);
                            Query q = orig.XClone();
                            {
                                q.Name = cnc.cloneName;
                            }                            
                            (myConfig as Queries).Add(q);
                            Refresh = true;
                            break;
                        }
                    default:
                        {
                            Handle("Configuration is Flagged as cloneable but handling is not implemented");
                            break;
                        }
                }
                MyDisplayData.ItemsSource = null;
                MyDisplayData.ItemsSource = myConfig.MyData.DefaultView;
            }
        }

        private void MyDisplayData_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string col = e.PropertyName.ToUpper();
            if (col == "NAME" || col == "ID")
                e.Column.Visibility = Visibility.Collapsed;
            
        }
    }
}
