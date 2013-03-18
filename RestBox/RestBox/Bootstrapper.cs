﻿using System.Windows;
using Microsoft.Practices.ServiceLocation;
using PrismContrib.WindsorExtensions;
using RestBox.ApplicationServices;
using RestBox.Installers;

namespace RestBox
{
    public class Bootstrapper : WindsorBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            Container.Install(new ShellInstaller());

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
