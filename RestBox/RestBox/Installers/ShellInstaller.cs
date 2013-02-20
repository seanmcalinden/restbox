using System.Windows;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using RestBox.Services;
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
                    Component.For<IApplicationLayout>().ImplementedBy<ApplicationLayout>().LifeStyle.Singleton
                );
        }
    }
}
