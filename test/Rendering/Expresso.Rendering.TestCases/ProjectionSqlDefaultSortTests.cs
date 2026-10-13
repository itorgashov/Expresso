using Expresso.Core.Projection;

namespace Expresso.Rendering.TestCases;

public abstract partial class ProjectionSqlTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GrandchildOnlySort_KeepsIntermediateDefaultOrder(bool explicitParent)
    {
        // Scenario: the SQL path and EF path share local-default semantics; descendant sort never replaces parent ordering.
        var leaf = new SqlProjectionMapping().Key("id", "l.id", typeof(int)).Field("value", "value", "l.value", typeof(string));
        var child = new SqlProjectionMapping().Key("id", "c.id", typeof(int)).Field("name", "name", "c.name", typeof(string))
            .Collection("leaves", "leaves", new SqlCollectionProjectionMapping("leaf l", leaf,
                new[] { new SqlProjectionKey("id", "l.child_id", typeof(int)) }, Q("l.value") + " ASC",
                new SqlQueryMapping(new Dictionary<string, string> { ["value"] = "l.value" })));
        var parent = new SqlProjectionMapping().Key("id", "p.id", typeof(int)).Collection("children", "children",
            new SqlCollectionProjectionMapping("child c", child, new[] { new SqlProjectionKey("id", "c.parent_id", typeof(int)) },
                Q("c.name") + " ASC", new SqlQueryMapping(new Dictionary<string, string> { ["name"] = "c.name" })));
        var io = new SqlIo((index, plan) => index == 0 ? new[] { Values(plan, ("p.id", 1)) } : index == 1
            ? (explicitParent ? new[] { ("beta", 1), ("alpha", 2) } : new[] { ("alpha", 2), ("beta", 1) })
                .Select(item => Values(plan, ("c.name", item.Item1), ("c.id", item.Item2), ("c.parent_id", 1))).ToArray()
            : new[] { Values(plan, ("l.id", 2), ("l.value", "zulu"), ("l.child_id", 1)), Values(plan, ("l.id", 1), ("l.value", "first"), ("l.child_id", 1)) });
        var result = await new SqlProjectionExecutor(_renderer, _renderer).ExecuteAsync(_renderer.RenderProjection(ProjectionDirective.All, parent),
            "root", new Dictionary<string, object>(), io, OrderedProjectionFixture.Sort(explicitParent));
        Assert.EndsWith("ORDER BY " + Q("c.name") + (explicitParent ? " DESC" : " ASC"), io.Sql[1]);
        Assert.EndsWith("ORDER BY " + Q("l.value") + " DESC", io.Sql[2]);
        var items = ((IEnumerable<IReadOnlyDictionary<string, object?>>)result[0]["children"]!).ToArray();
        Assert.Equal(explicitParent ? new[] { "beta", "alpha" } : new[] { "alpha", "beta" }, items.Select(i => (string)i["name"]!));
    }
}
