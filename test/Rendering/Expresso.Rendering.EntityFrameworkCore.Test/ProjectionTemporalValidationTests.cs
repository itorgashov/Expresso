using System.Collections;
using System.Linq.Expressions;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed partial class ProjectionTemporalTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CapturedTemporalKeySource_IsRejectedBeforeRootRead(bool adapter)
    {
        // Scenario: only explicit keySource contains history; validation must still precede the current root read.
        var noIo = new NoIo();
        using var db = new TemporalContext(noIo);
        var source = db.Set<ProjectionParent>().Where(p => p.Tenant == 1);
        var keys = Capture(db, Temporal(db, "AsOf"), "field");
        var executor = new ProjectionSqlExecutor(_ => throw new Xunit.Sdk.XunitException("Unexpected root read."));
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        await Assert.ThrowsAsync<NotSupportedException>(() => adapter
            ? source.ToProjectedListAsync(transformer, ProjectionFixture.Nested(), ProjectionFixture.Mapping(), keySource: keys)
            : transformer.RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping()).ExecuteAsync(source, executor, keySource: keys));
        Assert.Empty(executor.RowCounts);
        Assert.Equal(0, noIo.OpenAttempts);
    }

    [Theory]
    [MemberData(nameof(OpaqueScopes))]
    public async Task OpaqueQuerySources_AreRejectedWithoutInvokingApplicationCode(string kind, bool adapter, bool explicitKeySource)
    {
        // Scenario: a query-producing getter/factory/conversion could hide history; reject without evaluating it or opening a connection.
        var noIo = new NoIo();
        using var db = new TemporalContext(noIo);
        var holder = new OpaqueHolder(Temporal(db, "AsOf"));
        Func<IQueryable<ProjectionParent>> factory = holder.Create;
        var queries = new[] { Temporal(db, "AsOf") };
        var list = queries.ToList();
        var anchor = db.Set<ProjectionParent>();
        var source = kind switch
        {
            "property" => anchor.SelectMany(_ => holder.Query),
            "boxed" => anchor.SelectMany(_ => (IQueryable<ProjectionParent>)holder.BoxedQuery),
            "method" => anchor.SelectMany(_ => holder.Create()),
            "delegate" => anchor.SelectMany(_ => factory()),
            "array" => anchor.SelectMany(_ => queries[0]),
            "indexer" => anchor.SelectMany(_ => list[0]),
            "constructor" => anchor.SelectMany(_ => new QueryValue(holder)),
            _ => anchor.SelectMany(_ => (QueryValue)holder)
        };
        var executor = new ProjectionSqlExecutor(_ => throw new Xunit.Sdk.XunitException("Unexpected read."));
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var error = await Assert.ThrowsAsync<NotSupportedException>(() => adapter
            ? source.ToProjectedListAsync(transformer, ProjectionFixture.Nested(), ProjectionFixture.Mapping(), keySource: explicitKeySource ? source : null)
            : transformer.RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping())
                .ExecuteAsync(source, executor, keySource: explicitKeySource ? source : null));
        Assert.Contains("Capture the IQueryable", error.Message);
        Assert.Equal(0, holder.Calls);
        Assert.Empty(executor.RowCounts);
        Assert.Equal(0, noIo.OpenAttempts);
    }

    [Fact]
    public void ConstantQueries_UseReferenceIdentityAndHandleSelfReferences()
    {
        // Scenario: queries override equality/hash or expose themselves as constants; inspect each by reference without enumeration/loops.
        using var db = new TemporalContext();
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var current = new QueryValue(db.Set<ProjectionParent>());
        var historical = new QueryValue(Temporal(db, "AsOf"));
        IQueryable<ProjectionParent> Concat(QueryValue right) => ((IQueryable<ProjectionParent>)db.Set<ProjectionParent>()).Provider.CreateQuery<ProjectionParent>(
            Expression.Call(typeof(Queryable), nameof(Queryable.Concat), new[] { typeof(ProjectionParent) },
                Expression.Constant(current, typeof(IQueryable<ProjectionParent>)), Expression.Constant(right, typeof(IQueryable<ProjectionParent>))));
        transformer.ValidateSource(Concat(current));
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(Concat(historical)));
        current.Expression = Expression.Constant(current, typeof(IQueryable<ProjectionParent>));
        transformer.ValidateSource(current);
        transformer.ValidateSource(ProjectionFixture.Data().AsQueryable());
        Assert.Throws<ArgumentNullException>(() => transformer.ValidateSource(null!));
    }

    [Fact]
    public void StoredFieldChainsAndBuiltInConversions_AreInspected()
    {
        // Scenario: converted field owners still hold an inspectable query; null/static/opaque owners do not invoke application code.
        using var db = new TemporalContext();
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var holder = new CaptureHolder { Query = Temporal(db, "AsOf") };
        foreach (var conversion in new[] { ExpressionType.Convert, ExpressionType.ConvertChecked, ExpressionType.TypeAs })
        {
            var owner = Expression.MakeUnary(conversion, Expression.Constant(holder, typeof(object)), typeof(CaptureHolder));
            var stored = Expression.Field(owner, nameof(CaptureHolder.Query));
            Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(new QueryValue(db.Set<ProjectionParent>()) { Expression = stored }));
        }
        holder.Query = null!;
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(Capture(db, holder.Query!, "field")));
        var emptyOwner = Expression.Field(Expression.Constant(null, typeof(CaptureHolder)), nameof(CaptureHolder.Query));
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(new QueryValue(db.Set<ProjectionParent>()) { Expression = emptyOwner }));
        var staticQuery = Expression.Field(null, typeof(OpaqueHolder).GetField(nameof(OpaqueHolder.StaticQuery))!);
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(new QueryValue(db.Set<ProjectionParent>()) { Expression = staticQuery }));
        var opaqueOwner = new OpaqueHolder(Temporal(db, "AsOf"));
        var source = db.Set<ProjectionParent>().SelectMany(_ => opaqueOwner.Owner.Query);
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(source));
        Assert.Equal(0, opaqueOwner.Calls);
        object? missing = null;
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(db.Set<ProjectionParent>().SelectMany(_ => (IQueryable<ProjectionParent>)missing!)));
    }

    [Fact]
    public void StandardQueryOperators_RemainSupportedWithCapturedCurrentQueries()
    {
        // Scenario: EF tracking/splitting/tagging and ordinary Queryable operators do not require invoking a query factory.
        using var db = new TemporalContext();
        var source = Capture(db, db.Set<ProjectionParent>().Where(p => p.Tenant == 1), "field")
            .AsNoTracking().AsSplitQuery().TagWith("current capture").Where(p => p.Id > 0);
        new EfCoreExpressionToLinqTransformer(db.Database.ProviderName).ValidateSource(source);
        Assert.DoesNotContain("FOR SYSTEM_TIME", source.ToQueryString());
    }

    [Fact]
    public void ConditionalAndCoalescedQueries_InspectTheirStoredBranches()
    {
        // Scenario: standard expression composition is inspectable without evaluating its condition; any history branch is rejected.
        using var db = new TemporalContext();
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var current = db.Set<ProjectionParent>().AsQueryable();
        var historical = Temporal(db, "AsOf");
        var choose = true;
        var conditional = current.SelectMany(_ => choose ? historical : current);
        Assert.Contains("FOR SYSTEM_TIME", conditional.ToQueryString());
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(conditional));
        transformer.ValidateSource(current.SelectMany(_ => choose ? current : current));
        transformer.ValidateSource(current.SelectMany(_ => (current ?? current)!));
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(current.SelectMany(_ => historical ?? current)));
        var missing = Expression.Constant(null, typeof(IQueryable<ProjectionParent>));
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(new QueryValue(current) { Expression = missing }));
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(new QueryValue(current)
            { Expression = Expression.Default(typeof(IQueryable<ProjectionParent>)) }));
    }

    public static IEnumerable<object[]> OpaqueScopes()
    {
        foreach (var kind in new[] { "property", "boxed", "method", "conversion", "delegate", "array", "indexer", "constructor" })
        foreach (var adapter in new[] { false, true })
        foreach (var explicitKeySource in new[] { false, true })
            yield return new object[] { kind, adapter, explicitKeySource };
    }

    private sealed class OpaqueHolder
    {
        private readonly IQueryable<ProjectionParent> _query;
        internal int Calls { get; private set; }
        internal OpaqueHolder(IQueryable<ProjectionParent> query) { _query = query; }
        public IQueryable<ProjectionParent> Query => Create();
        public object BoxedQuery => Create();
        public CaptureHolder Owner { get { Calls++; return new CaptureHolder { Query = _query }; } }
        public IQueryable<ProjectionParent> Create() { Calls++; return _query; }
        public static IQueryable<ProjectionParent>? StaticQuery = null;
        public static explicit operator QueryValue(OpaqueHolder holder) => new(holder.Create());
    }

    private sealed class QueryValue : IOrderedQueryable<ProjectionParent>
    {
        private readonly IQueryable<ProjectionParent> _query;
        internal QueryValue(IQueryable<ProjectionParent> query) { _query = query; Expression = query.Expression; }
        internal QueryValue(OpaqueHolder holder) : this(holder.Create()) { }
        public Type ElementType => typeof(ProjectionParent);
        public Expression Expression { get; set; }
        public IQueryProvider Provider => _query.Provider;
        public IEnumerator<ProjectionParent> GetEnumerator() => throw new Xunit.Sdk.XunitException("Unexpected enumeration.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public override bool Equals(object? obj) => true;
        public override int GetHashCode() => throw new Xunit.Sdk.XunitException("Unexpected application hash code.");
    }
}
