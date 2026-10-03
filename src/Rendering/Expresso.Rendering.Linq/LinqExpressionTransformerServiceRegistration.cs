using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering.Linq
{
    /// <summary>Registers the LINQ transformers.</summary>
    public static class LinqExpressionTransformerServiceRegistration
    {
        /// <summary>
        /// Registers <see cref="QueryableExpressionToLinqTransformer"/> as <see cref="IExpressionToLinqTransformer"/> and
        /// <see cref="InMemoryExpressionToLinqTransformer"/> as its concrete type, both singletons.
        /// </summary>
        /// <param name="services">Application service collection.</param>
        /// <returns>The same <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddLinqExpressionTransformations(this IServiceCollection services)
        {
            services.AddSingleton<IExpressionToLinqTransformer, QueryableExpressionToLinqTransformer>();
            services.AddSingleton<InMemoryExpressionToLinqTransformer>();
            return services;
        }
    }
}
