using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Paging;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;
using Expresso.Sample.WebApi.EfCore.DataAccess;
using Expresso.Sample.WebApi.EfCore.Entities;

namespace Expresso.Sample.Test;

public sealed class Db2ProjectionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrderedKeyFallback_UsesBoundedBatches_NoRankCase_AndKeepsKnownKeyOrder(bool paged)
    {
        // Scenario: more roots than the parameter budget, tied names, and a requested child; materialization is the only mocked boundary.
        var data = Enumerable.Range(1, 7).Select(id => new Author { Id = id, LastName = "same", DisplayName = "name" + id,
            Awards = new List<Award> { new Award { Id = id, Title = "award" + id, Year = 2024 } } }).ToArray().AsQueryable();
        var query = data.Where(a => a.Id > 1);
        var sort = new SortDirective(new[] { new SortDirectiveItem { Expression = new Field("id", typeof(int)), Direction = SortDirection.Descending } });
        var executor = new CaptureExecutor();
        var rows = await EfCoreListSort.ReadAsync(query, data, sort, BookLinqMappings.Authors,
            new QueryableExpressionToLinqTransformer(), EfCoreProvider.Db2, paged ? new PagingDirective(skip: 1, take: 3) : PagingDirective.None,
            ProjectionDirective.All, SampleProjectionMappings.Authors, default, executor, keyBudget: 2);
        Assert.Equal(paged ? new[] { 6, 5, 4 } : new[] { 7, 6, 5, 4, 3, 2 }, rows.Select(r => (int)r["id"]!));
        Assert.All(rows, row => Assert.Single((IEnumerable<IReadOnlyDictionary<string, object?>>)row["awards"]!));
        Assert.All(executor.Expressions.Skip(1), expression => Assert.DoesNotContain("OrderBy(entity => IIF", expression));
        Assert.All(executor.Expressions.Skip(1), expression => Assert.Contains("Contains", expression));
        Assert.Equal(paged ? 5 : 7, executor.Expressions.Count); // One ordered-key read, then root + child for each bounded batch.
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => EfCoreListSort.ReadAsync(query, data, sort, BookLinqMappings.Authors,
            new QueryableExpressionToLinqTransformer(), EfCoreProvider.Db2, PagingDirective.None, ProjectionDirective.All,
            SampleProjectionMappings.Authors, default, executor, keyBudget: 0));
    }

    [Fact]
    public async Task NormalProvider_OrdersPagesAndOmitsUnrequestedCollections()
    {
        var data = Enumerable.Range(1, 3).Select(id => new Author { Id = id, LastName = "name" + id }).AsQueryable();
        var executor = new CaptureExecutor();
        var rows = await EfCoreListSort.ReadAsync(data.Where(a => a.Id > 1), data, null, BookLinqMappings.Authors,
            new QueryableExpressionToLinqTransformer(), EfCoreProvider.SqlServer, new PagingDirective(take: 1),
            new ProjectionDirective(new[] { new ProjectionSelection("lastname") }), SampleProjectionMappings.Authors, default, executor);
        Assert.Equal("name2", Assert.Single(rows)["lastName"]);
        Assert.Single(rows[0]);
        Assert.Single(executor.Expressions);
    }

    private sealed class CaptureExecutor : IProjectionQueryExecutor
    {
        internal List<string> Expressions { get; } = new();
        public Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Expressions.Add(query.Expression.ToString());
            return Task.FromResult<IReadOnlyList<T>>(query.ToList());
        }
    }
}
