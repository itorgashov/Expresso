using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering
{
    public static class OracleExpressionTransformerServiceRegistration
    {
        public static IServiceCollection AddOracleExpressionTransformations(this IServiceCollection services)
        {
            services.AddSingleton<IExpressionToQueryClauseTransformer, ExpressionToOracleQueryClauseTransformer>();
            return services;
        }
    }
}
