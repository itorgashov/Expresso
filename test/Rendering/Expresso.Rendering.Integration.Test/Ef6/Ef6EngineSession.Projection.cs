#if NETFRAMEWORK
using System.Data.Entity;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;
using Expresso.Rendering.EntityFramework;
using Expresso.Rendering.Linq;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.Integration.Test.Ef6;

public sealed partial class Ef6EngineSession : IProjectionEngineSession
{
    private readonly List<string> _projectionCommands = new();
    public IReadOnlyList<string> ProjectionCommands => _projectionCommands;

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryProjectionAsync(ProjectionDirective projection, bool windowed = false)
    {
        _projectionCommands.Clear();
        using var context = NewContext();
        var transformer = new Ef6ExpressionToLinqTransformer(context);
        IQueryable<ProjectionParent> roots = context.Set<ProjectionParent>().AsNoTracking();

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
            _commands.Add(query.ToString()!);
            return await query.ToListAsync(cancellationToken);
        }
    }
}
#endif
