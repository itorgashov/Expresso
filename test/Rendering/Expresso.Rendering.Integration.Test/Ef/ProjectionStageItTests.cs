#if NET8_0_OR_GREATER
using Expresso.Core.Projection;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Expresso.Rendering.Integration.Test.Ef;

public sealed partial class ProjectionCompositionItTests
{
    [SkippableTheory]
    [MemberData(nameof(StageScopes))]
    public async Task Projection_Stages_PreserveActualRows(string shape, bool explicitKeys, bool adapter)
    {
        // Scenario: signature, wrapper, saved-expression, conditional and Unicode combinations execute against actual private fixture rows.
        Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        ProjectionDdl.Reset(connection, ParameterBinder.At);
        using var db = new Context(new DbContextOptionsBuilder<Context>().UseSqlite(connection).Options);
        var parents = db.Set<ProjectionParent>();
        var source = Stage(parents, shape);
        var mapping = ProjectionFixture.Mapping();
        var projection = ProjectionFixture.Nested();
        if (shape == "unicode")
        {
            const string name = "\u03ba\u03cc\u03c3\u03bc\u03bf\u03c2";
            mapping = new LinqProjectionMapping<ProjectionParent, ProjectionParentDto>().Key("tenant", p => p.Tenant).Key("id", p => p.Id)
                .Field(name, "title", p => p.Title, p => p.Title)
                .Collection("children" + name, "children", p => p.Children, p => p.Children,
                    new LinqProjectionMapping<ProjectionChild, ProjectionChildDto>().Key("tenant", c => c.Tenant).Key("id", c => c.Id)
                        .Field(name, "name", c => c.Name, c => c.Name), new LinqQueryMapping<ProjectionChild>());
            projection = new ProjectionDirective(new[] { new ProjectionSelection(name.ToUpperInvariant()),
                new ProjectionSelection("children" + name.ToUpperInvariant(), new[] { new ProjectionSelection(name.ToUpperInvariant()) }) });
        }
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var result = adapter
            ? await source.ToProjectedListAsync(transformer, projection, mapping, keySource: explicitKeys ? parents : null)
            : await transformer.RenderProjection(projection, mapping).ExecuteAsync(source, new Executor(), keySource: explicitKeys ? parents : null);
        var generator = shape is "empty-any" or "empty-count" or "unicode";
        Assert.Equal(shape == "conditional-false" ? 0 : shape == "saved-selector" ? 9 : generator ? 3 : 2, result.Items.Count);
        Assert.All(result.Items.Where(p => p.Title == "one"), p =>
        {
            Assert.Equal(new[] { "alpha", "beta" }, p.Children.Select(c => c.Name).OrderBy(n => n));
            if (shape != "unicode") Assert.Equal("first", Assert.Single(p.Children.SelectMany(c => c.Leaves)).Value);
        });
        Assert.All(result.Items.Where(p => p.Title == "empty"), p => Assert.Empty(p.Children));
        Assert.All(ProjectionShaper.ToSparseObjects(result), row => Assert.DoesNotContain("notes", row.Keys));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    public static IEnumerable<object[]> StageScopes()
    {
        foreach (var shape in new[] { "empty-any", "empty-count", "conditional-true", "conditional-false", "saved-predicate", "saved-selector", "unicode",
            "TagWith", "TagWithCallSite", "AsTracking", "AsNoTracking", "IdentityResolution", "IgnoreAutoIncludes", "IgnoreQueryFilters", "AsSplitQuery", "AsSingleQuery" })
        foreach (var keys in new[] { false, true })
        foreach (var adapter in new[] { false, true })
            yield return new object[] { shape, keys, adapter };
    }

    private static IQueryable<ProjectionParent> Stage(IQueryable<ProjectionParent> parents, string shape)
    {
        if (shape == "unicode") return parents;
        if (shape == "empty-any") return parents.Where(p => !Enumerable.Empty<int>().Any());
        if (shape == "empty-count") return parents.Where(p => p.Id > Enumerable.Empty<int>().Count());
        if (shape == "saved-predicate")
        {
            Expression<Func<ProjectionChild, bool>> predicate = c => c.Id > 0;
            return parents.Where(p => p.Children.AsQueryable().Where(predicate).Any());
        }
        if (shape == "saved-selector")
        {
            Expression<Func<ProjectionParent, IEnumerable<ProjectionChild>>> selector = p => p.Children;
            return parents.SelectMany(_ => parents.SelectMany(selector, (p, c) => p));
        }
        if (shape.StartsWith("conditional", StringComparison.Ordinal))
        {
            var positive = shape == "conditional-true";
            return parents.Select(p => positive
                ? new { Parent = p, Kids = p.Children.Where(c => c.Id > 0) }
                : new { Parent = p, Kids = p.Children.Where(c => c.Id < 0) }).Where(row => row.Kids.Any()).Select(row => row.Parent);
        }
        return Wrap(parents.Select(p => new { Parent = p, Kids = p.Children }), shape).Where(row => row.Kids.Any()).Select(row => row.Parent);
    }

    private static IQueryable<T> Wrap<T>(IQueryable<T> source, string shape) where T : class => shape switch
    {
        "TagWith" => source.TagWith("projection stage"),
        "TagWithCallSite" => source.TagWithCallSite(),
        "AsTracking" => source.AsTracking(),
        "AsNoTracking" => source.AsNoTracking(),
        "IdentityResolution" => source.AsNoTrackingWithIdentityResolution(),
        "IgnoreAutoIncludes" => source.IgnoreAutoIncludes(),
        "IgnoreQueryFilters" => source.IgnoreQueryFilters(),
        "AsSplitQuery" => source.AsSplitQuery(),
        _ => source.AsSingleQuery()
    };
}
#endif
