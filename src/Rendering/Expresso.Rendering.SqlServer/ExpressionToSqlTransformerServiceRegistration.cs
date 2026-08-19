using Microsoft.Extensions.DependencyInjection;

namespace Expresso.SqlServer
{
    public static class ExpressionToSqlTransformerServiceRegistration
    {
        public static IServiceCollection AddExpressionTransformations(this IServiceCollection services)
        {
            services.AddSingleton<IExpressionToQueryClauseTransformer, ExpressionToSqlServerQueryClauseTransformer>();
            return services;
        }
    }
}
