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

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for MiscSettingPage.xaml
    /// </summary>
    public partial class MiscSettingPage : Window// Ryan_UtilityCode.Dynamics.Configurations.SessionWindow
    {
        MiscSetting mySettings;
        public MiscSettingPage(MiscSetting toEdit)
        {
            InitializeComponent();            
            mySettings = toEdit;
            RefreshSetting.Value = mySettings.FileRefresh;
            Timeout.Value = mySettings.DefaultQueryTimeout;
            /*
            AndromedaFolder.Text = toEdit.AndromedaFilesBaseFolder;
            SandboxAndromedaFolder.Text = toEdit.SandboxBaseFolder;
            IntegrationServicesServer.Text = toEdit.DS_IS_Server;*/
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            /*
            mySettings.AndromedaFilesBaseFolder = AndromedaFolder.Text;
            mySettings.SandboxBaseFolder = SandboxAndromedaFolder.Text;
            mySettings.DS_IS_Server = IntegrationServicesServer.Text;
            mySettings.Save();*/
            mySettings.FileRefresh = (int) (RefreshSetting.Value ?? 7);
            mySettings.DefaultQueryTimeout = (int)(Timeout.Value ?? 120);
            //SettingManager.myMiscSettings = mySettings;
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
