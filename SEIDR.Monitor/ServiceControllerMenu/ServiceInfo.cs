using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Controls;

namespace ServiceControllerMenu
{
    class ServiceInfo
    {
        DateTime lastCheck = DateTime.MinValue;
        string Location;
        ServiceController controller;
        string ServiceName;
        
        public ServiceControllerStatus Status { get; private set; }
        public void Refresh()
        {
            try
            {
                if (controller == null)
                    controller = new ServiceController(ServiceName, Location);
                else
                    controller.Refresh();
                Status = controller.Status;
            }
            catch { controller = null; }
        }
        public void HoverRefresh(object sender, RoutedEventArgs e)
        {
            MenuItem m = sender as MenuItem;
            if (m == null)
                return;
            if (DateTime.Now > lastCheck.AddSeconds(40))
            {
                lastCheck = DateTime.Now;
                Refresh();
                m.Header = $"{ServiceName} Status: {Status.ToString()}";
                
            }
        }
        public void StartStop(object sender, RoutedEventArgs e)
        {
            if (Status == ServiceControllerStatus.PausePending || Status == ServiceControllerStatus.ContinuePending)
            {
                Refresh();
                return;
            }
            MenuItem m = sender as MenuItem;
            if (m == null)
                return;
            if(Status == ServiceControllerStatus.Paused || Status == ServiceControllerStatus.Running)
            {
                StopService();
                switch (Status)
                {
                    case ServiceControllerStatus.Stopped:
                        m.Header = "Start";
                        break;
                    case ServiceControllerStatus.StopPending:
                        m.Header = "Start";                        
                        break;
                    default:
                        m.Header = "Start/Stop";
                        break;
                }
            }
            else
            {
                StartService();
                switch (Status)
                {
                    case ServiceControllerStatus.StartPending:
                        m.Header = "Stop";
                        break;
                    case ServiceControllerStatus.Running:
                        m.Header = "Stop";
                        break;
                    default:
                        m.Header = "Start/Stop";
                        break;
                }
            }
        }
        public void EvalPC(object sender, RoutedEventArgs e)
        {
            MenuItem m = sender as MenuItem;
            if (m == null)
                return;
            string text = "";
            switch (Status)
            {
                case ServiceControllerStatus.Running:
                    text = "Pause";
                    break;
                case ServiceControllerStatus.Paused:
                    text = "Continue";
                    break;
                case ServiceControllerStatus.PausePending:
                    text = "Continue";
                    break;
                case ServiceControllerStatus.ContinuePending:
                    text = "Pause";
                    break;
                default:
                    text = "Pause/Continue";
                    break;
            }
            m.Header = text;
        }
        public void EvalSS(object sender, RoutedEventArgs e)
        {
            MenuItem m = sender as MenuItem;
            if (m == null)
                return;
            string text = "";
            switch (Status)
            {
                case ServiceControllerStatus.Running:
                    text = "Stop";
                    break;
                case ServiceControllerStatus.Paused:
                    text = "Stop";
                    break;
                case ServiceControllerStatus.Stopped:
                    text = "Start";
                    break;
                case ServiceControllerStatus.StopPending:
                    text = "Start";
                    break;
                default:
                    text = "Start/Stop";
                    break;
            }
            m.Header = text;
        }

        public void PauseContinue(object sender, RoutedEventArgs e)
        {
            MenuItem m = sender as MenuItem;
            if (m == null)
                return;
            if (Status == ServiceControllerStatus.Paused)
            {
                Continue();
                switch (Status)
                {
                    case ServiceControllerStatus.ContinuePending:
                        m.Header = "Pause";
                        break;
                    case ServiceControllerStatus.Running:
                        m.Header = "Pause";
                        break;
                    default:
                        m.Header = "Pause/Continue";
                        break;
                }
            }
            else
            {
                PauseService();
                switch (Status)
                {
                    case ServiceControllerStatus.PausePending:
                        m.Header = "Continue";
                        break;
                    case ServiceControllerStatus.Paused:
                        m.Header = "Continue";
                        break;
                    default:
                        m.Header = "Pause/Continue";
                        break;
                }
            }
        }
        public ServiceInfo(string Location, string ServiceName)
        {
            try
            {
                controller = new ServiceController(ServiceName, Location);
                Status = controller.Status;
                this.ServiceName = ServiceName;
                this.Location = Location; 
            }
            catch { }
        }
        public void StopService()
        {
            try
            {
                if(Status == ServiceControllerStatus.Running)
                    controller.Stop();
            }
            finally
            {
                Refresh();
            }
        }
        public void PauseService()
        {
            try
            {
                if (Status == ServiceControllerStatus.Running && controller.CanPauseAndContinue)
                {
                    controller.Pause();
                }
            }
            finally { Refresh(); }
        }
        public void Continue()
        {
            try
            {
                if(Status == ServiceControllerStatus.Paused)
                {
                    controller.Continue();
                }
            }
            finally { Refresh(); }
        }
        public void RestartService()
        {
            try
            {
                if(controller.Status == ServiceControllerStatus.Running)
                {
                    controller.Stop();
                    controller.Start();
                }
            }
            finally
            {
                Refresh();
            }
        }
        public void StartService()
        {
            try
            {
                controller.Start();
            }
            finally { Refresh(); }
        }
    }
}
