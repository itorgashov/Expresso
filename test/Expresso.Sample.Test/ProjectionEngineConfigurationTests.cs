using System;
using System.Collections.Generic;
using System.Linq;
using Expresso.Core.Filtering;
using Expresso.Core.Projection;
using Expresso.Sample.Shared.DataAccess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#if NETFRAMEWORK
using CatalogProvider = Expresso.Sample.WebApi.NetFx.Filtering.RequestProjectionModelProvider;
using EngineSetup = Expresso.Sample.WebApi.NetFx.DataAccess.SampleEngineSetup;
#else
using CatalogProvider = Expresso.Sample.WebApi.Filtering.RequestProjectionModelProvider;
using EngineSetup = Expresso.Sample.WebApi.DataAccess.SampleEngineSetup;
#endif

namespace Expresso.Sample.Test;

public sealed class ProjectionEngineConfigurationTests
{
    [Theory]
    [MemberData(nameof(Engines))]
    public void ConfiguredMappings_SupplyCachedCatalogsWithoutDatabaseIo(string engine)
    {
        // Scenario: DI selects each supported configured engine, including MariaDb's alias, without opening a connection.
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["ExpressoSample:Engine"] = engine, ["ConnectionStrings:" + engine] = "unused-test-connection" }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IRequestProjectionModelProvider, CatalogProvider>();
        EngineSetup.AddSampleEngine(services, configuration);
        using var container = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var sql = container.GetRequiredService<ISampleSql>();
        Assert.Equal(engine == "MariaDb" ? SampleEngine.MySql : (SampleEngine)Enum.Parse(typeof(SampleEngine), engine), sql.Engine);
        var mappings = container.GetRequiredService<SampleProjectionMappings>();
        // A response-mapping extension makes using some other hardcoded/static mapping observable.
        mappings.Books.Field("configured", "configured", sql.Col("b", "configured"), typeof(int));
        var provider = container.GetRequiredService<IRequestProjectionModelProvider>();
        Assert.Same(provider, container.GetRequiredService<IRequestProjectionModelProvider>());
        Assert.Same(mappings, container.GetRequiredService<SampleProjectionMappings>());
        Assert.True(provider.GetProjectionModel("book").TryGetField("configured", out _));
        foreach (var entry in new[] { ("book", mappings.Books), ("author", mappings.Authors), ("publisher", mappings.Publishers) })
        {
            var catalog = provider.GetProjectionModel(entry.Item1);
            Assert.Same(catalog, provider.GetProjectionModel(entry.Item1.ToUpperInvariant()));
            Assert.Equal(entry.Item2.GetProjectionModel().Fields, catalog.Fields);
            Assert.Equal(entry.Item2.Collections.Keys.OrderBy(n => n), catalog.Collections.Select(c => c.Name).OrderBy(n => n));
        }
        Assert.Same(QueryModel.Empty, provider.GetProjectionModel("unknown"));
    }

    public static IEnumerable<object[]> Engines()
    {
        foreach (var engine in new[] { "SqlServer", "PostgreSql", "MySql", "MariaDb", "Sqlite", "Oracle" }) yield return new object[] { engine };
#if !NETFRAMEWORK
        yield return new object[] { "Db2" };
#endif
    }

    [Fact]
    public void Catalogs_AreInstanceScoped_AndRequireMappings()
    {
        // Scenario: one host/configuration's catalog must not leak into another; reject a missing mapping dependency.
        Assert.Throws<ArgumentNullException>(() => new CatalogProvider(null!));
        var first = new SampleProjectionMappings(SampleSqlFactory.Create(SampleEngine.PostgreSql));
        var second = new SampleProjectionMappings(SampleSqlFactory.Create(SampleEngine.Sqlite));
        first.Books.Field("first", "first", "b.first", typeof(int));
        second.Books.Field("second", "second", "b.second", typeof(int));
        var firstProvider = new CatalogProvider(first);
        var secondProvider = new CatalogProvider(second);
        Assert.True(firstProvider.GetProjectionModel("book").TryGetField("first", out _));
        Assert.False(firstProvider.GetProjectionModel("book").TryGetField("second", out _));
        Assert.True(secondProvider.GetProjectionModel("book").TryGetField("second", out _));
        Assert.False(secondProvider.GetProjectionModel("book").TryGetField("first", out _));
    }
}
