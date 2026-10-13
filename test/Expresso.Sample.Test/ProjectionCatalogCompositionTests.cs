using Expresso.Core.Filtering;
using Expresso.Core.Projection;
using Expresso.Parsing;
using Expresso.Rendering;
using Expresso.Rendering.Linq;

namespace Expresso.Sample.Test;

public sealed class ProjectionCatalogCompositionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("children")]
    [InlineData("children.name")]
    [InlineData("children,children.name")]
    [InlineData("children.name,children")]
    public async Task RestrictedCatalog_ConstrainsBroaderSqlAndLinqMappings(string? input)
    {
        // Scenario: endpoint permissions exclude secrets/nested collections while the reusable mapping contains them.
        var model = new QueryModel(new[] { ("title", typeof(string)) }, new[] {
            new CollectionModel("children", new QueryModel(new[] { ("name", typeof(string)) })) });
        var projection = new ProjectionDirectiveParser().Parse(input, model);
        var sql = new ExpressionToSqlServerQueryClauseTransformer().RenderProjection(projection, SqlMapping());
        Assert.Equal(input is null ? new[] { "title" } : Array.Empty<string>(), sql.Fields.Select(f => f.Name));
        var child = Assert.Single(sql.Collections).Items;
        Assert.Equal("name", Assert.Single(child.Fields).Name);
        Assert.Empty(child.Collections);
        Assert.DoesNotContain("p.secret", sql.SelectList);
        Assert.DoesNotContain("c.secret", child.SelectList);

        var data = new[] { new Parent { Id = 1, Title = "visible", Secret = "private", Children = new() {
            new Child { Id = 2, Name = "child", Secret = "private", Leaves = new() { new Leaf { Id = 3, Value = "private" } } } } } };
        var result = await new QueryableExpressionToLinqTransformer().RenderProjection(projection, LinqMapping())
            .ExecuteAsync(data.AsQueryable(), new MemoryExecutor());
        var shape = Assert.Single(ProjectionShaper.ToSparseObjects(result));
        Assert.Equal(input is null ? 2 : 1, shape.Count);
        var shapedChild = Assert.Single((IReadOnlyDictionary<string, object?>[])shape["children"]!);
        Assert.Equal("name", Assert.Single(shapedChild).Key);
        Assert.Equal("child", shapedChild["name"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmptyCatalog_NeverExpandsToMappingFields(bool collection)
    {
        var catalog = collection ? new QueryModel(null, new[] { new CollectionModel("children", QueryModel.Empty) }) : QueryModel.Empty;
        var projection = new ProjectionDirectiveParser().Parse(null, catalog);
        var sql = new ExpressionToSqlServerQueryClauseTransformer().RenderProjection(projection, SqlMapping());
        Assert.Empty(sql.Fields);
        if (collection) { Assert.Empty(Assert.Single(sql.Collections).Items.Fields); Assert.Empty(sql.Collections[0].Items.Collections); }
        else Assert.Empty(sql.Collections);
        var data = new[] { new Parent { Id = 1, Title = "private", Children = new() { new Child { Id = 2, Name = "private" } } } };
        var result = await new QueryableExpressionToLinqTransformer().RenderProjection(projection, LinqMapping())
            .ExecuteAsync(data.AsQueryable(), new MemoryExecutor());
        var shape = Assert.Single(ProjectionShaper.ToSparseObjects(result));
        if (collection) Assert.Empty(Assert.Single((IReadOnlyDictionary<string, object?>[])shape["children"]!));
        else Assert.Empty(shape);
    }

    private static SqlProjectionMapping SqlMapping()
    {
        var leaf = new SqlProjectionMapping().Key("id", "l.id", typeof(int)).Field("value", "value", "l.value", typeof(string));
        var child = new SqlProjectionMapping().Key("id", "c.id", typeof(int)).Field("name", "name", "c.name", typeof(string))
            .Field("secret", "secret", "c.secret", typeof(string)).Collection("leaves", "leaves",
                new SqlCollectionProjectionMapping("leaf l", leaf, new[] { new SqlProjectionKey("id", "l.child_id", typeof(int)) }));
        return new SqlProjectionMapping().Key("id", "p.id", typeof(int)).Field("title", "title", "p.title", typeof(string))
            .Field("secret", "secret", "p.secret", typeof(string)).Collection("children", "children",
                new SqlCollectionProjectionMapping("child c", child, new[] { new SqlProjectionKey("id", "c.parent_id", typeof(int)) }));
    }

    private static LinqProjectionMapping<Parent, Parent> LinqMapping()
    {
        var leaf = new LinqProjectionMapping<Leaf, Leaf>().Key("id", l => l.Id).Field("value", "value", l => l.Value, l => l.Value);
        var child = new LinqProjectionMapping<Child, Child>().Key("id", c => c.Id).Field("name", "name", c => c.Name, c => c.Name)
            .Field("secret", "secret", c => c.Secret, c => c.Secret).Collection("leaves", "leaves", c => c.Leaves, c => c.Leaves, leaf, new LinqQueryMapping<Leaf>());
        return new LinqProjectionMapping<Parent, Parent>().Key("id", p => p.Id).Field("title", "title", p => p.Title, p => p.Title)
            .Field("secret", "secret", p => p.Secret, p => p.Secret).Collection("children", "children", p => p.Children, p => p.Children, child, new LinqQueryMapping<Child>());
    }

    private sealed class MemoryExecutor : IProjectionQueryExecutor
    {
        public Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<T>>(query.ToList());
    }
    public sealed class Parent
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Secret { get; set; } = "";
        public List<Child> Children { get; set; } = new();
    }
    public sealed class Child
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Secret { get; set; } = "";
        public List<Leaf> Leaves { get; set; } = new();
    }
    public sealed class Leaf { public int Id { get; set; } public string Value { get; set; } = ""; }
}
