using System;
using System.Diagnostics;
using System.Windows;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using PrismContrib.WindsorExtensions;
using RestBox.ApplicationServices;
using RestBox.Events;
using RestBox.Installers;

namespace RestBox
{
    public class Bootstrapper : WindsorBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            try
            {
                Container.Install(new ShellInstaller());
                var serviceLocator = ServiceLocator.Current;

                var instance = serviceLocator.GetInstance<Shell>();

                serviceLocator.GetInstance<IMainMenuApplicationService>().CreateInitialMenuItems();

                return instance;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.ToString());
                Trace.TraceError(ex.ToString());
                throw;
            }
        }

        protected override void InitializeShell()
        {
            Application.Current.MainWindow = (Window)Shell;
            Application.Current.MainWindow.Show();
        }
    }
}
