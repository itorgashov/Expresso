using Expresso.Core.Projection;
using Expresso.Rendering.TestCases;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Data.SQLite;
using Npgsql;

namespace Expresso.Rendering.EntityFramework.Test;

public sealed class ProjectionReviewTests
{
    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    [InlineData("Sqlite")]
    public async Task BinaryKeysAndSeededDtos_RemainTranslatableAndCorrect(string provider)
    {
        // Scenario: EF6 must translate binary composite predicates without custom SQL functions or database I/O.
        using var db = new BinaryContext(provider);
        var sql = new List<string>();
        var transformer = new Ef6ExpressionToLinqTransformer(db);
        var result = await transformer.RenderProjection(ProjectionDirective.All, BinaryProjectionFixture.Mapping())
            .ExecuteAsync(db.Set<BinaryProjectionParent>(), new ProjectionSqlExecutor<BinaryProjectionParent>(BinaryProjectionFixture.Data(), q => sql.Add(q.ToString()!)),
                transformer, options: new Expresso.Rendering.Linq.ProjectionExecutionOptions { MaxKeyParameters = 2 });
        Assert.Equal("first", Assert.Single(result.Items[0].Children).Name);
        Assert.Equal("second", Assert.Single(result.Items[1].Children).Name);
        Assert.Empty(result.Items[2].Children);
        Assert.Equal(4, sql.Count);
        Assert.All(sql.Skip(1), query => Assert.Contains("WHERE", query));
    }

    [DbConfigurationType(typeof(TestEf6Configuration))]
    private sealed class BinaryContext : DbContext
    {
        public BinaryContext(string provider) : base(Connection(provider), true) { Database.SetInitializer<BinaryContext>(null); }
        private static DbConnection Connection(string provider) => provider switch
        {
            "PostgreSql" => new NpgsqlConnection("Host=unused;Database=unused"),
            "Sqlite" => new SQLiteConnection("Data Source=:memory:"),
            _ => new SqlConnection("Server=unused;Database=unused"),
        };
        protected override void OnModelCreating(DbModelBuilder model)
        {
            model.Entity<BinaryProjectionParent>().HasKey(x => new { x.Tenant, x.Id });
            model.Entity<BinaryProjectionParent>().Property(x => x.Tenant).HasMaxLength(16);
            model.Entity<BinaryProjectionChild>().HasKey(x => x.Id);
            model.Entity<BinaryProjectionChild>().Property(x => x.ParentTenant).HasMaxLength(16);
            model.Entity<BinaryProjectionParent>().HasMany(x => x.Children).WithRequired().HasForeignKey(x => new { x.ParentTenant, x.ParentId });
        }
    }
}
