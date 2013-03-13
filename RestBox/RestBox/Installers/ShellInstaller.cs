using System.Windows;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using RestBox.ApplicationServices;
using RestBox.UserControls;
using RestBox.ViewModels;

namespace RestBox.Installers
{
    public class ShellInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(AllTypes
                                 .FromThisAssembly()
                                 .BasedOn<Window>()
                                 .Configure(c => c.LifeStyle.Singleton.Named(c.Implementation.Name)));
            container.Register(
                    Component.For<ShellViewModel>().ImplementedBy<ShellViewModel>().LifeStyle.Singleton,
                    Component.For<ILayoutApplicationService>().ImplementedBy<LayoutApplicationService>().LifeStyle.Singleton,
                    Component.For<IMainMenuApplicationService>().ImplementedBy<MainMenuApplicationService>().LifeStyle.Singleton,
                    Component.For<HttpRequest>().ImplementedBy<HttpRequest>().LifeStyle.Transient,
                    Component.For<HttpRequestViewModel>().ImplementedBy<HttpRequestViewModel>().LifeStyle.Transient,
                    Component.For<IHttpRequestService>().ImplementedBy<HttpRequestService>().LifeStyle.Transient,
                    Component.For<IFileService>().ImplementedBy<FileService>().LifeStyle.Transient,
                    Component.For<IIntellisenseService>().ImplementedBy<IntellisenseService>().LifeStyle.Singleton,

                    Component.For<RequestEnvironments>().ImplementedBy<RequestEnvironments>().LifeStyle.Singleton,
                    Component.For<RequestEnvironmentSettings>().ImplementedBy<RequestEnvironmentSettings>().LifeStyle.Transient,
                    Component.For<RequestEnvironmentsViewModel>().ImplementedBy<RequestEnvironmentsViewModel>().LifeStyle.Singleton,
                    Component.For<RequestEnvironmentSettingsViewModel>().ImplementedBy<RequestEnvironmentSettingsViewModel>().LifeStyle.Transient,

                    Component.For<RequestExtensions>().ImplementedBy<RequestExtensions>().LifeStyle.Singleton,
                    Component.For<RequestExtensionsViewModel>().ImplementedBy<RequestExtensionsViewModel>().LifeStyle.Singleton
                );
        }
    }
}
