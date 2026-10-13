#if NET8_0_OR_GREATER
using Expresso.Core.Projection;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Expresso.Rendering.Integration.Test.Ef;

[Trait("Category", "Integration")]
public sealed partial class ProjectionDerivedCastItTests
{
    [SkippableTheory]
    [MemberData(nameof(Scopes))]
    public async Task Projection_DerivedCasts_KeepMappedGraph(string shape, bool tpt, bool mapping, bool explicitKeys, bool adapter)
    {
        // Scenario: derived navigation casts in filters/mappings, TPH/TPT and both execution/key modes; execute the actual split graph.
        Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var db = tpt ? new TptContext(connection) : new Context(connection);
        await db.Database.EnsureCreatedAsync(); // Only private in-memory widget_cast_* tables.
        var data = DerivedCastProjectionFixture.Data();
        var special = (CastSpecialNode)data[0];
        special.Target = null;
        special.TargetId = null;
        db.AddRange(data);
        await db.SaveChangesAsync();
        if (tpt) await db.Database.ExecuteSqlRawAsync("UPDATE widget_cast_special SET TargetId = 1 WHERE Id = 1");
        else await db.Database.ExecuteSqlRawAsync("UPDATE widget_cast_nodes SET TargetId = 1 WHERE Id = 1");
        db.ChangeTracker.Clear();
        var nodes = db.Set<CastNode>();
        Expression<Func<CastNode, IEnumerable<CastItem>>> navigation = shape switch
        {
            "as" => n => (n as CastSpecialNode)!.Special,
            "property" => n => EF.Property<List<CastItem>>((CastSpecialNode)n, "Special"),
            "reference" => n => ((CastSpecialNode)n).Target!.Common,
            "ef-reference" => n => EF.Property<CastNode>((CastSpecialNode)n, "Target").Common,
            "boxed" => n => EF.Property<List<CastItem>>((object)(CastSpecialNode)n, "Special"),
            "inherited" => n => ((CastSpecialNode)n).Common,
            _ => n => ((CastSpecialNode)n).Special
        };
        var predicate = Expression.Lambda<Func<CastNode, bool>>(Expression.AndAlso(
            Expression.TypeIs(navigation.Parameters[0], typeof(CastSpecialNode)),
            Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), new[] { typeof(CastItem) }, navigation.Body)), navigation.Parameters);
        var source = mapping ? nodes.OfType<CastSpecialNode>().Cast<CastNode>() : nodes.Where(predicate);
        var map = DerivedCastProjectionFixture.Mapping(mapping ? navigation : null);
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var result = adapter ? await source.ToProjectedListAsync(transformer, ProjectionDirective.All, map, keySource: explicitKeys ? nodes : null)
            : await transformer.RenderProjection(ProjectionDirective.All, map).ExecuteAsync(source, new Executor(), keySource: explicitKeys ? nodes : null);
        Assert.Equal("special", Assert.Single(result.Items).Name);
        Assert.Equal("item", Assert.Single(result.Items[0].Items).Name);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    public static IEnumerable<object[]> Scopes()
    {
        foreach (var shape in new[] { "cast", "as", "property", "reference", "ef-reference", "boxed", "inherited" })
        foreach (var tpt in new[] { false, true })
        foreach (var mapping in new[] { false, true })
        foreach (var keys in new[] { false, true })
        foreach (var adapter in new[] { false, true })
            yield return new object[] { shape, tpt, mapping, keys, adapter };
    }

    private class Context : DbContext
    {
        internal Context(SqliteConnection connection) : base(new DbContextOptionsBuilder<Context>().UseSqlite(connection).Options) { }
        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<CastNode>().ToTable("widget_cast_nodes").Property(n => n.Id).ValueGeneratedNever();
            model.Entity<CastItem>().ToTable("widget_cast_items").Property(i => i.Id).ValueGeneratedNever();
            model.Entity<CastNode>().HasMany(n => n.Common).WithOne().HasForeignKey(i => i.CommonId);
            model.Entity<CastSpecialNode>().HasMany(n => n.Special).WithOne().HasForeignKey(i => i.SpecialId);
            model.Entity<CastSpecialNode>().HasOne(n => n.Target).WithMany().HasForeignKey(n => n.TargetId);
        }
    }
    private sealed class TptContext : Context
    {
        internal TptContext(SqliteConnection connection) : base(connection) { }
        protected override void OnModelCreating(ModelBuilder model) { base.OnModelCreating(model); model.Entity<CastSpecialNode>().ToTable("widget_cast_special"); }
    }
    private sealed class Executor : IProjectionQueryExecutor
    {
        public async Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) => await query.ToListAsync(cancellationToken);
    }
}
#endif
