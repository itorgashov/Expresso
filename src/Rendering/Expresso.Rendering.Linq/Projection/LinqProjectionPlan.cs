using System.Linq.Expressions;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;

namespace Expresso.Rendering.Linq;

/// <summary>Builds provider-translatable scalar projection selectors and collection execution plans.</summary>
public interface IProjectionToLinqTransformer
{
    /// <summary>Validates and freezes a typed mapping, then builds its requested projection.</summary>
    LinqProjectionPlan<TEntity, TResult> RenderProjection<TEntity, TResult>(
        ProjectionDirective projection, LinqProjectionMapping<TEntity, TResult> mapping) where TResult : new();
}

/// <summary>Typed scalar selector and recursive execution plan for an immutable projection.</summary>
public sealed class LinqProjectionPlan<TEntity, TResult> where TResult : new()
{
    internal LinqProjectionPlan(ProjectionDirective projection, LinqProjectionMapping<TEntity, TResult> mapping,
        IProjectionEntitySourceProvider? entitySources = null)
    {
        Directive = projection ?? throw new ArgumentNullException(nameof(projection));
        Mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
        mapping.Validate(projection);
        Selector = mapping.Selector(projection);
        _shape = mapping.BuildShaper(projection);
        _entitySources = entitySources;
    }
    private readonly IProjectionEntitySourceProvider? _entitySources;
    private readonly Func<object, IReadOnlyDictionary<string, object?>> _shape;
    internal LinqProjectionMapping<TEntity, TResult> Mapping { get; }
    /// <summary>Selected public fields, without internal keys. Can be passed directly to Queryable.Select.</summary>
    public Expression<Func<TEntity, TResult>> Selector { get; }
    /// <summary>Validated projection.</summary>
    public ProjectionDirective Directive { get; }
    /// <summary>Executes root and requested collection queries using a provider-specific async materializer.</summary>
    /// <remarks>Without entity-source resolution the first collection level uses the authorized key source directly.
    /// Deeper levels require provider-translatable structural key grouping or an <see cref="IProjectionEntitySourceProvider"/>.</remarks>
    /// <exception cref="InvalidOperationException">A requested collection's implicit key source still contains an outer Skip/Take
    /// behind an unsupported operator. Supply an explicit authorized, unpaged <paramref name="keySource"/>.</exception>
    public async Task<ProjectedResult<TResult>> ExecuteAsync(IQueryable<TEntity> source, IProjectionQueryExecutor executor,
        IExpressionToLinqTransformer? transformer = null, SortDirective? sort = null,
        ProjectionExecutionOptions? options = null, CancellationToken cancellationToken = default,
        IQueryable<TEntity>? keySource = null)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (executor is null) throw new ArgumentNullException(nameof(executor));
        var settings = options ?? new ProjectionExecutionOptions();
        var needsChildren = Mapping.Collections.Any(c => Directive.IsAll || Directive.Find(c.Name) is not null);
        var childrenSource = needsChildren ? keySource ?? ProjectionKeySource.Unwindow(source) : source;
        if (needsChildren)
        {
            ValidateSources(_entitySources as IProjectionSourceValidator, source, childrenSource);
            ValidateSources(executor as IProjectionSourceValidator, source, childrenSource);
        }
        var entitySources = executor as IProjectionEntitySourceProvider;
        if (_entitySources is not null)
            entitySources = entitySources is null ? _entitySources : new FallbackEntitySources(entitySources, _entitySources);
        var rows = await Mapping.LoadRootAsync(source, Directive, executor, cancellationToken).ConfigureAwait(false);
        await Mapping.LoadChildrenAsync(childrenSource, rows, Directive, executor, transformer, sort,
            Array.Empty<string>(), settings, cancellationToken, entitySources).ConfigureAwait(false);
        return new ProjectedResult<TResult>(rows.Select(r => (TResult)r.ItemObject).ToArray(), rows.Select(r => r.KeyValue).ToArray(),
            item => _shape(item!));
    }

    private void ValidateSources(IProjectionSourceValidator? validator, IQueryable source, IQueryable childrenSource)
    {
        if (validator is null) return;
        validator.ValidateSource(source);
        if (!ReferenceEquals(source, childrenSource)) validator.ValidateSource(childrenSource);
        Mapping.ValidateCollectionSources(childrenSource, Directive, validator);
    }

    private sealed class FallbackEntitySources : IProjectionEntitySourceProvider
    {
        private readonly IProjectionEntitySourceProvider _first, _second;
        internal FallbackEntitySources(IProjectionEntitySourceProvider first, IProjectionEntitySourceProvider second)
        { _first = first; _second = second; }
        public IQueryable? GetEntitySource(IQueryable source, Type entityType) =>
            _first.GetEntitySource(source, entityType) ?? _second.GetEntitySource(source, entityType);
    }
}

