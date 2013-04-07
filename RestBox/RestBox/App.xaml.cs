using System;
using System.Windows;
using Microsoft.Practices.ServiceLocation;
using RestBox.ApplicationServices;

namespace RestBox
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var bootstrapper = new Bootstrapper();
            bootstrapper.Run();
            if (AppDomain.CurrentDomain.SetupInformation.ActivationArguments != null)
            {
                string[] activationData = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
                if (activationData != null && activationData.Length > 0)
                {
                    var mainMenuApplicationService = ServiceLocator.Current.GetInstance<IMainMenuApplicationService>();
                    mainMenuApplicationService.OpenSolution(activationData[0]);
                }
            }
        }
    }
}
