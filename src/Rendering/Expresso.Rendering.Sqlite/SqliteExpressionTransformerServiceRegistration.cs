using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering
{
    public static class SqliteExpressionTransformerServiceRegistration
    {
        public static IServiceCollection AddSqliteExpressionTransformations(this IServiceCollection services)
        {
            services.AddSingleton<IExpressionToQueryClauseTransformer, ExpressionToSqliteQueryClauseTransformer>();
            return services;
        }
    }
}
