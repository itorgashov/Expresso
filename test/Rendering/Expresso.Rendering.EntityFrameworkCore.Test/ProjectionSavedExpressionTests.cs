using Expresso.Core.Projection;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed partial class ProjectionTemporalTests
{
    [Theory]
    [MemberData(nameof(SavedScopes))]
    public async Task SavedExpressions_AreInspectedBeforeAnyRead(string shape, string keys, bool adapter)
    {
        // Scenario: an indirect lambda hides a historical/opaque producer; all key modes reject without reads, opens or application calls.
        var noIo = new NoIo();
        using var db = new TemporalContext(noIo);
        var parents = db.Set<ProjectionParent>();
        var holder = new ExpressionProducer(Temporal(db, "AsOf"));
        Expression<Func<ProjectionParent, IEnumerable<ProjectionParent>>> rows = _ => holder.Stored;
        Expression<Func<ProjectionParent, IEnumerable<ProjectionParent>>> opaque = _ => holder.Query();
        Expression<Func<ProjectionChild, bool>> predicate = c => holder.Stored.Any(p => p.Id == c.ParentId);
        var expressions = new[] { rows };
        var source = shape switch
        {
            "predicate" => parents.Where(p => p.Children.AsQueryable().Where(predicate).Any()),
            "opaque" => parents.SelectMany(_ => parents.SelectMany(opaque)),
            "property" => parents.SelectMany(_ => parents.SelectMany(holder.Rows)),
            "factory" => parents.SelectMany(_ => parents.SelectMany(holder.Build())),
            "first" => parents.SelectMany(_ => parents.SelectMany(expressions.First())),
            "index" => parents.SelectMany(_ => parents.SelectMany(expressions[0])),
            _ => parents.SelectMany(_ => parents.SelectMany(rows))
        };
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var executor = new ProjectionSqlExecutor(_ => throw new Xunit.Sdk.XunitException("Unexpected read."));
        var root = keys == "empty" ? parents.Where(p => p.Id < 0) : keys == "only" ? parents : source;
        await Assert.ThrowsAsync<NotSupportedException>(() => adapter
            ? root.ToProjectedListAsync(transformer, ProjectionFixture.Nested(), ProjectionFixture.Mapping(), keySource: keys == "inferred" ? null : source)
            : transformer.RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping()).ExecuteAsync(root, executor, keySource: keys == "inferred" ? null : source));
        Assert.Empty(executor.RowCounts);
        Assert.Equal(0, noIo.OpenAttempts);
        Assert.Equal(0, holder.Calls);
    }

    public static IEnumerable<object[]> SavedScopes()
    {
        foreach (var shape in new[] { "rows", "predicate", "opaque", "property", "factory", "first", "index" })
        foreach (var keys in new[] { "inferred", "explicit", "only", "empty" })
        foreach (var adapter in new[] { false, true })
            yield return new object[] { shape, keys, adapter };
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SavedCurrentExpressions_RetainBinding(bool selector, bool adapter)
    {
        // Scenario: safely stored reusable expressions remain supported; inspect original SQL and evaluate only constructed graph data.
        var noIo = new NoIo();
        using var db = new TemporalContext(noIo);
        var parents = db.Set<ProjectionParent>();
        Expression<Func<ProjectionChild, bool>> predicate = c => c.Id > 0;
        Expression<Func<ProjectionParent, IEnumerable<ProjectionChild>>> children = p => p.Children;
        var source = selector
            ? parents.SelectMany(_ => parents.SelectMany(children, (p, c) => p))
            : parents.Where(p => p.Children.AsQueryable().Where(predicate).Any());
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        Assert.DoesNotContain("FOR SYSTEM_TIME", source.ToQueryString());
        if (adapter)
        {
            await Assert.ThrowsAsync<Xunit.Sdk.XunitException>(() => source.ToProjectedListAsync(transformer,
                ProjectionFixture.Nested(), ProjectionFixture.Mapping(), keySource: parents));
            Assert.Equal(1, noIo.OpenAttempts);
            return;
        }
        var commands = new List<string>();
        var result = await transformer.RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping())
            .ExecuteAsync(source, new StageExecutor(commands), keySource: parents);
        Assert.Equal(selector ? 9 : 2, result.Items.Count);
        Assert.Equal(3, commands.Count);
    }

    private sealed class ExpressionProducer
    {
        internal readonly IQueryable<ProjectionParent> Stored;
        internal int Calls;
        internal ExpressionProducer(IQueryable<ProjectionParent> query) => Stored = query;
        internal IQueryable<ProjectionParent> Query() { Calls++; return Stored; }
        internal Expression<Func<ProjectionParent, IEnumerable<ProjectionParent>>> Rows { get { Calls++; return _ => Stored; } }
        internal Expression<Func<ProjectionParent, IEnumerable<ProjectionParent>>> Build() { Calls++; return _ => Stored; }
    }

    [Fact]
    public async Task SavedIntermediatePredicate_BindsToWrappedRowDefinition()
    {
        // Scenario: saved lambda plus EF wrapper plus member-init projection must compose, not only pass in isolation.
        using var db = new TemporalContext();
        Expression<Func<Intermediate, bool>> predicate = row => row.Kids.Any();
        var source = db.Set<ProjectionParent>().Select(p => new Intermediate { Parent = p, Kids = p.Children })
            .TagWith("saved intermediate").Where(predicate).Select(row => row.Parent);
        var commands = new List<string>();
        var result = await new EfCoreExpressionToLinqTransformer(db.Database.ProviderName)
            .RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping()).ExecuteAsync(source, new StageExecutor(commands));
        Assert.Equal(2, result.Items.Count);
        Assert.Contains("saved intermediate", commands[0]);
        Assert.Equal(3, commands.Count);
    }

    private sealed class StageExecutor : IProjectionQueryExecutor
    {
        private readonly List<string> _commands;
        private readonly ProjectionSqlExecutor _memory = new(_ => { });
        internal StageExecutor(List<string> commands) => _commands = commands;
        public Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            _commands.Add(query.ToQueryString());
            return _memory.ToListAsync(query.Provider.CreateQuery<T>(new StoredArguments().Visit(query.Expression)!), cancellationToken);
        }
    }

    private sealed class StoredArguments : ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression node)
        {
            if (Stored(node, out var value))
            {
                if (value is IQueryable query) return Visit(query.Expression);
                if (value is LambdaExpression lambda) return Expression.Quote((LambdaExpression)Visit(lambda));
            }
            return base.VisitMember(node);
        }
        protected override Expression VisitMethodCall(MethodCallExpression node) => node.Method.Name is
            "AsTracking" or "AsNoTracking" or "AsNoTrackingWithIdentityResolution" or "TagWith" or "TagWithCallSite" or
            "IgnoreAutoIncludes" or "IgnoreQueryFilters" or "AsSplitQuery" or "AsSingleQuery" ? Visit(node.Arguments[0]) : base.VisitMethodCall(node);
        private static bool Stored(Expression expression, out object? value)
        {
            if (expression is ConstantExpression constant) { value = constant.Value; return true; }
            if (expression is MemberExpression { Member: FieldInfo field, Expression: { } owner } && Stored(owner, out var target))
            { value = field.GetValue(target); return true; }
            value = null; return false;
        }
    }
}
