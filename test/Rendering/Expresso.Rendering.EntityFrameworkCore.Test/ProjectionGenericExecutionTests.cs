using Expresso.Core.Projection;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Rendering.EntityFrameworkCore.Test;

public sealed class ProjectionGenericExecutionTests
{
    [Theory]
    [InlineData("narrow", false, false)]
    [InlineData("narrow", false, true)]
    [InlineData("narrow", true, false)]
    [InlineData("narrow", true, true)]
    [InlineData("all", false, false)]
    [InlineData("all", false, true)]
    [InlineData("all", true, false)]
    [InlineData("all", true, true)]
    [InlineData("scalar", false, false)]
    [InlineData("scalar", false, true)]
    [InlineData("scalar", true, false)]
    [InlineData("scalar", true, true)]
    public async Task GenericPlan_LoadsFirstLevel_WithoutEntityResolver(string selection, bool decliningResolver, bool explicitSource)
    {
        // Scenario: DI's generic transformer with a custom executor; preserve authorized composite-key paging without requiring GroupBy translation.
        using var db = new ProjectionContext();
        using var services = new ServiceCollection().AddLinqExpressionTransformations().BuildServiceProvider();
        var transformer = services.GetRequiredService<IProjectionToLinqTransformer>();
        var directive = selection == "all" ? ProjectionDirective.All : new ProjectionDirective(selection == "scalar"
            ? new[] { new ProjectionSelection("title") } : new[] { new ProjectionSelection("title"),
                new ProjectionSelection("children", new[] { new ProjectionSelection("name") }) });
        var commands = new List<string>();
        var recorder = new ProjectionSqlExecutor(q => commands.Add(q.ToQueryString()));
        var declining = new DecliningExecutor(recorder);
        IProjectionQueryExecutor executor = decliningResolver ? declining : recorder;
        var authorized = db.Parents.Where(x => x.Tenant == 1);
        var result = await transformer.RenderProjection(directive, ProjectionFixture.FirstLevelMapping())
            .ExecuteAsync(authorized.OrderBy(x => x.Id).Take(2), executor,
                keySource: explicitSource ? authorized : null, options: new ProjectionExecutionOptions { MaxKeyParameters = 2 });
        Assert.Equal(new[] { "one", "empty" }, result.Items.Select(x => x.Title));
        Assert.Equal(selection == "scalar" ? new[] { 2 } : new[] { 2, 2, 0 }, recorder.RowCounts);
        if (selection != "scalar") Assert.Equal(new[] { "alpha", "beta" }, result.Items[0].Children.Select(x => x.Name).OrderBy(x => x));
        Assert.Empty(result.Items[1].Children);
        Assert.Equal(decliningResolver && selection != "scalar" ? 1 : 0, declining.Resolutions);
        Assert.All(commands, command => { Assert.DoesNotContain("GROUP BY", command); Assert.DoesNotContain("DISTINCT", command); Assert.DoesNotContain("Payload", command); });
    }

    private sealed class DecliningExecutor : IProjectionQueryExecutor, IProjectionEntitySourceProvider
    {
        private readonly IProjectionQueryExecutor _inner;
        internal int Resolutions { get; private set; }
        internal DecliningExecutor(IProjectionQueryExecutor inner) => _inner = inner;
        public IQueryable? GetEntitySource(IQueryable source, Type entityType) { Resolutions++; return null; }
        public Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) => _inner.ToListAsync(query, cancellationToken);
    }
}
