using System.Windows;
using Microsoft.Practices.ServiceLocation;
using PrismContrib.WindsorExtensions;
using RestBox.ApplicationServices;
using RestBox.Domain.Services.Installers;
using RestBox.Installers;
using RestBox.ViewModels;

namespace RestBox
{
    public class Bootstrapper : WindsorBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            Container.Install(new ShellInstaller());
            Container.Install(new ServicesInstaller());

            var serviceLocator = ServiceLocator.Current;

            var instance = serviceLocator.GetInstance<Shell>();
            serviceLocator.GetInstance<IMainMenuApplicationService>().CreateInitialMenuItems();
            
            return instance;
        }

        protected override void InitializeShell()
        {
            Application.Current.MainWindow = (Window)Shell;
            Application.Current.MainWindow.Show();
        }
    }
}
