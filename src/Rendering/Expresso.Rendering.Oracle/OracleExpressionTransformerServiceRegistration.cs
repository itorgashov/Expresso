using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering
{
    /// <summary>Registers the Oracle <see cref="IExpressionToQueryClauseTransformer"/>.</summary>
    public static class OracleExpressionTransformerServiceRegistration
    {
        /// <summary>Registers <see cref="ExpressionToOracleQueryClauseTransformer"/> as a singleton.</summary>
        /// <param name="services">Application service collection.</param>
        /// <returns>The same <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddOracleExpressionTransformations(this IServiceCollection services)
        {
            services.AddSingleton<IExpressionToQueryClauseTransformer, ExpressionToOracleQueryClauseTransformer>();
            return services;
        }
    }
}
