using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Parsing
{
    public static class ParserRegistration
    {
        public static IServiceCollection AddRequestParametersParsers(this IServiceCollection services)
        {
            return services.AddRequestParametersParsers((LiteralParseOptions?)null);
        }

        public static IServiceCollection AddRequestParametersParsers(
            this IServiceCollection services,
            Action<LiteralParseOptions> configure)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var options = new LiteralParseOptions();
            configure(options);
            return services.AddRequestParametersParsers(options);
        }

        public static IServiceCollection AddRequestParametersParsers(
            this IServiceCollection services,
            LiteralParseOptions? options)
        {
            services.AddSingleton(options ?? LiteralParseOptions.Default);
            services.AddSingleton<IFilterParser, FilterParser>();
            services.AddSingleton<ISortDirectiveParser, SortDirectiveParser>();
            return services;
        }
    }
}
