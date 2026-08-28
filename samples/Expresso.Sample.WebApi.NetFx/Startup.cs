using System;
using System.IO;
using System.Web.Http;
using Expresso.Core.Filtering;
using Expresso.Parsing;
using Expresso.Sample.Shared.DataAccess;
using Expresso.Sample.Shared.Filtering;
using Expresso.Sample.Shared.Models;
using Expresso.Sample.WebApi.NetFx.DataAccess;
using Expresso.Sample.WebApi.NetFx.Infrastructure;
using Expresso.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Owin;
using Swashbuckle.Application;

namespace Expresso.Sample.WebApi.NetFx;

public sealed class Startup
{
    public void Configuration(IAppBuilder app)
    {
        var configuration = BuildConfiguration();
        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        var provider = services.BuildServiceProvider();

        var config = new HttpConfiguration
        {
            DependencyResolver = new ServiceProviderDependencyResolver(provider),
        };

        config.MapHttpAttributeRoutes();
        config
            .EnableSwagger(c => c.SingleApiVersion("v1", "Expresso Sample API"))
            .EnableSwaggerUi(c => c.DocumentTitle("Expresso Sample API"));
        app.UseWebApi(config);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddUserSecrets<Startup>(optional: true)
            .Build();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddRequestParametersParsers();
        services.AddExpressionTransformations();
        services.AddSingleton<IRequestFieldsInfoProvider, RequestFieldsInfoProvider>();
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddTransient<IRepository<Book>, BookRepository>();
        services.AddTransient<IRepository<Author>, AuthorRepository>();
        services.AddTransient<IRepository<Publisher>, PublisherRepository>();
        services.AddTransient<Controllers.BooksController>();
        services.AddTransient<Controllers.AuthorsController>();
        services.AddTransient<Controllers.PublishersController>();
    }
}
