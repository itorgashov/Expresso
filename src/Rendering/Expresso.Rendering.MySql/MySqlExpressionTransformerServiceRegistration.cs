using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering
{
    /// <summary>Registers the MySQL and MariaDB <see cref="IExpressionToQueryClauseTransformer"/>.</summary>
    public static class MySqlExpressionTransformerServiceRegistration
    {
        /// <summary>Registers <see cref="ExpressionToMySqlQueryClauseTransformer"/> as a singleton.</summary>
        /// <param name="services">Application service collection.</param>
        /// <returns>The same <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddMySqlExpressionTransformations(this IServiceCollection services)
        {
            services.AddSingleton<IExpressionToQueryClauseTransformer, ExpressionToMySqlQueryClauseTransformer>();
            return services;
        }
    }
}
