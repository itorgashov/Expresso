using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Linq.Expressions;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed partial class ProjectionTemporalTests
{
    [Fact]
    public void MaterializedValuesAndNormalNavigations_AreInspectedWithoutEnumeration()
    {
        // Scenario: literal/key containers and mapped/filter/cast navigations are safe; retain current provider translation.
        using var db = new TemporalContext();
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var ids = new List<int> { 1, 2 };
        var set = new HashSet<int>(ids);
        var array = ids.ToArray();
        var source = db.Set<ProjectionParent>().Where(p => ids.Contains(p.Id) && set.Contains(p.Id) && Enumerable.Contains(array, p.Id))
            .SelectMany(p => ((ProjectionParent)(object)p).Children.Where(c => c.Id > 0));
        transformer.ValidateSource(source);
        Assert.DoesNotContain("FOR SYSTEM_TIME", source.ToQueryString());
        transformer.ValidateSource(db.Set<ProjectionParent>().Where(p => Enumerable.Contains(new[] { 1, 2 }, p.Id)));
        transformer.ValidateSource(ProjectionFixture.Data().AsQueryable().SelectMany(p => p.Children));
        var conditional = true;
        IEnumerable<ProjectionParent> stored = db.Set<ProjectionParent>();
        transformer.ValidateSource(db.Set<ProjectionParent>().SelectMany(_ => conditional ? stored : stored));
    }

    [Fact]
    public void UninspectableEnumerableFields_AreNotEnumerated()
    {
        // Scenario: deferred/non-BCL stored sequences could hide I/O; fail without asking for their enumerator.
        using var db = new TemporalContext();
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        IEnumerable<ProjectionParent> deferred = new DeferredSequence();
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(db.Set<ProjectionParent>().SelectMany(_ => deferred)));
        IEnumerable<ProjectionParent>? missing = null;
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(db.Set<ProjectionParent>().SelectMany(_ => missing!)));
        var parameter = Expression.Parameter(typeof(IQueryable<ProjectionParent>), "opaque");
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(new QueryValue(db.Set<ProjectionParent>()) { Expression = parameter }));
    }

    private sealed class DeferredSequence : IEnumerable<ProjectionParent>
    {
        public IEnumerator<ProjectionParent> GetEnumerator() => throw new Xunit.Sdk.XunitException("Validation must not enumerate.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void MappedPrimitiveCollections_RemainSupported_ButUnmappedGettersReject()
    {
        // Scenario: collection-valued mapped properties are not query factories; an unmapped parameter getter remains opaque.
        using var db = new PrimitiveContext();
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var source = db.Set<PrimitiveParent>().Where(p => p.Values.Contains(1));
        transformer.ValidateSource(source);
        Assert.Contains("OPENJSON", source.ToQueryString());
        Assert.Throws<NotSupportedException>(() => transformer.ValidateSource(db.Set<PrimitiveParent>().Where(p => p.HiddenValues.Any())));
    }

    private sealed class PrimitiveContext : DbContext
    {
        internal PrimitiveContext() : base(new DbContextOptionsBuilder<PrimitiveContext>().UseSqlServer("Server=unused;Database=unused")
            .AddInterceptors(new NoIo()).Options) { }
        protected override void OnModelCreating(ModelBuilder model) => model.Entity<PrimitiveParent>().Ignore(p => p.HiddenValues);
    }

    private sealed class PrimitiveParent
    {
        public int Id { get; set; }
        public List<int> Values { get; set; } = new();
        public IEnumerable<int> HiddenValues => throw new Xunit.Sdk.XunitException("Validation must not invoke a getter.");
    }
}
