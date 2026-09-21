using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering
{
    public static class PostgreSqlExpressionTransformerServiceRegistration
    {
        public static IServiceCollection AddPostgreSqlExpressionTransformations(this IServiceCollection services)
        {
            services.AddSingleton<IExpressionToQueryClauseTransformer, ExpressionToPostgreSqlQueryClauseTransformer>();
            return services;
        }
    }
}
