using System.Linq.Expressions;
using System.Reflection;
using Expresso.Core.Filtering;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;

namespace Expresso.Rendering.Linq;

/// <summary>Shared metadata for a typed entity-to-result projection.</summary>
public abstract class LinqProjectionMapping
{
    internal readonly List<(string Name, LambdaExpression Source)> Keys = new List<(string, LambdaExpression)>();
    internal readonly List<ProjectionField> Fields = new List<ProjectionField>();
    internal readonly List<ProjectionCollection> Collections = new List<ProjectionCollection>();
    internal bool Frozen;
    internal bool Contains(LinqProjectionMapping mapping) => ReferenceEquals(this, mapping) || Collections.Any(c => c.Items.Contains(mapping));
    internal void Destination(MemberInfo member)
    {
        if (Fields.Any(f => f.Target == member) || Collections.Any(c => c.Target == member))
            throw new ArgumentException("Each projection requires a distinct DTO destination member.");
    }
    internal static void Scalar(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (!(type.IsValueType || type == typeof(string) || type == typeof(byte[])))
            throw new ArgumentException("Projection values must be provider-translatable scalars.");
    }
    internal void Writable()
    {
        if (Frozen) throw new InvalidOperationException("A rendered projection mapping cannot be changed.");
    }
    internal void Name(string name, string outputName)
    {
        Writable();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(outputName))
            throw new ArgumentException("Projection names must not be empty.");
        if (Fields.Any(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase) || f.OutputName == outputName) ||
            Collections.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase) || c.OutputName == outputName))
            throw new ArgumentException("Duplicate projection field or output name: " + name);
    }
    internal void Validate(ProjectionDirective directive)
    {
        if (Keys.Count == 0) throw new ArgumentException("A projection mapping requires at least one key.");
        foreach (var selection in directive.Selections)
        {
            var field = Fields.FirstOrDefault(f => string.Equals(f.Name, selection.Name, StringComparison.OrdinalIgnoreCase));
            var collection = Collections.FirstOrDefault(c => string.Equals(c.Name, selection.Name, StringComparison.OrdinalIgnoreCase));
            if (field is null && collection is null) throw new ArgumentException("Unknown projection mapping: " + selection.Name);
            if (field is not null && selection.IsCollectionSubtree)
                throw new ArgumentException("Cannot traverse a scalar projection field.");
        }
        foreach (var collection in Collections)
        {
            var selected = directive.Find(collection.Name);
            if (directive.IsAll || selected is not null)
                collection.Items.Validate(directive.IsAll ? ProjectionDirective.All : selected!.ToDirective());
        }
        Frozen = true;
    }
    internal Func<object, IReadOnlyDictionary<string, object?>> BuildShaper(ProjectionDirective directive)
    {
        var fields = Fields.Where(f => directive.IsAll || directive.Find(f.Name) is not null).ToArray();
        var children = Collections.Where(c => directive.IsAll || directive.Find(c.Name) is not null)
            .Select(c => (Collection: c, Shape: c.Items.BuildShaper(directive.IsAll ? ProjectionDirective.All : directive.Find(c.Name)!.ToDirective())))
            .ToArray();
        return item =>
        {
            var result = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var field in fields) result.Add(field.OutputName, field.Read(item));
            foreach (var child in children)
                result.Add(child.Collection.OutputName, child.Collection.Read(item).Select(child.Shape).ToArray());
            return new System.Collections.ObjectModel.ReadOnlyDictionary<string, object?>(result);
        };
    }

    internal void ValidateCollectionSources(IQueryable source, ProjectionDirective directive, IProjectionSourceValidator validator)
    {
        foreach (var collection in Collections)
        {
            var selected = directive.Find(collection.Name);
            if (!directive.IsAll && selected is null) continue;
            // Compose only the requested navigation scopes; no rows, normalization, keys, or query translation are needed.
            var items = collection.ItemScope(source);
            validator.ValidateSource(items);
            collection.Items.ValidateCollectionSources(items, directive.IsAll ? ProjectionDirective.All : selected!.ToDirective(), validator);
        }
    }
}

