using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering
{
    /// <summary>Registers the IBM Db2 <see cref="IExpressionToQueryClauseTransformer"/>.</summary>
    public static class Db2ExpressionTransformerServiceRegistration
    {
        /// <summary>Registers <see cref="ExpressionToDb2QueryClauseTransformer"/> as a singleton.</summary>
        /// <param name="services">Application service collection.</param>
        /// <returns>The same <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddDb2ExpressionTransformations(this IServiceCollection services)
        {
            services.AddSingleton<ExpressionToDb2QueryClauseTransformer>();
            services.AddSingleton<IExpressionToQueryClauseTransformer>(sp => sp.GetRequiredService<ExpressionToDb2QueryClauseTransformer>());
            services.AddSingleton<IProjectionToSqlTransformer>(sp => sp.GetRequiredService<ExpressionToDb2QueryClauseTransformer>());
            return services;
        }
    }
}
