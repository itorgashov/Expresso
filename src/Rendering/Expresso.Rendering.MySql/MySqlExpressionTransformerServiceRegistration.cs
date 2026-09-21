using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering
{
    public static class MySqlExpressionTransformerServiceRegistration
    {
        public static IServiceCollection AddMySqlExpressionTransformations(this IServiceCollection services)
        {
            services.AddSingleton<IExpressionToQueryClauseTransformer, ExpressionToMySqlQueryClauseTransformer>();
            return services;
        }
    }
}
