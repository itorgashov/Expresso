#if NET8_0_OR_GREATER
using Expresso.Core.Projection;
using Expresso.Rendering.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.Integration.Test.Ef;

[Trait("Category", "Integration")]
public sealed class ProjectionDefaultSortItTests
{
    [SkippableTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Projection_GrandchildSort_KeepsParentOrdering(bool explicitParent)
    {
        // Scenario: execute a deepest-only sort against actual data; configured child defaults remain active.
        Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        ProjectionDdl.Reset(connection, ParameterBinder.At);
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO widget_projection_leaf (tenant,id,child_id,value) VALUES (1,2,1,'zulu')";
        await command.ExecuteNonQueryAsync();
        using var db = new Context(new DbContextOptionsBuilder<Context>().UseSqlite(connection).Options);
        var result = await db.Set<ProjectionParent>().Where(p => p.Tenant == 1 && p.Id == 1).ToProjectedListAsync(
            new EfCoreExpressionToLinqTransformer(db.Database.ProviderName), ProjectionDirective.All, OrderedProjectionFixture.Mapping(),
            OrderedProjectionFixture.Sort(explicitParent));
        var parent = Assert.Single(result.Items);
        Assert.Equal(explicitParent ? new[] { "beta", "alpha" } : new[] { "alpha", "beta" }, parent.Children.Select(c => c.Name));
        Assert.Equal(new[] { "zulu", "first" }, parent.Children.Single(c => c.Name == "beta").Leaves.Select(l => l.Value));
    }

    private sealed class Context : DbContext
    {
        internal Context(DbContextOptions<Context> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder model) => ProjectionModelConfiguration.Configure(model);
    }
}
#endif
