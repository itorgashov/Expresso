using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering
{
    /// <summary>Registers the SQLite <see cref="IExpressionToQueryClauseTransformer"/>.</summary>
    public static class SqliteExpressionTransformerServiceRegistration
    {
        /// <summary>Registers <see cref="ExpressionToSqliteQueryClauseTransformer"/> as a singleton.</summary>
        /// <param name="services">Application service collection.</param>
        /// <returns>The same <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddSqliteExpressionTransformations(this IServiceCollection services)
        {
            services.AddSingleton<IExpressionToQueryClauseTransformer, ExpressionToSqliteQueryClauseTransformer>();
            return services;
        }
    }
}
