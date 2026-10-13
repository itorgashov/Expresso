using System.Linq.Expressions;
using System.Reflection;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed class ProjectionEntitySourceTests
{
    [Fact]
    public void Resolver_UsesModelEntityRoot_AndRejectsUnsupportedSources()
    {
        using var db = new ProjectionContext();
        var scope = db.Parents.Where(x => x.Tenant == 1).SelectMany(x => x.Children);
        var source = EfCoreProjectionEntitySource.Resolve(scope, typeof(ProjectionChild));
        var typedSource = Assert.IsAssignableFrom<IQueryable<ProjectionChild>>(source);
        Assert.Contains("widget_projection_child", typedSource.ToQueryString());
        Assert.DoesNotContain("widget_projection_parent", typedSource.ToQueryString());
        Assert.Null(EfCoreProjectionEntitySource.Resolve(scope, typeof(ProjectionParentDto)));
        Assert.Null(EfCoreProjectionEntitySource.Resolve(ProjectionFixture.Data().AsQueryable(), typeof(ProjectionParent)));
        Assert.Null(EfCoreProjectionEntitySource.Resolve(new ModelLessQuery(), typeof(ProjectionParent)));
    }

    [Fact]
    public void AdapterExecutor_ResolvesRoots_EvenWithoutProjectionTransformerCapability()
    {
        using var db = new ProjectionContext();
        var executor = (IProjectionEntitySourceProvider)Activator.CreateInstance(typeof(EfCoreProjectionExtensions)
            .GetNestedType("Executor", BindingFlags.NonPublic)!, nonPublic: true)!;
        Assert.IsAssignableFrom<IQueryable<ProjectionChild>>(executor.GetEntitySource(db.Parents, typeof(ProjectionChild)));
    }

    [Fact]
    public async Task OwnedItems_RemainUnique_AfterRepeatedOwnerScope_WithoutEntityDistinct()
    {
        // Scenario: duplicate owner paths feed two owned collection levels; normalize owners before expansion, retaining XML-free selectors.
        using var db = new OwnedProjectionContext();
        var childScope = db.Parents.SelectMany(x => x.Children);
        Assert.Same(childScope, EfCoreProjectionEntitySource.Resolve(childScope, typeof(ProjectionChild)));
        var sql = new List<string>();
        var executor = new ProjectionSqlExecutor(q => sql.Add(q.ToQueryString()));
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var result = await transformer.RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping())
            .ExecuteAsync(db.Parents.Concat(db.Parents), executor);
        Assert.Equal(6, result.Items.Count);
        Assert.Equal(new[] { 6, 3, 2 }, executor.RowCounts);
        Assert.All(sql, command => { Assert.DoesNotContain("DISTINCT", command); Assert.DoesNotContain("Payload", command); });
    }

    private sealed class OwnedProjectionContext : DbContext
    {
        internal OwnedProjectionContext() : base(new DbContextOptionsBuilder<OwnedProjectionContext>()
            .UseSqlServer("Server=unused;Database=unused").Options) { }
        internal DbSet<ProjectionParent> Parents => Set<ProjectionParent>();
        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<ProjectionParent>().HasKey(x => new { x.Tenant, x.Id });
            model.Entity<ProjectionParent>().OwnsMany(x => x.Children, child =>
            {
                child.WithOwner().HasForeignKey(x => new { x.Tenant, x.ParentId });
                child.HasKey(x => new { x.Tenant, x.Id });
                child.Property(x => x.Payload).HasColumnType("xml");
                child.OwnsMany(x => x.Leaves, leaf =>
                {
                    leaf.WithOwner().HasForeignKey(x => new { x.Tenant, x.ChildId });
                    leaf.HasKey(x => new { x.Tenant, x.Id });
                });
            });
        }
    }
    private sealed class ModelLessQuery : IQueryable<ProjectionParent>, IAsyncQueryProvider
    {
        public Type ElementType => typeof(ProjectionParent);
        public Expression Expression => Expression.Constant(this);
        public IQueryProvider Provider => this;
        public IEnumerator<ProjectionParent> GetEnumerator() => throw new NotSupportedException();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        public IQueryable CreateQuery(Expression expression) => throw new NotSupportedException();
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => throw new NotSupportedException();
        public object? Execute(Expression expression) => throw new NotSupportedException();
        public TResult Execute<TResult>(Expression expression) => throw new NotSupportedException();
        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
