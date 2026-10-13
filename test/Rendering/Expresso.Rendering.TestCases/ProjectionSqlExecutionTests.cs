using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;

namespace Expresso.Rendering.TestCases;

public abstract partial class ProjectionSqlTests
{
    [Fact]
    public async Task Executor_BatchesCompleteKeys_PreservesIncompleteChildIdentities_AndSparseNulls()
    {
        // Scenario: repeated tenant-local ids, duplicate joins, and two null child identities; only complete duplicates collapse.
        var map = Mapping();
        var plan = _renderer.RenderProjection(ProjectionDirective.All, map);
        var io = new SqlIo((index, p) => index == 0
            ? new[] { Values(p, ("p.title", null), ("p.tenant", 1), ("p.id", 1)),
                Values(p, ("p.title", "two"), ("p.tenant", 2), ("p.id", 1)),
                Values(p, ("p.title", "missing"), ("p.tenant", null), ("p.id", 1)) }
            : index == 1
                ? new[] { Values(p, ("c.name", "first"), ("c.tenant", 1), ("c.id", 1), ("c.parent_id", 1)),
                    Values(p, ("c.name", "duplicate"), ("c.tenant", 1), ("c.id", 1), ("c.parent_id", 1)),
                    Values(p, ("c.name", "null-one"), ("c.tenant", 1), ("c.id", null), ("c.parent_id", 1)),
                    Values(p, ("c.name", "null-two"), ("c.tenant", 1), ("c.id", null), ("c.parent_id", 1)) }
                : new[] { Values(p, ("c.name", "second"), ("c.tenant", 2), ("c.id", 1), ("c.parent_id", 1)),
                    Values(p, ("c.name", "orphan"), ("c.tenant", 2), ("c.id", 2), ("c.parent_id", 99)) });
        var rows = await new SqlProjectionExecutor(_renderer, _renderer).ExecuteAsync(plan, "root", new Dictionary<string, object>(), io, maxKeyParameters: 2);
        Assert.Equal(3, io.Sql.Count);
        Assert.Null(rows[0]["title"]);
        Assert.Equal(3, ((IEnumerable<IReadOnlyDictionary<string, object?>>)rows[0]["children"]!).Count());
        Assert.Single((IEnumerable<IReadOnlyDictionary<string, object?>>)rows[1]["children"]!);
        Assert.Empty((IEnumerable<IReadOnlyDictionary<string, object?>>)rows[2]["children"]!);
        Assert.All(io.Parameters.Skip(1), p => Assert.Equal(2, p.Count));
        Assert.Contains(Q("c.parent_id"), io.Sql[1]);
    }

    [Fact]
    public async Task Executor_DeepAttachment_DefaultAndRequestedSort_UseOnlySelectedLevels()
    {
        // Scenario: one complete composite relationship at each depth; nested sorting differs from the mapping default.
        var leaf = new SqlProjectionMapping().Key("tenant", "l.tenant", typeof(int)).Key("id", "l.id", typeof(int))
            .Field("value", "value", "l.value", typeof(string));
        var child = new SqlProjectionMapping().Key("tenant", "c.tenant", typeof(int)).Key("id", "c.id", typeof(int))
            .Field("name", "name", "c.name", typeof(string))
            .Collection("leaves", "leaves", new SqlCollectionProjectionMapping("leaf l", leaf,
                new[] { new SqlProjectionKey("tenant", "l.tenant", typeof(int)), new SqlProjectionKey("id", "l.child_id", typeof(int)) }, "l.id"));
        var parent = new SqlProjectionMapping().Key("tenant", "p.tenant", typeof(int)).Key("id", "p.id", typeof(int))
            .Collection("children", "children", new SqlCollectionProjectionMapping("child c", child,
                new[] { new SqlProjectionKey("tenant", "c.tenant", typeof(int)), new SqlProjectionKey("id", "c.parent_id", typeof(int)) }, "c.id",
                new SqlQueryMapping(new Dictionary<string, string> { ["name"] = "c.name" })));
        var plan = _renderer.RenderProjection(new ProjectionDirective(new[] { new ProjectionSelection("children") }), parent);
        var sort = new SortDirective(Array.Empty<SortDirectiveItem>(), new[] { new CollectionSort("children",
            new SortDirective(new[] { new SortDirectiveItem { Expression = new Field("name", typeof(string)), Direction = SortDirection.Descending } })) });
        var io = new SqlIo((i, p) => new[] { i == 0 ? Values(p, ("p.tenant", 1), ("p.id", 2)) : i == 1
            ? Values(p, ("c.name", "child"), ("c.tenant", 1), ("c.id", 3), ("c.parent_id", 2))
            : Values(p, ("l.value", "leaf"), ("l.tenant", 1), ("l.id", 4), ("l.child_id", 3)) });
        var rows = await new SqlProjectionExecutor(_renderer, _renderer).ExecuteAsync(plan, "root", new Dictionary<string, object>(), io, sort);
        Assert.Single(rows[0]);
        Assert.EndsWith("ORDER BY " + Q("c.name") + " DESC", io.Sql[1]);
        Assert.EndsWith("ORDER BY l.id", io.Sql[2]);
        var item = Assert.Single((IEnumerable<IReadOnlyDictionary<string, object?>>)rows[0]["children"]!);
        Assert.Equal("leaf", Assert.Single((IEnumerable<IReadOnlyDictionary<string, object?>>)item["leaves"]!)["value"]);
    }

