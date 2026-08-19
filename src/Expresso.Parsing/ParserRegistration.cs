using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Parsing
{
    public static class ParserRegistration
    {
        public static IServiceCollection AddRequestParametersParsers(this IServiceCollection services)
        {
            services.AddSingleton<IFilterParser, FilterParser>();
            services.AddSingleton<ISortDirectiveParser, SortDirectiveParser>();
            return services;
        }
    }
}
