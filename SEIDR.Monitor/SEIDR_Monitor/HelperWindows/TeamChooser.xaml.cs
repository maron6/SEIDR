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

namespace SEIDR.WindowMonitor.HelperWindows
{
    /// <summary>
    /// Interaction logic for TeamChooser.xaml
    /// </summary>
    public partial class TeamChooser : SessionWindow
    {
        public bool NeedShow { get; private set; } = true;
        public string Team { get; private set; }
        public TeamChooser(bool canIncludeDefault = false)
            :base(true, SEIDR.Dynamics.Configurations.UserAccessMode.Team)
        {            
            InitializeComponent();
            if(CurrentAccessMode == SEIDR.Dynamics.Configurations.UserAccessMode.SingleUser)
            { 
                NeedShow = true;
                CanShowWindow = false; //so that it correctly gets dialog result of false if we try to show still.
                return;
            }
            var ts = SettingManager.GetTeams(canIncludeDefault);
            if(ts.Length == 0 )
            {
                Team = null; 
                NeedShow = false;                
                return;
            }
            else if(ts.Length == 1)
            {
                Team = ts[0];
                if(ts[0] == User.DEFAULT_TEAM_GUI_PLACEHOLDER)
                    Team = null; 
                NeedShow = false;
                return;
            }
            TeamBox.ItemsSource = ts;
        }

        private void OkB_Click(object sender, RoutedEventArgs e)
        {
            Team = TeamBox.SelectedValue.ToString();
            if (Team == User.DEFAULT_TEAM_GUI_PLACEHOLDER)
                Team = null;
            DialogResult = true;
            Close();
        }

        private void CancelB_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