    [Fact]
    public async Task Executor_RejectsInvalidInputs_BeforeIo_AndPropagatesCancellation()
    {
        var plan = _renderer.RenderProjection(ProjectionDirective.All, Mapping());
        var io = new SqlIo((_, p) => Array.Empty<object?[]>());
        var executor = new SqlProjectionExecutor(_renderer, _renderer);
        var args = new Dictionary<string, object>();
        Assert.Throws<ArgumentNullException>(() => new SqlProjectionExecutor(null!, _renderer));
        Assert.Throws<ArgumentNullException>(() => new SqlProjectionExecutor(_renderer, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.ExecuteAsync(null!, "root", args, io));
        await Assert.ThrowsAsync<ArgumentException>(() => executor.ExecuteAsync(plan, "", args, io));
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.ExecuteAsync(plan, "root", null!, io));
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.ExecuteAsync(plan, "root", args, null!));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => executor.ExecuteAsync(plan, "root", args, io, maxKeyParameters: 0));
        await Assert.ThrowsAsync<ArgumentException>(() => executor.ExecuteAsync(plan, "root", args, io, maxKeyParameters: 1));
        Assert.Empty(io.Sql);
        Assert.Empty(await executor.ExecuteAsync(plan, "root", args, io));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executor.ExecuteAsync(plan, "root", args, io, cancellationToken: new CancellationToken(true)));
        await Assert.ThrowsAsync<ArgumentException>(() => executor.ExecuteAsync(plan, "root", args,
            new SqlIo((_, p) => new[] { new object?[0] })));
        var rootIo = new SqlIo((_, p) => new[] { Values(p, ("p.title", "one"), ("p.tenant", 1), ("p.id", 1)) });
        var sort = new SortDirective(Array.Empty<SortDirectiveItem>(), new[] { new CollectionSort("children",
            new SortDirective(new[] { new SortDirectiveItem { Expression = new Field("name", typeof(string)) } })) });
        await Assert.ThrowsAsync<ArgumentException>(() => executor.ExecuteAsync(plan, "root", args, rootIo, sort));
    }

    [Fact]
    public void KeyPredicates_ValidateAndKeepTrustedSqlExpressionsRaw()
    {
        var components = new[] { new SqlProjectionKey("id", "p.id", typeof(int), rawExpression: true) };
        var keys = new[] { new CompositeProjectionKey(new object?[] { 1 }) };
        Assert.StartsWith("(p.id = ", _renderer.RenderProjectionKeys(components, keys).whereClause);
        Assert.Throws<ArgumentNullException>(() => _renderer.RenderProjectionKeys(null!, keys));
        Assert.Throws<ArgumentNullException>(() => _renderer.RenderProjectionKeys(components, null!));
        foreach (var prefix in new string?[] { null, "1bad", "bad;" })
            Assert.Throws<ArgumentException>(() => _renderer.RenderProjectionKeys(components, keys, prefix!));
        Assert.Throws<ArgumentException>(() => _renderer.RenderProjectionKeys(Array.Empty<SqlProjectionKey>(), keys));
        Assert.Throws<ArgumentException>(() => _renderer.RenderProjectionKeys(components, Array.Empty<CompositeProjectionKey>()));
        Assert.Throws<ArgumentException>(() => _renderer.RenderProjectionKeys(components, new[] { new CompositeProjectionKey(new object?[] { null }) }));
        Assert.Throws<ArgumentException>(() => _renderer.RenderProjectionKeys(components, new[] { new CompositeProjectionKey(new object?[] { 1, 2 }) }));
        var mapping = new SqlProjectionMapping().Key("id", "p.id", typeof(int), rawExpression: true)
            .Field("clock", "clock", "CURRENT_TIMESTAMP", typeof(DateTime)).Field("raw", "raw", "p.id", typeof(int), rawExpression: true);
        var plan = _renderer.RenderProjection(ProjectionDirective.All, mapping);
        Assert.StartsWith("CURRENT_TIMESTAMP AS ", plan.SelectList);
        Assert.Contains("p.id AS ", plan.SelectList);
        Assert.Equal(2, plan.Columns.Count);
        Assert.Equal(2, mapping.GetProjectionModel().Fields.Length);
        Assert.Single(Mapping().GetProjectionModel().Collections);
    }

    private static object?[] Values(RenderedSqlProjection plan, params (string Column, object? Value)[] values)
    {
        var dictionary = values.ToDictionary(p => p.Column, p => p.Value);
        return plan.Columns.Select(c => dictionary[c.ColumnExpression]).ToArray();
    }

    private sealed class SqlIo : IProjectionSqlQueryExecutor
    {
        private readonly Func<int, RenderedSqlProjection, object?[][]> _read;
        internal SqlIo(Func<int, RenderedSqlProjection, object?[][]> read) => _read = read;
        internal List<string> Sql { get; } = new();
        internal List<IReadOnlyDictionary<string, object>> Parameters { get; } = new();
        public Task<IReadOnlyList<object?[]>> ReadAsync(string sql, IReadOnlyDictionary<string, object> parameters,
            RenderedSqlProjection plan, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Sql.Add(sql);
            Parameters.Add(parameters);
            return Task.FromResult<IReadOnlyList<object?[]>>(_read(Sql.Count - 1, plan));
        }
    }
}
