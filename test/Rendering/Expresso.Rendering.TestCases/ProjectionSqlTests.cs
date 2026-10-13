using Expresso.Core.Projection;

namespace Expresso.Rendering.TestCases;

public abstract partial class ProjectionSqlTests
{
    [Fact]
    public void CyclicMappingsAreRejected_ButSharedSubtreesAreAllowed()
    {
        var first = new SqlProjectionMapping().Key("id", "p.id", typeof(int));
        var second = new SqlProjectionMapping().Key("id", "c.id", typeof(int));
        SqlCollectionProjectionMapping Child(SqlProjectionMapping map) => new SqlCollectionProjectionMapping("child c", map,
            new[] { new SqlProjectionKey("id", "c.parent_id", typeof(int)) });
        Assert.Throws<ArgumentException>(() => first.Collection("self", "self", Child(first)));
        first.Collection("children", "children", Child(second));
        Assert.Throws<ArgumentException>(() => second.Collection("back", "back", Child(first)));
        var third = new SqlProjectionMapping().Key("id", "g.id", typeof(int)).Collection("children", "children", Child(first));
        third.Collection("shared", "shared", Child(second));
        Assert.Equal(2, _renderer.RenderProjection(ProjectionDirective.All, third).Collections.Count);
    }

    [Fact]
    public void NullableCorrelationAndTrustedExpressionsAreSupported()
    {
        var map = new SqlProjectionMapping().Key("id", "p.id", typeof(int?))
            .Field("computed", "computed", "COALESCE(p.title, 'empty')", typeof(string))
            .Collection("children", "children", new SqlCollectionProjectionMapping("child c",
                new SqlProjectionMapping().Key("id", "c.id", typeof(int)).Field("name", "name", "c.name", typeof(string)),
                new[] { new SqlProjectionKey("id", "c.parent_id", typeof(int?)) }));
        var plan = _renderer.RenderProjection(ProjectionDirective.All, map);
        Assert.StartsWith("COALESCE(p.title, 'empty') AS ", plan.SelectList);
        Assert.Equal(typeof(int?), plan.Columns[1].ValueType);
        Assert.Equal(1, plan.Columns[1].Ordinal);
        Assert.Equal("computed", plan.Fields[0].Name);
        Assert.Equal(0, plan.Fields[0].Ordinal);
        Assert.Equal("children", plan.Collections[0].Name);
        Assert.Throws<ArgumentException>(() => new SqlCollectionProjectionMapping("c", map, new SqlProjectionKey[] { null! }));
        Assert.Throws<ArgumentException>(() => new SqlCollectionProjectionMapping("c", map,
            new[] { new SqlProjectionKey("id", "id", typeof(int)), new SqlProjectionKey("ID", "id", typeof(int)) }));
        Assert.Throws<ArgumentException>(() => new SqlCollectionProjectionMapping("", map, new[] { new SqlProjectionKey("id", "id", typeof(int)) }));
        var collection = map.Collections["children"].Mapping;
        Assert.Throws<InvalidOperationException>(() => map.Key("late", "p.extra", typeof(int)));
        Assert.Throws<ArgumentException>(() => new SqlProjectionMapping().Key("other", "p.id", typeof(int)).Collection("c", "c", collection));
        Assert.Throws<ArgumentException>(() => new SqlProjectionMapping().Key("id", "p.id", typeof(int)).Key("tenant", "p.tenant", typeof(int)).Collection("c", "c", collection));
        Assert.Throws<ArgumentException>(() => map.Field("CHILDREN", "different", "p.id", typeof(int)));
        Assert.Throws<ArgumentException>(() => map.Field("different", "children", "p.id", typeof(int)));
        Assert.Throws<ArgumentException>(() => map.Collection("other", "computed", collection));
        Assert.Throws<ArgumentException>(() => map.Collection("children", "other", collection));
        Assert.Throws<ArgumentException>(() => _renderer.RenderProjection(new ProjectionDirective(new[] {
            new ProjectionSelection("computed", new[] { new ProjectionSelection("x") }) }), map));
    }
    [Fact]
    public void ChildProjectionWithoutIdentity_IsRejectedBeforeExecution()
    {
        // Scenario: parent correlation exists but child identity is absent; a reader cannot deduplicate such rows.
        var child = new SqlProjectionMapping().Field("name", "name", "c.name", typeof(string));
        var map = new SqlProjectionMapping().Key("id", "p.id", typeof(int))
            .Collection("children", "children", new SqlCollectionProjectionMapping("child c", child,
                new[] { new SqlProjectionKey("id", "c.parent_id", typeof(int)) }));
        Assert.Throws<ArgumentException>(() => _renderer.RenderProjection(ProjectionDirective.All, child));
        Assert.Throws<ArgumentException>(() => _renderer.RenderProjection(ProjectionDirective.All, map));
    }

    private readonly ExpressionToSqlQueryClauseTransformerBase _renderer;
    private readonly string _open;
    private readonly string _close;
    protected ProjectionSqlTests(ExpressionToSqlQueryClauseTransformerBase renderer, string open, string close)
    { _renderer = renderer; _open = open; _close = close; }
    private string Q(string value) => string.Join(".", value.Split('.').Select(p => _open + p + _close));
    private static SqlProjectionMapping Mapping() => new SqlProjectionMapping()
        .Field("title", "title", "p.title", typeof(string)).Field("id", "id", "p.id", typeof(int))
        .Key("tenant", "p.tenant", typeof(int)).Key("id", "p.id", typeof(int))
        .Collection("children", "children", new SqlCollectionProjectionMapping("child c",
            new SqlProjectionMapping().Key("tenant", "c.tenant", typeof(int)).Key("id", "c.id", typeof(int))
                .Field("name", "name", "c.name", typeof(string)),
            new[] { new SqlProjectionKey("tenant", "c.tenant", typeof(int)), new SqlProjectionKey("id", "c.parent_id", typeof(int)) }));

