using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering
{
    /// <summary>Registers the PostgreSQL <see cref="IExpressionToQueryClauseTransformer"/>.</summary>
    public static class PostgreSqlExpressionTransformerServiceRegistration
    {
        /// <summary>Registers <see cref="ExpressionToPostgreSqlQueryClauseTransformer"/> as a singleton.</summary>
        /// <param name="services">Application service collection.</param>
        /// <returns>The same <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddPostgreSqlExpressionTransformations(this IServiceCollection services)
        {
            services.AddSingleton<IExpressionToQueryClauseTransformer, ExpressionToPostgreSqlQueryClauseTransformer>();
            return services;
        }
    }
}
