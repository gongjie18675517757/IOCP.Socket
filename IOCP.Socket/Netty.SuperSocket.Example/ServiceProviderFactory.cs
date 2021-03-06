﻿using AspectCore.Extensions.DependencyInjection;
using AspectCore.Injector;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Netty.SuperSocket.Example
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
