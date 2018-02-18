using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SEIDR.Dynamics.Configurations
{
    public abstract class iNotifyControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// when called from a setter property, invokes property changed for the name of the property
        /// </summary>
        /// <param name="caller"></param>
        public void InvokeChanged([CallerMemberName] string caller = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }
    }
}
