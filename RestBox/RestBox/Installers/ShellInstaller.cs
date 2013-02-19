using System.Windows;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

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
        }
    }
}
