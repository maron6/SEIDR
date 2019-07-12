using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Security
{
    public class NoPermissionException: Exception
    {
        const string MSG_FORMAT = "Area: '{0}' - Expected Permission '{1}', but have permission '{2}'.";
        public NoPermissionException(string RoleExpected, Permission ExpectedPermission, Permission actualPermission)
            : base(string.Format(MSG_FORMAT, RoleExpected, ExpectedPermission.GetDescription(), actualPermission.GetDescription()))
        {

        }
    }
}
