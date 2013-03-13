using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace RestBox.Domain.Services.Installers
{
    public class ServicesInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(AllTypes
                                   .FromThisAssembly()
                                   .Where(type => type.Name.EndsWith("Service"))
                                   .WithService
                                   .AllInterfaces()
                                   .Configure(c => c.LifeStyle.Transient.Named(c.Implementation.Name)));
            container.Register(Component.For<IJsonSerializer>().ImplementedBy<JsonSerializer>().LifeStyle.Transient);
        }
    }
}
