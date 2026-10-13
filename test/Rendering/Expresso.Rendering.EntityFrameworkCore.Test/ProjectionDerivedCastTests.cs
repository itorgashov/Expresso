using Expresso.Core.Projection;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed partial class ProjectionTemporalTests
{
    [Theory]
    [MemberData(nameof(DerivedCastScopes))]
    public async Task DerivedCasts_ResolveEffectiveNavigationMetadata(string shape, bool tpt, bool mapped, bool adapter, bool explicitKeys)
    {
        // Scenario: a base-typed authorized subtype scope accesses derived collections/references; preserve genuine SQL and thin DTO results.
        var noIo = new NoIo();
        using var db = tpt ? new CastTptContext(noIo) : new CastContext(noIo);
        var nodes = db.Set<CastNode>();
        var navigation = CastNavigation(shape);
        var source = mapped ? nodes.OfType<CastSpecialNode>().Cast<CastNode>() : nodes.Where(CastPredicate(navigation));
        Assert.Contains(mapped ? "CastNodes" : "EXISTS", source.ToQueryString());
        var mapping = DerivedCastProjectionFixture.Mapping(mapped ? navigation : null);
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        if (adapter)
        {
            await Assert.ThrowsAsync<Xunit.Sdk.XunitException>(() => source.ToProjectedListAsync(transformer, ProjectionDirective.All,
                mapping, keySource: explicitKeys ? nodes : null));
            Assert.Equal(1, noIo.OpenAttempts);
            return;
        }
        var commands = new List<string>();
        var memory = new ProjectionSqlExecutor<CastNode>(DerivedCastProjectionFixture.Data().AsQueryable(), _ => { });
        var executor = new CastExecutor(memory, commands);
        var result = await transformer.RenderProjection(ProjectionDirective.All, mapping).ExecuteAsync(source, executor, keySource: explicitKeys ? nodes : null);
        Assert.Equal("special", Assert.Single(result.Items).Name);
        Assert.Equal("item", Assert.Single(result.Items[0].Items).Name);
        Assert.Equal(2, commands.Count);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Equal(0, noIo.OpenAttempts);
    }

    public static IEnumerable<object[]> DerivedCastScopes()
    {
        foreach (var shape in new[] { "cast", "as", "property", "reference", "ef-reference", "boxed", "inherited" })
        foreach (var tpt in new[] { false, true })
        foreach (var mapped in new[] { false, true })
        foreach (var adapter in new[] { false, true })
        foreach (var keys in new[] { false, true })
            yield return new object[] { shape, tpt, mapped, adapter, keys };
    }

    private static Expression<Func<CastNode, IEnumerable<CastItem>>> CastNavigation(string shape) => shape switch
    {
        "as" => n => (n as CastSpecialNode)!.Special,
        "property" => n => EF.Property<List<CastItem>>((CastSpecialNode)n, "Special"),
        "reference" => n => ((CastSpecialNode)n).Target!.Common,
        "ef-reference" => n => EF.Property<CastNode>((CastSpecialNode)n, "Target").Common,
        "boxed" => n => EF.Property<List<CastItem>>((object)(CastSpecialNode)n, "Special"),
        "inherited" => n => ((CastSpecialNode)n).Common,
        _ => n => ((CastSpecialNode)n).Special
    };

    private static Expression<Func<CastNode, bool>> CastPredicate(Expression<Func<CastNode, IEnumerable<CastItem>>> navigation) =>
        Expression.Lambda<Func<CastNode, bool>>(Expression.AndAlso(Expression.TypeIs(navigation.Parameters[0], typeof(CastSpecialNode)),
            Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), new[] { typeof(CastItem) }, navigation.Body)), navigation.Parameters);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnselectedDerivedMapping_RemainsUnused(bool tpt)
    {
        using var db = tpt ? new CastTptContext(new NoIo()) : new CastContext(new NoIo());
        var commands = new List<string>();
        var memory = new ProjectionSqlExecutor<CastNode>(DerivedCastProjectionFixture.Data().AsQueryable(), _ => { });
        var result = await new EfCoreExpressionToLinqTransformer(db.Database.ProviderName)
            .RenderProjection(new ProjectionDirective(new[] { new ProjectionSelection("name") }), DerivedCastProjectionFixture.Mapping(CastNavigation("cast")))
            .ExecuteAsync(db.Set<CastNode>(), new CastExecutor(memory, commands));
        Assert.Equal(2, result.Items.Count);
        Assert.Single(commands);
        Assert.All(result.Items, n => Assert.Empty(n.Items));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CastMetadata_DoesNotTrustOpaqueOrUnmappedOwners(bool tpt)
    {
        using var db = tpt ? new CastTptContext(new NoIo()) : new CastContext(new NoIo());
        var nodes = db.Set<CastNode>();
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(nodes.Where(n => EF.Property<List<CastItem>>((CastSpecialNode)n, "Missing").Any())));
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(nodes.Where(n => ((CastSpecialNode)OpaqueCastOwner()).Special.Any())));
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(nodes.Where(n => ((CastSpecialNode)(CastNode)new CustomCastOwner()).Special.Any())));
        transformer.ValidateSource(nodes.OfType<CastSpecialNode>().Where(n => n.Special.Any()));
        var parameter = Expression.Parameter(typeof(CastNode), "n");
        var navigation = Expression.Property(Expression.ConvertChecked(parameter, typeof(CastSpecialNode)), nameof(CastSpecialNode.Special));
        transformer.ValidateSource(nodes.Where(Expression.Lambda<Func<CastNode, bool>>(
            Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), new[] { typeof(CastItem) }, navigation), parameter)));
    }
    private static CastNode OpaqueCastOwner() => throw new Xunit.Sdk.XunitException("Do not invoke factories.");
    private sealed class CustomCastOwner
    {
        public static explicit operator CastNode(CustomCastOwner _) => throw new Xunit.Sdk.XunitException("Do not invoke conversions.");
    }
    private class CastContext : DbContext
    {
        internal CastContext(NoIo noIo) : base(new DbContextOptionsBuilder<CastContext>().UseSqlServer("Server=unused;Database=unused")
            .AddInterceptors(noIo).Options) { }
        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<CastNode>().ToTable("CastNodes");
            model.Entity<CastNode>().HasMany(n => n.Common).WithOne().HasForeignKey(i => i.CommonId);
            model.Entity<CastSpecialNode>().HasMany(n => n.Special).WithOne().HasForeignKey(i => i.SpecialId);
            model.Entity<CastSpecialNode>().HasOne(n => n.Target).WithMany().HasForeignKey(n => n.TargetId);
        }
    }
    private sealed class CastTptContext : CastContext
    {
        internal CastTptContext(NoIo noIo) : base(noIo) { }
        protected override void OnModelCreating(ModelBuilder model) { base.OnModelCreating(model); model.Entity<CastSpecialNode>().ToTable("CastSpecialNodes"); }
    }
    private sealed class CastExecutor : Expresso.Rendering.Linq.IProjectionQueryExecutor
    {
        private readonly ProjectionSqlExecutor<CastNode> _memory;
        private readonly List<string> _commands;
        internal CastExecutor(ProjectionSqlExecutor<CastNode> memory, List<string> commands) { _memory = memory; _commands = commands; }
        public Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            _commands.Add(query.ToQueryString());
            return _memory.ToListAsync(query.Provider.CreateQuery<T>(new CastProperties().Visit(query.Expression)!), cancellationToken);
        }
    }
    private sealed class CastProperties : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node) => node.Method.DeclaringType == typeof(EF)
            ? Expression.Property(Unbox(Visit(node.Arguments[0])), (string)((ConstantExpression)node.Arguments[1]).Value!) : base.VisitMethodCall(node);
        private static Expression Unbox(Expression expression) => expression is UnaryExpression { NodeType: ExpressionType.Convert, Type: { } type } unary &&
            type == typeof(object) ? unary.Operand : expression.Type == typeof(object) ? Expression.Convert(expression, typeof(CastNode)) : expression;
    }
}
