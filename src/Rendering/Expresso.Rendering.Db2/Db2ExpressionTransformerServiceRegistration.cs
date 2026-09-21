using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering
{
    public static class Db2ExpressionTransformerServiceRegistration
    {
        public static IServiceCollection AddDb2ExpressionTransformations(this IServiceCollection services)
        {
            services.AddSingleton<IExpressionToQueryClauseTransformer, ExpressionToDb2QueryClauseTransformer>();
            return services;
        }
    }
}
