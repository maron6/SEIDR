using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SEIDR.WindowMonitor.MonitorConfigurationHelpers;
using System.Data.SqlClient;
using SEIDR.Dynamics.Configurations;
using SEIDR.Dynamics;
using static SEIDR.WindowMonitor.MonitorConfigurationHelpers.LibraryManagement;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Exception details;
            details = new Exception("SENDER:" + sender.ToString());
            details = new Exception("EXCEPTION MESSAGE:" + e.Exception.Message, details);
            e.Handled = true;
            if (e.Exception is StackOverflowException)
                e.Handled = false;
            if(e.Exception is SqlException)
            {
                SqlException temp = e.Exception as SqlException;
                if(temp != null) //Should always be true in here
                {
                    details = new Exception("SQL PROCEDURE:" + temp.Procedure ?? string.Empty, details);
                    details = new Exception("SQL ERROR CODE:" + temp.ErrorCode.ToString(), details);
                }
            }
            if(e.Exception is InvalidBasicUserSesssionException)
            {
                if (((InvalidBasicUserSesssionException)e.Exception).IsLoggedIn)
                    e.Handled = true;
                else
                    e.Handled = false;
            }
            string Message = "Unhandled Exception. See log for details.";
            if (!e.Handled)
                Message += Environment.NewLine +  "Unable to recover. Application closing.";
            Handle(details, Message, ExceptionLevel.UI_Basic);
            if (e.Handled)
            {
                Window v = sender as Window;                
                if (v == null)
                {
                    var windowList = Application.Current.Windows;
                    foreach (Window w in windowList)
                    {
                        if (w.GetType() == typeof(LoginSplash))
                            continue;                                                
                        w.Close();
                    }
                }
                else
                {
                    v.DialogResult = false;
                    v.Close();
                }
            }
        }
    }
}
