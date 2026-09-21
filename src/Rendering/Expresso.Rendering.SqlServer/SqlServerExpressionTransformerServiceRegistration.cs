using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering
{
    public static class SqlServerExpressionTransformerServiceRegistration
    {
        public static IServiceCollection AddSqlServerExpressionTransformations(this IServiceCollection services)
        {
            services.AddSingleton<IExpressionToQueryClauseTransformer, ExpressionToSqlServerQueryClauseTransformer>();
            return services;
        }
    }
}
