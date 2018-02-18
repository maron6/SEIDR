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
using SEIDR.WindowMonitor.Models;
using System.Data;
using System.Data.SqlClient;
//using SEIDR.Processing.Data.DBObjects;
using SEIDR.DataBase;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for DashboardDisplay.xaml
    /// </summary>
    public partial class DashboardDisplay : SessionWindow
    {
        Dashboard d;
        DashboardPage current = null;
        public DashboardDisplay(SqlCommand sourceCommand, DatabaseConnection connection, string Title = "Dashboard Viewer")
        {
            InitializeComponent();
            d = new Dashboard(sourceCommand, connection);
            current = d.RefreshList();
            this.Title = Title;
            SetupPage();
        }
        private void SetupPage()
        {
            DashboardData.Height = 150;
            DashboardData.RowDefinitions.Clear();
            DashboardData.Children.Clear();
            DashboardName.Text = current.DashboardTitle;
            int currentCol = 0;
            int currentRow = 0;
            foreach(DashboardItem item in current.MyItems)
            {
                if(currentCol %2 == 0)
                    AddRow();
                //Grid.SetR
                if(item.ProgressMin.HasValue && item.ProgressMax.HasValue && item.Progress.HasValue){
                    if (currentCol != 0)
                    {
                        currentRow++;
                        currentCol = 0;
                        AddRow();
                    }
                    ProgressBar pb = new ProgressBar()
                    {
                        Name = item.ItemName + "_pb",
                        Value = item.Progress.Value,
                        Margin = new Thickness(10, 10, 10,10),
                        Maximum = item.ProgressMax.Value,
                        Minimum = item.ProgressMin.Value,
                        Foreground = (LinearGradientBrush)new BrushConverter().ConvertFromString(item.Color),
                        Height = 40
                    };
                    DashboardData.Children.Add(pb);
                    Grid.SetRow(pb, currentRow);
                    Grid.SetColumn(pb, currentCol);
                    currentCol++;                    
                }
                TextBox tb = new TextBox(){
                        Name = item.ItemName + "_tb",
                        Text = item.value,
                        ToolTip = item.ItemName,
                        Margin = new Thickness(10,10,10,10),
                        Background = (SolidColorBrush)new BrushConverter().ConvertFromString(item.Color),
                        IsReadOnly = true,
                        Height = 40
                };
                DockPanel.SetDock(tb, Dock.Bottom);
                Label lb = new Label()
                {
                    Content = item.ItemName
                };
                DockPanel.SetDock(lb, Dock.Top);
                DockPanel dp = new DockPanel()
                {
                    LastChildFill = true
                };
                dp.Children.Add(tb);
                dp.Children.Add(lb);
                DashboardData.Children.Add(dp);     //tb);
                Grid.SetRow(dp, currentRow);
                Grid.SetColumn(dp, currentCol);
                
                currentCol++;
                if(currentCol > 1)
                {
                    currentRow++;
                    currentCol = 0;
                }
            }
            
            

        }
        private void AddRow()
        {
            RowDefinition r = new RowDefinition() { Height = new GridLength(45) };
            DashboardData.RowDefinitions.Add(r);
            if (DashboardData.Height < 900)
                DashboardData.Height += 50;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            SetupPage();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
