using Expresso.Core.Projection;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.Linq.Test;

public sealed class ProjectionRecheckTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrappedWindow_RequiresExplicitKeySource_BeforeReading(bool explicitKeySource)
    {
        // Scenario: an identity Select hides the page; reject implicit lookup before I/O, but allow an explicit authorized source.
        var source = ProjectionFixture.Data().AsQueryable();
        var executor = new MemoryProjectionExecutor();
        var plan = new QueryableExpressionToLinqTransformer().RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping());
        var query = source.OrderBy(p => p.Title).Take(1).Select(p => p);
        if (explicitKeySource)
        {
            var result = await plan.ExecuteAsync(query, executor, keySource: source);
            Assert.Single(result.Items);
        }
        else
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => plan.ExecuteAsync(query, executor));
            Assert.Contains("keySource", error.Message);
            Assert.Empty(executor.Expressions);
        }
    }

    [Fact]
    public async Task ScalarWrappedWindow_DoesNotRequireAChildSource()
    {
        var source = ProjectionFixture.Data().AsQueryable().Take(1).Select(p => p);
        var executor = new MemoryProjectionExecutor();
        var plan = new QueryableExpressionToLinqTransformer().RenderProjection(
            new ProjectionDirective(new[] { new ProjectionSelection("title") }), ProjectionFixture.Mapping());
        Assert.Single((await plan.ExecuteAsync(source, executor)).Items);
        Assert.Single(executor.Expressions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnknownUnpagedOperator_IsAllowed_AndNestedJoinWindowIsNotStripped(bool nestedWindow)
    {
        // Scenario: only outer windows are unsafe. An identity projection without a page and a windowed inner join retain their rows.
        var source = ProjectionFixture.Data().AsQueryable();
        var query = nestedWindow
            ? source.Join(source.Where(p => p.Tenant == 1).Take(1), p => p.Tenant, p => p.Tenant, (p, other) => p)
            : source.Select(p => p);
        var executor = new MemoryProjectionExecutor();
        var result = await new QueryableExpressionToLinqTransformer().RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping())
            .ExecuteAsync(query, executor);
        Assert.Equal(nestedWindow ? 2 : 3, result.Items.Count);
        Assert.Equal("first", result.Items[0].Children.Single(c => c.Name == "beta").Leaves.Single().Value);
    }

    [Fact]
    public async Task ConvertedWrappedWindow_IsRejectedBeforeIo()
    {
        var source = ProjectionFixture.Data().AsQueryable();
        var expression = System.Linq.Expressions.Expression.Convert(source.Take(1).Select(p => p).Expression,
            typeof(IQueryable<ProjectionParent>));
        var query = source.Provider.CreateQuery<ProjectionParent>(expression);
        var executor = new MemoryProjectionExecutor();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new QueryableExpressionToLinqTransformer()
            .RenderProjection(ProjectionFixture.Nested(), ProjectionFixture.Mapping()).ExecuteAsync(query, executor));
        Assert.Empty(executor.Expressions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharedAuthors_ReadEachAwardOnce_BeforeAttachment(bool incompleteKeys)
    {
        // Scenario: one author appears in ten books; two different awards have null or complete keys. Read both only once.
        var source = SharedProjectionFixture.Data(incompleteKeys).AsQueryable();
        var executor = new ProjectionSqlExecutor<SharedBook>(source, _ => { });
        var plan = new QueryableExpressionToLinqTransformer().RenderProjection(SharedProjectionFixture.Nested(), SharedProjectionFixture.Mapping());
        var result = await plan.ExecuteAsync(source.Where(b => b.Tenant == 1).OrderBy(b => b.Id).Take(2), executor, keySource: source);
        Assert.Equal(new[] { 2, 2, 2 }, executor.RowCounts);
        Assert.All(result.Items, book => Assert.Equal(2, Assert.Single(book.Authors).Awards.Count));
    }
}
