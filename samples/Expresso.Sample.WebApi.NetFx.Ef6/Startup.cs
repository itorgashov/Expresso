using System;
using System.IO;
using System.Web.Http;
using Expresso.Parsing;
using Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;
using Expresso.Sample.WebApi.NetFx.Ef6.Filtering;
using Expresso.Sample.WebApi.NetFx.Ef6.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Owin;
using Swashbuckle.Application;

namespace Expresso.Sample.WebApi.NetFx.Ef6;

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
            .EnableSwagger(c => c.SingleApiVersion("v1", "Expresso EF6 Sample API"))
            .EnableSwaggerUi(c => c.DocumentTitle("Expresso EF6 Sample API"));
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
        var engine = SampleEngineParser.Parse(configuration["ExpressoSample:Engine"]);
        var fieldsProvider = new RequestFieldsInfoProvider(engine);
        SamplePolicySetup.Register<Controllers.BooksController>(services, configuration, "book", fieldsProvider, Console.WriteLine);
        SamplePolicySetup.Register<Controllers.AuthorsController>(services, configuration, "author", fieldsProvider, Console.WriteLine);
        SamplePolicySetup.Register<Controllers.PublishersController>(services, configuration, "publisher", fieldsProvider, Console.WriteLine);
        SampleEngineSetup.AddSampleEngine(services, configuration);
        services.AddTransient<Controllers.BooksController>();
        services.AddTransient<Controllers.AuthorsController>();
        services.AddTransient<Controllers.PublishersController>();
    }
}
