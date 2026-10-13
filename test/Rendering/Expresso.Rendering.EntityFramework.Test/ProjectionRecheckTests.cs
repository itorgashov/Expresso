using System.Data.Entity;
using System.Data.SqlClient;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.EntityFramework.Test;

public sealed class ProjectionRecheckTests
{
    [Theory]
    [InlineData("include", false)]
    [InlineData("include", true)]
    [InlineData("identity", false)]
    [InlineData("identity", true)]
    public async Task WrappedWindow_RejectsImplicitSource_AndAllowsExplicitSource(string wrapper, bool explicitKeySource)
    {
        // Scenario: a trailing provider operator hides Take. Fail before I/O, or use explicit scope despite a page-shifting insert.
        using var db = new SharedProjectionContext();
        var data = SharedProjectionFixture.Data(true);
        var sql = new List<string>();
        var reads = 0;
        var executor = new ProjectionSqlExecutor<SharedBook>(data.AsQueryable(), q => sql.Add(q.ToString()!), () =>
        {
            if (reads++ == 0) data.Add(new SharedBook { Tenant = 1, Id = 0, Title = "shift" });
        });
        var transformer = new Ef6ExpressionToLinqTransformer(db);
        var plan = transformer.RenderProjection(SharedProjectionFixture.Nested(), SharedProjectionFixture.Mapping());
        IQueryable<SharedBook> query = db.Books.Where(b => b.Tenant == 1).OrderBy(b => b.Id).Take(1);
        query = wrapper switch
        {
            "include" => query.Include(b => b.Authors),
            _ => query.Select(b => b),
        };
        if (!explicitKeySource)
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => plan.ExecuteAsync(query, executor));
            Assert.Contains("keySource", error.Message);
            Assert.Empty(sql);
            return;
        }
        var result = await plan.ExecuteAsync(query, executor, keySource: db.Books.Where(b => b.Tenant == 1));
        Assert.Equal("B1", Assert.Single(result.Items).Title);
        Assert.Equal(2, Assert.Single(result.Items[0].Authors).Awards.Count);
        Assert.Equal(new[] { 1, 1, 2 }, executor.RowCounts);
        Assert.All(sql.Skip(1), command => Assert.DoesNotContain("TOP(", command));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SharedAuthors_ExcludeXmlAndEntityDistinct_AndReadEachAwardOnce(bool incompleteKeys, bool defaultProjection)
    {
        // Scenario: composite-key author shared by ten books; one page contains two links. Nullable awards remain distinct rows.
        using var db = new SharedProjectionContext();
        var commands = new List<string>();
        var executor = new ProjectionSqlExecutor<SharedBook>(SharedProjectionFixture.Data(incompleteKeys).AsQueryable(),
            q => commands.Add(q.ToString()!));
        var transformer = new Ef6ExpressionToLinqTransformer(db);
        var projection = defaultProjection ? Expresso.Core.Projection.ProjectionDirective.All : SharedProjectionFixture.Nested();
        var result = await transformer.RenderProjection(projection, SharedProjectionFixture.Mapping())
            .ExecuteAsync(db.Books.Where(b => b.Tenant == 1).OrderBy(b => b.Id).Take(2), executor,
                options: new ProjectionExecutionOptions { MaxKeyParameters = 2 }, keySource: db.Books);
        Assert.Equal(new[] { 2, 1, 2, 1, 2 }, executor.RowCounts);
        Assert.All(result.Items, book => Assert.Equal(2, Assert.Single(book.Authors).Awards.Count));
        Assert.All(commands, command =>
        {
            var select = command.Substring(command.IndexOf("SELECT", StringComparison.Ordinal));
            Assert.DoesNotContain("Bio", select.Substring(0, select.IndexOf("FROM", StringComparison.Ordinal)));
            Assert.DoesNotContain("DISTINCT", command);
        });
    }
}

[DbConfigurationType(typeof(TestEf6Configuration))]
public sealed class SharedProjectionContext : DbContext
{
    public SharedProjectionContext() : base(new SqlConnection("Server=unused;Database=unused"), true)
    { Database.SetInitializer<SharedProjectionContext>(null); }
    public DbSet<SharedBook> Books => Set<SharedBook>();
    protected override void OnModelCreating(DbModelBuilder model)
    {
        model.Entity<SharedBook>().HasKey(x => new { x.Tenant, x.Id });
        model.Entity<SharedAuthor>().HasKey(x => new { x.Tenant, x.Id });
        model.Entity<SharedAuthor>().Property(x => x.Bio).HasColumnType("xml");
        model.Entity<SharedAward>().HasKey(x => new { x.Tenant, x.Id });
        model.Entity<SharedBook>().HasMany(x => x.Authors).WithMany().Map(m =>
            m.ToTable("shared_book_author").MapLeftKey("BookTenant", "BookId").MapRightKey("AuthorTenant", "AuthorId"));
        model.Entity<SharedAuthor>().HasMany(x => x.Awards).WithRequired().HasForeignKey(x => new { x.Tenant, x.AuthorId });
    }
}