/// <summary>Optional provider capability that rejects unsupported collection query scopes before any projection I/O.</summary>
public interface IProjectionSourceValidator
{
    /// <summary>Validates root, unpaged key, and selected navigation scopes at every requested depth before materialization.</summary>
    /// <param name="source">Caller-owned or mapping-composed query scope; implementations must not execute it.</param>
    void ValidateSource(IQueryable source);
}

/// <summary>Async query materialization boundary used by the EF adapters and test doubles.</summary>
public interface IProjectionQueryExecutor
{
    /// <summary>Materializes a provider query without changing its expression.</summary>
    Task<IReadOnlyList<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken);
}

/// <summary>Optional transformer/materializer capability for unique entity query roots used in child projection.</summary>
public interface IProjectionEntitySourceProvider
{
    /// <summary>Returns an unpaged source containing each entity once, or null when this provider cannot resolve it.</summary>
    /// <param name="source">Original authorized navigation source, used to locate the model/context.</param>
    /// <param name="entityType">Requested entity CLR type.</param>
    /// <returns>A compatible unique entity source. Expresso retains authorization by key-correlating it to
    /// <paramref name="source"/>. Returning that same source asserts it is already unique and preserves its scope.</returns>
    IQueryable? GetEntitySource(IQueryable source, Type entityType);
}

/// <summary>Controls child query batching. Budgets count scalar key parameters, including composite components.</summary>
public sealed class ProjectionExecutionOptions
{
    private int _maxKeyParameters = 500;
    /// <summary>Scalar key parameter budget per query; default 500 leaves room for filters/sorts on common providers.</summary>
    public int MaxKeyParameters
    {
        get => _maxKeyParameters;
        set => _maxKeyParameters = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }
}

/// <summary>Materialized typed DTOs and their sparse response shaper.</summary>
public sealed class ProjectedResult<TResult>
{
    internal ProjectedResult(IReadOnlyList<TResult> items, IReadOnlyList<CompositeProjectionKey> keys, Func<TResult, IReadOnlyDictionary<string, object?>> shape)
    {
        Items = items;
        _keys = keys;
        Shape = shape;
    }
    private readonly IReadOnlyList<CompositeProjectionKey> _keys;
    /// <summary>Gets internal identity for a root row, for provider-specific host operations. Sparse shaping never exposes it.</summary>
    public CompositeProjectionKey GetKey(int index) => _keys[index];
    /// <summary>Typed DTOs. Unselected members retain DTO defaults and must be shaped before serialization.</summary>
    public IReadOnlyList<TResult> Items { get; }
    internal Func<TResult, IReadOnlyDictionary<string, object?>> Shape { get; }
}

/// <summary>Framework-neutral response shaping performed only after database materialization.</summary>
public static class ProjectionShaper
{
    /// <summary>Returns sparse objects; selected nulls remain present and internal keys are never exposed.</summary>
    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> ToSparseObjects<TResult>(ProjectedResult<TResult> result)
    {
        if (result is null) throw new ArgumentNullException(nameof(result));
        return result.Items.Select(result.Shape).ToArray();
    }
}
