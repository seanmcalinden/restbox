using System.Windows;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using RestBox.Activities;
using RestBox.ApplicationServices;
using RestBox.Domain.Services;
using RestBox.Factories;
using RestBox.Mappers;
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
                    Component.For<IRestBoxStateService>().ImplementedBy<RestBoxStateService>().LifeStyle.Transient,
                    Component.For<ILayoutDataFactory>().ImplementedBy<LayoutDataFactory>().LifeStyle.Transient,
                    Component.For<ILayoutApplicationService>().ImplementedBy<LayoutApplicationService>().LifeStyle.Singleton,
                    Component.For<IMainMenuApplicationService>().ImplementedBy<MainMenuApplicationService>().LifeStyle.Singleton,
                    Component.For<HttpRequest>().ImplementedBy<HttpRequest>().LifeStyle.Transient,
                    Component.For<HttpRequestViewModel>().ImplementedBy<HttpRequestViewModel>().LifeStyle.Transient,
                    Component.For<HttpRequestFiles>().ImplementedBy<HttpRequestFiles>().LifeStyle.Singleton,
                    Component.For<HttpRequestFilesViewModel>().ImplementedBy<HttpRequestFilesViewModel>().LifeStyle.Singleton,
                    Component.For<IHttpRequestService>().ImplementedBy<HttpRequestService>().LifeStyle.Transient,
                    Component.For<IFileService>().ImplementedBy<FileService>().LifeStyle.Transient,
                    Component.For<IIntellisenseService>().ImplementedBy<IntellisenseService>().LifeStyle.Singleton,

                    Component.For<HttpRequestSequenceFiles>().ImplementedBy<HttpRequestSequenceFiles>().LifeStyle.Singleton,
                    Component.For<HttpRequestSequence>().ImplementedBy<HttpRequestSequence>().LifeStyle.Transient,
                    Component.For<HttpRequestSequenceFilesViewModel>().ImplementedBy<HttpRequestSequenceFilesViewModel>().LifeStyle.Singleton,
                    Component.For<HttpRequestSequenceViewModel>().ImplementedBy<HttpRequestSequenceViewModel>().LifeStyle.Transient,

                    Component.For<HttpInterceptorFiles>().ImplementedBy<HttpInterceptorFiles>().LifeStyle.Singleton,
                    Component.For<HttpInterceptorFilesViewModel>().ImplementedBy<HttpInterceptorFilesViewModel>().LifeStyle.Singleton,
                    Component.For<HttpInterceptor>().ImplementedBy<HttpInterceptor>().LifeStyle.Transient,
                    Component.For<HttpInterceptorViewModel>().ImplementedBy<HttpInterceptorViewModel>().LifeStyle.Transient,
                    Component.For<IProxyService>().ImplementedBy<ProxyService>().LifeStyle.Singleton,
                    
                    Component.For<RequestEnvironments>().ImplementedBy<RequestEnvironments>().LifeStyle.Singleton,
                    Component.For<RequestEnvironmentSettings>().ImplementedBy<RequestEnvironmentSettings>().LifeStyle.Transient,
                    Component.For<RequestEnvironmentsFilesViewModel>().ImplementedBy<RequestEnvironmentsFilesViewModel>().LifeStyle.Singleton,
                    Component.For<RequestEnvironmentSettingsViewModel>().ImplementedBy<RequestEnvironmentSettingsViewModel>().LifeStyle.Transient,

                    Component.For<RequestExtensions>().ImplementedBy<RequestExtensions>().LifeStyle.Singleton,
                    Component.For<RequestExtensionFilesViewModel>().ImplementedBy<RequestExtensionFilesViewModel>().LifeStyle.Singleton,
                    Component.For<IJsonSerializer>().ImplementedBy<JsonSerializer>().LifeStyle.Transient,

                    Component.For<IMapper<HttpRequestItemFile, HttpRequestViewModel>>().ImplementedBy<MapHttpRequestItemFileToHttpRequestViewModel>(),
                    Component.For<IMapper<RequestEnvironmentSettingFile, RequestEnvironmentSettingsViewModel>>().ImplementedBy<MapRequestEnvironmentFileToRequestEnvironmentSettingsViewModel>(),
                    Component.For<IMapper<HttpRequestItemFile, HttpInterceptorViewModel>>().ImplementedBy<MapHttpRequestItemToHttpInterceptorViewModel>(),

                    Component.For<HttpRequestActivityModel>().ImplementedBy<HttpRequestActivityModel>().LifeStyle.Transient,
                    Component.For<StartPage>().ImplementedBy<StartPage>().LifeStyle.Transient,
                    Component.For<StartPageViewModel>().ImplementedBy<StartPageViewModel>().LifeStyle.Transient);


        }
    }
}
