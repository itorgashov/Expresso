using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering
{
    /// <summary>Registers the SQL Server <see cref="IExpressionToQueryClauseTransformer"/>.</summary>
    public static class SqlServerExpressionTransformerServiceRegistration
    {
        /// <summary>Registers <see cref="ExpressionToSqlServerQueryClauseTransformer"/> as a singleton.</summary>
        /// <param name="services">Application service collection.</param>
        /// <returns>The same <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddSqlServerExpressionTransformations(this IServiceCollection services)
        {
            services.AddSingleton<IExpressionToQueryClauseTransformer, ExpressionToSqlServerQueryClauseTransformer>();
            return services;
        }
    }
}
