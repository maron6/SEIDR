using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Windows;

namespace SEIDR.Dynamics.Configurations
{
    [Flags]
    public enum UserAccessMode
    {        
        None = 0,
        SingleUser = 1,
        Team = 2,
        Impersonation = 4
            /*, //Note useful... more useful to have on the User. (Window validation at show)
        [EditableObjectHiddenEnumValue]
        TEAM_IMPERSONATION //Decided against... User access mode should be something that the user can change after login...not just from misc setting choice
        // */
    }
}
