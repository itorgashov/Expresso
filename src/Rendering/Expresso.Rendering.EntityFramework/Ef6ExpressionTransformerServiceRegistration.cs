using Expresso.Rendering.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering.EntityFramework
{
    /// <summary>Registers the Entity Framework 6 LINQ transformer.</summary>
    public static class Ef6ExpressionTransformerServiceRegistration
    {
        /// <summary>
        /// Registers a singleton <see cref="Ef6ExpressionToLinqTransformer"/> for <paramref name="providerInvariantName"/>,
        /// as itself and as <see cref="IExpressionToLinqTransformer"/>.
        /// </summary>
        /// <param name="services">Application service collection.</param>
        /// <param name="providerInvariantName">ADO.NET provider invariant name of the EF6 connection (for example <c>System.Data.SqlClient</c>).</param>
        /// <returns>The same <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddEf6ExpressionTransformations(this IServiceCollection services, string providerInvariantName)
        {
            services.AddSingleton(new Ef6ExpressionToLinqTransformer(providerInvariantName));
            services.AddSingleton<IExpressionToLinqTransformer>(sp => sp.GetRequiredService<Ef6ExpressionToLinqTransformer>());
            return services;
        }
    }
}
