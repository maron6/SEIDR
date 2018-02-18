using SEIDR.Dynamics.Configurations;
using SEIDR.Dynamics.Windows;
//using SEIDR.Extensions;
//using SEIDR.Processing.Data.DBObjects;
using SEIDR.DataBase;
using System.Windows;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for DBConnectioneditor.xaml
    /// </summary>
    public partial class DBConnectioneditor : SessionWindow
    {
        public DBConnection myConnection;
        bool NewRecord = false;
        public DBConnectioneditor(DBConnection toEdit = null)
            :base(true)
        {
            InitializeComponent();
            UseSessionColor = false;
            
            //myConnection = toEdit ?? new DBConnection();
            string[] colors = DBConnection.GetColorList();
            Color.ItemsSource = colors;
            TextColor.ItemsSource = colors;
            if (toEdit != null)
            {
                myConnection = SEIDR.Dynamics.Extensions.XClone(toEdit);
                timeoutPicker.Value = toEdit.InternalDBConn.Timeout;
                ServerInstance.Text = toEdit.InternalDBConn.Server;
                Catalog.Text = toEdit.InternalDBConn.DefaultCatalog;
                MyName.Text = toEdit.Name;
                MyName.IsEnabled = false;
                Color.SelectedIndex = Array.IndexOf(colors, toEdit.Color ?? "Default");
                TextColor.SelectedIndex = Array.IndexOf(colors, toEdit.TextColor ?? "Default");
                //Color.SelectedIndex = colors.IndexOf(toEdit.Color ?? "Default"); //I don't remember what using reference this is from!
            }
            else
            {
                NewRecord = true;
                myConnection = new DBConnection();
                timeoutPicker.Value = SettingManager.myMiscSettings.DefaultQueryTimeout;
                ServerInstance.Text = "";
                Catalog.Text = "";
                MyName.Text = "";
                MyName.IsEnabled = true;
                Color.SelectedIndex = Array.IndexOf(colors, "Default");
                Color.SelectedIndex = Array.IndexOf(colors, "Default");
            }
            if (Color.SelectedIndex < 0)
                Color.SelectedIndex = 0;
            if (TextColor.SelectedIndex < 0)
                TextColor.SelectedIndex = 0;            
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (MyName.Text.nTrim() == "")
            {
                new Alert("Name is required", Choice: false).ShowDialog();
                return;
            }

            if (NewRecord && SettingManager.myConnections[MyName.Text] != null)
            {
                Handle($"Context menu named '{MyName.Text}' already exists.", SEIDR.Dynamics.ExceptionLevel.UI_Basic);
                return;
            }
            myConnection.Name = MyName.Text;
            DatabaseConnection temp = new DatabaseConnection(ServerInstance.Text.Trim(), Catalog.Text.nTrim(true))
            {
                Timeout = (int)(timeoutPicker.Value ?? SettingManager.myMiscSettings.DefaultQueryTimeout)
            };
            temp.CommandTimeout = temp.Timeout;
            myConnection.InternalDBConn = temp;
            myConnection.Color = Color.SelectedItem?.ToString();
            myConnection.TextColor = TextColor.SelectedItem?.ToString();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
