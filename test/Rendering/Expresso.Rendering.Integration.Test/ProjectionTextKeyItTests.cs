using System.Collections;
using Expresso.Core.Projection;
using Microsoft.Data.Sqlite;

namespace Expresso.Rendering.Integration.Test;

/// <summary>Checks matching and attachment using actual SQLite collations in private widget tables.</summary>
[Trait("Category", "Integration")]
public sealed class ProjectionTextKeyItTests
{
    [SkippableTheory]
    [InlineData("NOCASE", false)]
    [InlineData("NOCASE", true)]
    [InlineData("RTRIM", false)]
    [InlineData("RTRIM", true)]
    [InlineData("BINARY", false)]
    [InlineData("BINARY", true)]
    public async Task Projection_TextKeys_DatabaseMatchingAndAttachmentAgree(string collation, bool composite)
    {
        // Scenario: mixed-case or padded stored foreign keys compare equal in SQLite; preserve them through nested attachment.
        Skip.IfNot(IntegrationEnabled.IsOn, IntegrationEnabled.SkipReason);
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        string Variant(string value) => collation == "NOCASE" ? value.ToLowerInvariant() : collation == "RTRIM" ? value + "  " : value;
        // Collation is restricted to the three literal theory values; all SQL targets this private in-memory database.
        foreach (var sql in new[]
        {
            "PRAGMA foreign_keys=ON",
            "CREATE TABLE widget_projection_text_parent (tenant INT, code TEXT COLLATE " + collation + ", title TEXT, PRIMARY KEY(tenant,code))",
            "CREATE TABLE widget_projection_text_child (tenant INT, code TEXT COLLATE " + collation + ", parent_code TEXT COLLATE " + collation + ", name TEXT, PRIMARY KEY(tenant,code), FOREIGN KEY(tenant,parent_code) REFERENCES widget_projection_text_parent(tenant,code))",
            "CREATE TABLE widget_projection_text_leaf (id INT PRIMARY KEY, tenant INT, child_code TEXT COLLATE " + collation + ", value TEXT, FOREIGN KEY(tenant,child_code) REFERENCES widget_projection_text_child(tenant,code))"
        })
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
        async Task Insert(string sql, params object[] values)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            for (var i = 0; i < values.Length; i++) ParameterBinder.At(command, "@p" + i, values[i]);
            await command.ExecuteNonQueryAsync();
        }
        await Insert("INSERT INTO widget_projection_text_parent VALUES (@p0,@p1,@p2)", 1, "PARENT-A", "root");
        await Insert("INSERT INTO widget_projection_text_child VALUES (@p0,@p1,@p2,@p3)", 1, "CHILD-A", Variant("PARENT-A"), "child");
        await Insert("INSERT INTO widget_projection_text_leaf VALUES (@p0,@p1,@p2,@p3)", 1, 1, Variant("CHILD-A"), "leaf");
        // For BINARY the lower-case parent is a different valid identity, not a row belonging to the selected root.
        var otherTenant = collation == "BINARY" ? 1 : 2;
        var otherParent = collation == "BINARY" ? "parent-a" : composite ? "PARENT-A" : "PARENT-B";
        var otherChild = collation == "BINARY" ? "child-a" : composite ? "CHILD-A" : "CHILD-B";
        await Insert("INSERT INTO widget_projection_text_parent VALUES (@p0,@p1,@p2)", otherTenant, otherParent, "other");
        await Insert("INSERT INTO widget_projection_text_child VALUES (@p0,@p1,@p2,@p3)", otherTenant, otherChild, otherParent, "other");

        var textComparer = collation == "NOCASE" ? (IEqualityComparer)StringComparer.OrdinalIgnoreCase : new PaddingComparer();
        SqlProjectionMapping Keys(string alias)
        {
            var map = new SqlProjectionMapping();
            if (composite) map.Key("tenant", alias + ".tenant", typeof(int));
            map.Key("code", alias + ".code", typeof(string));
            return collation == "BINARY" ? map : map.UseKeyComparer(composite
                ? new CompositeProjectionKeyComparer(null, textComparer) : new CompositeProjectionKeyComparer(textComparer));
        }
        SqlProjectionKey[] Correlation(string alias, string column) => composite
            ? new[] { new SqlProjectionKey("tenant", alias + ".tenant", typeof(int)), new SqlProjectionKey("code", alias + "." + column, typeof(string)) }
            : new[] { new SqlProjectionKey("code", alias + "." + column, typeof(string)) };
        var leaf = new SqlProjectionMapping().Key("id", "l.id", typeof(int)).Field("value", "value", "l.value", typeof(string));
        var child = Keys("c").Field("name", "name", "c.name", typeof(string)).Collection("leaves", "leaves",
            new SqlCollectionProjectionMapping("widget_projection_text_leaf l", leaf, Correlation("l", "child_code")));
        var parent = Keys("p").Field("title", "title", "p.title", typeof(string)).Collection("children", "children",
            new SqlCollectionProjectionMapping("widget_projection_text_child c CROSS JOIN (SELECT 1 UNION ALL SELECT 1) duplicates", child, Correlation("c", "parent_code")));
        var renderer = new ExpressionToSqliteQueryClauseTransformer();
        var plan = renderer.RenderProjection(ProjectionDirective.All, parent);
        var io = new Reader(connection);
        var result = await new SqlProjectionExecutor(renderer, renderer).ExecuteAsync(plan,
            "SELECT " + plan.SelectList + " FROM widget_projection_text_parent p CROSS JOIN (SELECT 1 UNION ALL SELECT 1) duplicates WHERE p.tenant = 1 AND p.code = @code",
            new Dictionary<string, object> { ["@code"] = "PARENT-A" }, io, maxKeyParameters: composite ? 2 : 1);
        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { 2, 2, 1 }, io.Counts);
        Assert.All(result, root =>
        {
            Assert.Equal("root", root["title"]);
            var attached = Assert.Single((IEnumerable<IReadOnlyDictionary<string, object?>>)root["children"]!);
            Assert.Equal("child", attached["name"]);
            Assert.Equal("leaf", Assert.Single((IEnumerable<IReadOnlyDictionary<string, object?>>)attached["leaves"]!)["value"]);
        });
    }

    private sealed class PaddingComparer : IEqualityComparer
    {
        public new bool Equals(object? left, object? right) => StringComparer.Ordinal.Equals((left as string)?.TrimEnd(' '), (right as string)?.TrimEnd(' '));
        public int GetHashCode(object value) => StringComparer.Ordinal.GetHashCode(((string)value).TrimEnd(' '));
    }

    private sealed class Reader : IProjectionSqlQueryExecutor
    {
        private readonly SqliteConnection _connection;
        internal Reader(SqliteConnection connection) => _connection = connection;
        internal List<int> Counts { get; } = new();
        public async Task<IReadOnlyList<object?[]>> ReadAsync(string sql, IReadOnlyDictionary<string, object> parameters,
            RenderedSqlProjection plan, CancellationToken cancellationToken)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            foreach (var parameter in parameters) ParameterBinder.At(command, parameter.Key, parameter.Value);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var rows = new List<object?[]>();
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(plan.Columns.Select(c => reader.IsDBNull(c.Ordinal) ? null : Convert.ChangeType(reader.GetValue(c.Ordinal), c.ValueType)).ToArray());
            Counts.Add(rows.Count);
            return rows;
        }
    }
}
