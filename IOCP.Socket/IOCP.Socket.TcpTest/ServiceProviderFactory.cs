using System;
using Microsoft.Extensions.DependencyInjection;
using AspectCore.Extensions.DependencyInjection;
using AspectCore.Injector;

namespace IOCP.Socket.TcpTest
{
    public class ServiceProviderFactory : IServiceProviderFactory<IServiceResolver>
    {
        public IServiceResolver CreateBuilder(IServiceCollection services)
        {
            var container = services.ToServiceContainer();
            return container.Build();
        }

        public IServiceProvider CreateServiceProvider(IServiceResolver containerBuilder)
        {
            return containerBuilder;
        }
    }
}
