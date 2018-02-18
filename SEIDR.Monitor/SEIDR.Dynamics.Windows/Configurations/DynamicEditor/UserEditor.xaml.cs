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
using SEIDR.Dynamics.Configurations.UserConfiguration;

namespace SEIDR.Dynamics.Configurations.DynamicEditor
{
    /// <summary>
    /// Interaction logic for UserEditor.xaml
    /// </summary>
    public partial class UserEditor : BaseEditorWindow<WindowUser>
    {
        public UserEditor(WindowUser edit, bool SameTeam)
            :base(edit,
                 SameTeam
                 ? BasicUserPermissions.UserEditor| BasicUserPermissions.TeamEditor
                 : BasicUserPermissions.UserEditor)
        {
            InitializeComponent();            
            TeamPicker.Configure(SessionBroker.GetLookup(WindowConfigurationScope.TM),
                "Team", edit.TeamID, WindowConfigurationScope.TM, false);
            AdminLevel.Configure(SessionBroker.GetLookup(WindowConfigurationScope.ADML),
                "Admin Level", edit.AdminLevel, WindowConfigurationScope.ADML, false);
            DataContext = Edit;
        }        
        
    }
}
