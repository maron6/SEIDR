using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Dynamics.Configurations.QueryConfiguration
{
    public class DefaultQueryConfigurationList : QueryList
    {
        public override WindowConfigurationList<Query> cloneSetup()
        {
            return this.XClone();
        }
    }
}