    [Fact]
    public void SelectList_OnlyPublicSelectionAndHiddenKeys()
    {
        var result = _renderer.RenderProjection(new ProjectionDirective(new[] { new ProjectionSelection("title") }), Mapping());
        Assert.Equal(Q("p.title") + " AS " + Q("c0") + ", " + Q("p.tenant") + " AS " + Q("c1") + ", " + Q("p.id") + " AS " + Q("c2"), result.SelectList);
        Assert.Equal(new[] { 1, 2 }, result.KeyOrdinals);
        Assert.Single(result.Fields);
        Assert.Empty(result.Collections);
        Assert.Equal("title", result.Fields[0].OutputName);
    }

    [Fact]
    public void All_DeduplicatesVisibleAndHiddenKeys_AndPlansChildren()
    {
        var result = _renderer.RenderProjection(ProjectionDirective.All, Mapping());
        Assert.Equal(3, result.Columns.Count);
        Assert.Equal(new[] { 2, 1 }, result.KeyOrdinals);
        Assert.Equal(new[] { "title", "id" }, result.Fields.Select(f => f.Name));
        var child = Assert.Single(result.Collections);
        Assert.Equal("children", child.OutputName);
        Assert.Equal(new[] { 1, 3 }, child.Items.ParentKeyOrdinals);
        Assert.Equal(new[] { 1, 2 }, child.Items.KeyOrdinals);
        Assert.Equal(4, child.Items.Columns.Count);
        Assert.Equal("child c", child.Mapping.FromClause);
    }

    [Fact]
    public void ExplicitChild_AndBareChild_SelectOnlyRequestedSubtree()
    {
        foreach (var selection in new[] {
            new ProjectionSelection("children", selectDefaultSubtree: true),
            new ProjectionSelection("children", new[] { new ProjectionSelection("name") }) })
        {
            var result = _renderer.RenderProjection(new ProjectionDirective(new[] { selection }), Mapping());
            Assert.Empty(result.Fields);
            Assert.Single(result.Collections[0].Items.Fields);
        }
    }

    [Fact]
    public void InvalidMappingsAndDirectives_FailEarly()
    {
        Assert.Throws<ArgumentNullException>(() => _renderer.RenderProjection(null!, Mapping()));
        Assert.Throws<ArgumentNullException>(() => _renderer.RenderProjection(ProjectionDirective.All, null!));
        Assert.Throws<ArgumentException>(() => _renderer.RenderProjection(ProjectionDirective.All, new SqlProjectionMapping()));
        Assert.Throws<ArgumentException>(() => _renderer.RenderProjection(new ProjectionDirective(new[] { new ProjectionSelection("missing") }), Mapping()));
        Assert.Throws<ArgumentException>(() => _renderer.RenderProjection(new ProjectionDirective(new[] {
            new ProjectionSelection("title", selectDefaultSubtree: true) }), Mapping()));
        Assert.Throws<ArgumentException>(() => new SqlProjectionMapping().Key("id", "a.id", typeof(int)).Key("ID", "a.id", typeof(int)));
        Assert.Throws<ArgumentException>(() => new SqlProjectionMapping().Field("", "x", "x", typeof(int)));
        Assert.Throws<ArgumentException>(() => new SqlProjectionMapping().Field("x", "", "x", typeof(int)));
        Assert.Throws<ArgumentException>(() => new SqlProjectionMapping().Field("x", "x", "x", typeof(int)).Field("X", "y", "y", typeof(int)));
        Assert.Throws<ArgumentException>(() => new SqlProjectionMapping().Field("x", "x", "x", typeof(int)).Field("y", "x", "y", typeof(int)));
        Assert.Throws<ArgumentException>(() => new SqlProjectionKey("x", "", typeof(int)));
        Assert.Throws<ArgumentNullException>(() => new SqlProjectionKey("x", "x", null!));
        Assert.Throws<ArgumentNullException>(() => new SqlCollectionProjectionMapping("c", null!, new[] { new SqlProjectionKey("id", "c.id", typeof(int)) }));
        Assert.Throws<ArgumentNullException>(() => new SqlCollectionProjectionMapping("c", new SqlProjectionMapping(), null!));
        Assert.Throws<ArgumentException>(() => new SqlCollectionProjectionMapping("c", new SqlProjectionMapping(), Array.Empty<SqlProjectionKey>()));
        var collection = new SqlCollectionProjectionMapping("c", new SqlProjectionMapping().Key("id", "c.id", typeof(int)),
            new[] { new SqlProjectionKey("id", "c.parent_id", typeof(int)) }, "c.id", new SqlQueryMapping(new Dictionary<string, string>()));
        Assert.Equal("c.id", collection.DefaultOrderBy);
        Assert.NotNull(collection.QueryMapping);
        Assert.Throws<ArgumentException>(() => new SqlProjectionMapping().Collection("c", "c", collection));
        Assert.Throws<ArgumentException>(() => new SqlProjectionMapping().Key("id", "p.id", typeof(string)).Collection("c", "c", collection));
        Assert.Throws<ArgumentNullException>(() => new SqlProjectionMapping().Collection("c", "c", null!));
    }
}
