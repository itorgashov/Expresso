using Expresso.Core.Projection;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed class ProjectionReviewTests
{
    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    [InlineData("MySql")]
    [InlineData("Sqlite")]
    [InlineData("Oracle")]
    public async Task BinaryKeysAndSeededDtos_RemainTranslatableAndCorrect(string provider)
    {
        // Scenario: real SQL translation of binary composite predicates; fixture materialization verifies content matching.
        var options = new DbContextOptionsBuilder<BinaryContext>();
        switch (provider)
        {
            case "SqlServer": options.UseSqlServer("Server=unused;Database=unused"); break;
            case "PostgreSql": options.UseNpgsql("Host=unused;Database=unused"); break;
            case "MySql": options.UseMySql("Server=unused;Database=unused", new MySqlServerVersion(new Version(8, 0, 36))); break;
            case "Sqlite": options.UseSqlite("Data Source=unused.db"); break;
            case "Oracle": options.UseOracle("User Id=unused;Password=unused;Data Source=unused"); break;
        }
        using var db = new BinaryContext(options.Options);
        var sql = new List<string>();
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var result = await transformer.RenderProjection(ProjectionDirective.All, BinaryProjectionFixture.Mapping())
            .ExecuteAsync(db.Set<BinaryProjectionParent>(), new ProjectionSqlExecutor<BinaryProjectionParent>(BinaryProjectionFixture.Data(), q => sql.Add(q.ToQueryString())),
                transformer, options: new Expresso.Rendering.Linq.ProjectionExecutionOptions { MaxKeyParameters = 2 });
        Assert.Equal("first", Assert.Single(result.Items[0].Children).Name);
        Assert.Equal("second", Assert.Single(result.Items[1].Children).Name);
        Assert.Empty(result.Items[2].Children);
        Assert.Equal(4, sql.Count);
        Assert.All(sql.Skip(1), query => Assert.Contains("WHERE", query));
    }

    private sealed class BinaryContext : DbContext
    {
        public BinaryContext(DbContextOptions<BinaryContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<BinaryProjectionParent>().HasKey(x => new { x.Tenant, x.Id });
            model.Entity<BinaryProjectionParent>().Property(x => x.Tenant).HasMaxLength(16);
            model.Entity<BinaryProjectionChild>().HasKey(x => x.Id);
            model.Entity<BinaryProjectionChild>().Property(x => x.ParentTenant).HasMaxLength(16);
            model.Entity<BinaryProjectionParent>().HasMany(x => x.Children).WithOne().HasForeignKey(x => new { x.ParentTenant, x.ParentId });
        }
    }
}
