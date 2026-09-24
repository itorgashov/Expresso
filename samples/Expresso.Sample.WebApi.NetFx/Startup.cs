using System;
using System.IO;
using System.Web.Http;
using Expresso.Core.Filtering;
using Expresso.Parsing;
using Expresso.Sample.Shared.DataAccess;
using Expresso.Sample.WebApi.NetFx.DataAccess;
using Expresso.Sample.WebApi.NetFx.Filtering;
using Expresso.Sample.WebApi.NetFx.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Owin;
using Swashbuckle.Application;

namespace Expresso.Sample.WebApi.NetFx;

/// <summary>OWIN startup for the .NET Framework sample host.</summary>
public sealed class Startup
{
    /// <summary>Builds configuration, registers the sample engine, and hosts Web API with Swagger.</summary>
    /// <param name="app">OWIN application builder.</param>
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
        services.AddSingleton<IRequestFieldsInfoProvider, RequestFieldsInfoProvider>();
        SampleEngineSetup.AddSampleEngine(services, configuration);
        services.AddTransient<Controllers.BooksController>();
        services.AddTransient<Controllers.AuthorsController>();
        services.AddTransient<Controllers.PublishersController>();
    }
}
