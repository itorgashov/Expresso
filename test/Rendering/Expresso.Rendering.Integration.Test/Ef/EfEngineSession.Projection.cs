#if NET8_0_OR_GREATER
using Microsoft.EntityFrameworkCore;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.Integration.Test.Ef;

public sealed partial class EfEngineSession : IProjectionEngineSession
{
    private readonly List<string> _projectionCommands = new();
    public IReadOnlyList<string> ProjectionCommands => _projectionCommands;

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryProjectionAsync(ProjectionDirective projection, bool windowed = false)
    {
        _projectionCommands.Clear();
        using var context = new WidgetDbContext(_options);
        var transformer = new EfCoreExpressionToLinqTransformer(context.Database.ProviderName);
        IQueryable<ProjectionParent> roots = context.Set<ProjectionParent>().AsNoTracking();

        if (windowed && _liftSortKeys)
        {
            // DB2 drops SQL offsets: choose known composite keys first and project only that identity.
            var keyQuery = roots.Where(p => p.Title != "empty").OrderByDescending(p => p.Title).ThenBy(p => p.Tenant).ThenBy(p => p.Id)
                .Select(p => new { p.Tenant, p.Id });
            _projectionCommands.Add(keyQuery.ToQueryString());
            var keys = await keyQuery.ToListAsync();
            var selected = keys.Skip(1).Take(1).Single();
            roots = roots.Where(p => p.Tenant == selected.Tenant && p.Id == selected.Id);
        }
        else
        if (windowed) roots = roots.Where(p => p.Title != "empty").OrderByDescending(p => p.Title).ThenBy(p => p.Tenant).ThenBy(p => p.Id).Skip(1).Take(1);
        var sort = new SortDirective(Array.Empty<SortDirectiveItem>(), new[] { new CollectionSort("children",
            new SortDirective(new[] { new SortDirectiveItem { Expression = new Field("name", typeof(string)) } })) });
        var result = await transformer.RenderProjection(projection, ProjectionFixture.Mapping()).ExecuteAsync(roots,
            new Executor(_projectionCommands), transformer, sort, keySource: context.Set<ProjectionParent>().AsNoTracking());
        return ProjectionShaper.ToSparseObjects(result);
    }

    private sealed class Executor : IProjectionQueryExecutor
    {
        private readonly List<string> _commands;
        internal Executor(List<string> commands) => _commands = commands;
        public async Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            _commands.Add(query.ToQueryString());
            return await query.ToListAsync(cancellationToken);
        }
    }
}
#endif
