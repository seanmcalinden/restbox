using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.ServiceLocation;
using PrismContrib.WindsorExtensions;
using RestBox.Installers;
using RestBox.ViewModels;

namespace RestBox
{
    public class Bootstrapper : WindsorBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            Container.Install(new ShellInstaller());

            var serviceLocator = ServiceLocator.Current;

            var instance = serviceLocator.GetInstance<Shell>();

            LoadInitialMenu(instance);

            return instance;
        }

        private void LoadInitialMenu(Shell instance)
        {
            var viewModel = instance.DataContext as ShellViewModel;

            var fileMenu = new MenuItem();
            fileMenu.Header = "_File";

            var exitMenu = new MenuItem();
            exitMenu.Command = viewModel.ExitApplicationCommand;
            exitMenu.Header = "E_xit";

            fileMenu.Items.Add(new Separator());
            fileMenu.Items.Add(exitMenu);

            viewModel.MenuItems.Add(fileMenu);
        }

        protected override void InitializeShell()
        {
            Application.Current.MainWindow = (Window)Shell;
            Application.Current.MainWindow.Show();
        }
    }
}
