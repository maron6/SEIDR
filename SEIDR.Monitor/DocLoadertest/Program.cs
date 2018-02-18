using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using SEIDR.Dynamics.Windows;

namespace DocLoadertest
{
    class Program
    {
        static void Main(string[] args)
        {
            DocumentLoader.DocLoader d = new DocumentLoader.DocLoader();
            d.callerWindow = new TestWindow();
        }
    }
    class TestWindow : SEIDR_Window
    {
        public void UpdateDisplay(DataTable dt, string pluginName, System.Windows.Controls.ContextMenu startingMenu = null, 
            Action Callback = null, ushort? WaitTime = null)
        {
            throw new NotImplementedException();
        }

        public void UpdateDisplay(DataTable dt, int pluginID, ContextMenu startingMenu = null, Action Callback = null, ushort? WaitPeriod = null)
        {
            throw new NotImplementedException();
        }

        public void UpdateDisplayNoContextChange(DataTable dt)
        {
            throw new NotImplementedException();
        }

        public void UpdateDisplayNoContextChange(DataTable dt, ushort? waitPeriod = default(ushort?))
        {
            throw new NotImplementedException();
        }

        public void UpdateLabel(string pluginName, string LabelText)
        {
            throw new NotImplementedException();
        }
    }
}
