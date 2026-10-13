using System.Collections;
using Expresso.Core.Projection;

namespace Expresso.Rendering.TestCases;

public abstract partial class ProjectionSqlTests
{
    [Theory]
    [InlineData("IgnoreCase", false)]
    [InlineData("IgnoreCase", true)]
    [InlineData("Ordinal", false)]
    [InlineData("Ordinal", true)]
    [InlineData("Padded", false)]
    [InlineData("Padded", true)]
    public async Task ConfiguredTextEquality_AttachesAndDeduplicatesAtEveryDepth(string equality, bool composite)
    {
        // Scenario: DB-eligible foreign representations differ; per-level equality retains original values and collapses duplicate joins.
        IEqualityComparer text = equality == "IgnoreCase" ? StringComparer.OrdinalIgnoreCase : equality == "Padded" ? new PaddingComparer() : StringComparer.Ordinal;
        string Variant(string value) => equality == "IgnoreCase" ? value.ToLowerInvariant() : equality == "Padded" ? value + "  " : value;
        SqlProjectionMapping Keys(string alias)
        {
            var map = new SqlProjectionMapping();
            if (composite) map.Key("tenant", alias + ".tenant", typeof(int));
            return map.Key("code", alias + ".code", typeof(string)).UseKeyComparer(composite
                ? new CompositeProjectionKeyComparer(null, text) : new CompositeProjectionKeyComparer(text));
        }
        SqlProjectionKey[] Correlation(string alias, string column) => composite
            ? new[] { new SqlProjectionKey("tenant", alias + ".tenant", typeof(int)), new SqlProjectionKey("code", alias + "." + column, typeof(string)) }
            : new[] { new SqlProjectionKey("code", alias + "." + column, typeof(string)) };
        var leaf = new SqlProjectionMapping().Key("id", "l.id", typeof(int)).Field("value", "value", "l.value", typeof(string));
        var child = Keys("c").Field("name", "name", "c.name", typeof(string)).Collection("leaves", "leaves",
            new SqlCollectionProjectionMapping("leaf l", leaf, Correlation("l", "child_code")));
        var parent = Keys("p").Field("title", "title", "p.title", typeof(string)).Collection("children", "children",
            new SqlCollectionProjectionMapping("child c", child, Correlation("c", "parent_code")));
        var plan = _renderer.RenderProjection(ProjectionDirective.All, parent);
        Assert.Same(parent.KeyComparer, plan.KeyComparer);
        var io = new SqlIo((index, p) => index == 0
            ? new[] { Values(p, ("p.title", "root"), ("p.tenant", 7), ("p.code", "PARENT-A")) }
            : index == 1
                ? new[] { Values(p, ("c.name", "child"), ("c.tenant", 7), ("c.code", "CHILD-A"), ("c.parent_code", Variant("PARENT-A"))),
                    Values(p, ("c.name", "duplicate"), ("c.tenant", 7), ("c.code", Variant("CHILD-A")), ("c.parent_code", "PARENT-A")) }
                : new[] { Values(p, ("l.id", 1), ("l.value", "leaf"), ("l.tenant", 7), ("l.child_code", Variant("CHILD-A"))) });
        var result = await new SqlProjectionExecutor(_renderer, _renderer).ExecuteAsync(plan, "root", new Dictionary<string, object>(), io,
            maxKeyParameters: composite ? 2 : 1);
        var attached = Assert.Single((IEnumerable<IReadOnlyDictionary<string, object?>>)Assert.Single(result)["children"]!);
        Assert.Equal("child", attached["name"]);
        Assert.Equal("leaf", Assert.Single((IEnumerable<IReadOnlyDictionary<string, object?>>)attached["leaves"]!)["value"]);
        Assert.Equal(3, io.Sql.Count);
        Assert.Contains("PARENT-A", io.Parameters[1].Values);
        Assert.Contains("CHILD-A", io.Parameters[2].Values);
        Assert.DoesNotContain("code", result[0].Keys);
    }

    [Fact]
    public void KeyComparer_RejectsInvalidConfiguration_AndKeepsDefaultOrdinalEquality()
    {
        var map = new SqlProjectionMapping().Key("code", "p.code", typeof(string));
        Assert.Throws<ArgumentNullException>(() => map.UseKeyComparer(null!));
        Assert.False(map.KeyComparer.Equals(new CompositeProjectionKey(new object?[] { "A" }), new CompositeProjectionKey(new object?[] { "a" })));
        map.UseKeyComparer(new CompositeProjectionKeyComparer(null, StringComparer.OrdinalIgnoreCase));
        Assert.Throws<ArgumentException>(() => _renderer.RenderProjection(ProjectionDirective.All, map));
        Assert.Throws<ArgumentException>(() => _renderer.RenderProjection(new ProjectionDirective(new[] {
            ProjectionSelection.Subtree("title", ProjectionDirective.Empty) }), Mapping()));
    }

    private sealed class PaddingComparer : IEqualityComparer
    {
        public new bool Equals(object? left, object? right) => StringComparer.Ordinal.Equals((left as string)?.TrimEnd(' '), (right as string)?.TrimEnd(' '));
        public int GetHashCode(object value) => StringComparer.Ordinal.GetHashCode(((string)value).TrimEnd(' '));
    }
}
