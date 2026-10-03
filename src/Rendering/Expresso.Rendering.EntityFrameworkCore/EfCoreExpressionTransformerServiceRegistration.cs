using Expresso.Rendering.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering.EntityFrameworkCore
{
    /// <summary>Registers the EF Core LINQ transformer.</summary>
    public static class EfCoreExpressionTransformerServiceRegistration
    {
        /// <summary>
        /// Registers a scoped <see cref="EfCoreExpressionToLinqTransformer"/> for the provider of <typeparamref name="TContext"/>,
        /// as itself and as <see cref="IExpressionToLinqTransformer"/>. With several contexts, the last call owns the interface.
        /// </summary>
        /// <typeparam name="TContext">Registered context whose <c>Database.ProviderName</c> selects the overrides.</typeparam>
        /// <param name="services">Application service collection.</param>
        /// <returns>The same <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddEfCoreExpressionTransformations<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            services.AddScoped(sp => new EfCoreExpressionToLinqTransformer(sp.GetRequiredService<TContext>().Database.ProviderName));
            services.AddScoped<IExpressionToLinqTransformer>(sp => sp.GetRequiredService<EfCoreExpressionToLinqTransformer>());
            return services;
        }
    }
}