/// <summary>Fluent typed projection mapping. Keys are internal unless separately mapped as public fields.</summary>
public sealed partial class LinqProjectionMapping<TEntity, TResult> : LinqProjectionMapping where TResult : new()
{
    /// <summary>Declares one ordered scalar member component of the entity's identity. The selector must be rooted in the entity parameter.</summary>
    public LinqProjectionMapping<TEntity, TResult> Key<TKey>(string keyName, Expression<Func<TEntity, TKey>> source)
    {
        Writable();
        if (string.IsNullOrWhiteSpace(keyName) || Keys.Any(k => string.Equals(k.Name, keyName, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Projection keys require unique, non-empty names.");
        if (source is null) throw new ArgumentNullException(nameof(source));
        Scalar(typeof(TKey));
        Expression? root = source.Body;
        while (root is MemberExpression member) root = member.Expression;
        if (source.Body is not MemberExpression || root != source.Parameters[0])
            throw new ArgumentException("Key selectors must be scalar member access expressions rooted in the entity.");
        Keys.Add((keyName.ToLowerInvariant(), source));
        return this;
    }

    /// <summary>Maps a public field to a typed result member and its response property name.</summary>
    public LinqProjectionMapping<TEntity, TResult> Field<TValue>(string name, string outputName,
        Expression<Func<TEntity, TValue>> source, Expression<Func<TResult, TValue>> target, Func<TValue, object?>? outputConverter = null)
    {
        Name(name, outputName);
        if (source is null) throw new ArgumentNullException(nameof(source));
        Scalar(typeof(TValue));
        var member = Target(target, writable: true);
        Destination(member);
        var getter = target.Compile();
        Fields.Add(new ProjectionField(name.ToLowerInvariant(), outputName, source, member,
            item => outputConverter is null ? getter((TResult)item) : outputConverter(getter((TResult)item))));
        return this;
    }

    /// <summary>Maps a navigation and initialized mutable DTO collection. The renderer owns child querying and attachment.</summary>
    public LinqProjectionMapping<TEntity, TResult> Collection<TItem, TItemResult>(
        string name, string outputName, Expression<Func<TEntity, IEnumerable<TItem>>> navigation,
        Expression<Func<TResult, ICollection<TItemResult>>> target,
        LinqProjectionMapping<TItem, TItemResult> items, LinqQueryMapping<TItem> itemQueryMapping,
        SortDirective? defaultSort = null) where TItemResult : new()
    {
        Name(name, outputName);
        if (navigation is null) throw new ArgumentNullException(nameof(navigation));
        if (items is null) throw new ArgumentNullException(nameof(items));
        if (itemQueryMapping is null) throw new ArgumentNullException(nameof(itemQueryMapping));
        if (items.Contains(this)) throw new ArgumentException("Projection mappings must form a finite, acyclic tree.", nameof(items));
        var member = Target(target, writable: false);
        Destination(member);
        var getter = target.Compile();
        var sample = getter(new TResult());
        if (sample is null || sample.IsReadOnly) throw new ArgumentException("Target collections must be initialized and mutable.");
        Collections.Add(new ProjectionCollection<TEntity, TResult, TItem, TItemResult>(name.ToLowerInvariant(),
            outputName, member, navigation, getter, items, itemQueryMapping, defaultSort));
        return this;
    }

    internal Expression<Func<TEntity, TResult>> Selector(ProjectionDirective directive)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var bindings = Fields.Where(f => directive.IsAll || directive.Find(f.Name) is not null)
            .Select(f => Expression.Bind(f.Target, ProjectionExpressions.Replace(f.Source, parameter)));
        return Expression.Lambda<Func<TEntity, TResult>>(Expression.MemberInit(Expression.New(typeof(TResult)), bindings), parameter);
    }
    /// <summary>Gets the projection allow-list using the mapping's public names and source CLR types.</summary>
    public QueryModel GetProjectionModel() => new QueryModel(Fields.Select(f => (f.Name, f.Source.ReturnType)).ToArray(),
        Collections.Select(c => new CollectionModel(c.Name, c.GetModel())).ToArray());

    private static MemberInfo Target<TValue>(Expression<Func<TResult, TValue>> target, bool writable)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (target.Body is not MemberExpression member || member.Expression != target.Parameters[0])
            throw new ArgumentException("Target selectors must name a direct DTO member.");
        if (member.Member is PropertyInfo property)
        {
            if (property.GetMethod?.IsPublic != true || (writable && property.SetMethod?.IsPublic != true))
                throw new ArgumentException("Projection target properties must be public and writable.");
        }
        else if (member.Member is not FieldInfo field || !field.IsPublic || (writable && field.IsInitOnly))
            throw new ArgumentException("Projection target members must be public and writable.");
        return member.Member;
    }
}

internal sealed class ProjectionField
{
    public ProjectionField(string name, string outputName, LambdaExpression source, MemberInfo target, Func<object, object?> read)
    {
        Name = name;
        OutputName = outputName;
        Source = source;
        Target = target;
        Read = read;
    }
    public string Name { get; }
    public string OutputName { get; }
    public LambdaExpression Source { get; }
    public MemberInfo Target { get; }
    public Func<object, object?> Read { get; }
}
