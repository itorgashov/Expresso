using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Sample.WebApi.NetFx.Infrastructure;

internal sealed class ServiceProviderDependencyResolver : IDependencyResolver
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceProviderDependencyResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IDependencyScope BeginScope() => new ServiceProviderDependencyScope(_serviceProvider);

    public object? GetService(Type serviceType) => _serviceProvider.GetService(serviceType);

    public IEnumerable<object?> GetServices(Type serviceType) => _serviceProvider.GetServices(serviceType);

    public void Dispose()
    {
    }

    private sealed class ServiceProviderDependencyScope : IDependencyScope
    {
        private readonly IServiceScope _scope;

        public ServiceProviderDependencyScope(IServiceProvider serviceProvider)
        {
            _scope = serviceProvider.CreateScope();
        }

        public object? GetService(Type serviceType) => _scope.ServiceProvider.GetService(serviceType);

        public IEnumerable<object?> GetServices(Type serviceType) => _scope.ServiceProvider.GetServices(serviceType);

        public void Dispose() => _scope.Dispose();
    }
}
