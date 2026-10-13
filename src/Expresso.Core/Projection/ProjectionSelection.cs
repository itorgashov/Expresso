namespace Expresso.Core.Projection;

/// <summary>One scalar field, explicit collection subtree, or complete default collection subtree.</summary>
public sealed class ProjectionSelection
{
    private readonly ProjectionDirective _directive;
    /// <summary>Creates a selection. A default subtree cannot also have explicit children.</summary>
    public ProjectionSelection(string name, IEnumerable<ProjectionSelection>? children = null, bool selectDefaultSubtree = false)
        : this(name, children, selectDefaultSubtree, null) { }

    private ProjectionSelection(string name, IEnumerable<ProjectionSelection>? children, bool selectDefaultSubtree, ProjectionDirective? directive)
    {
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(new[] { '.', ',', ' ', '\t', '\r', '\n' }) >= 0)
            throw new ArgumentException("A selection name must be a single non-empty catalog name.", nameof(name));
        var items = children?.ToArray() ?? Array.Empty<ProjectionSelection>();
        ProjectionDirective.ValidateSiblings(items);
        if (selectDefaultSubtree && items.Length != 0)
            throw new ArgumentException("A default subtree cannot have explicit children.", nameof(children));
        Name = name.ToLowerInvariant();
        Children = Array.AsReadOnly(items.OrderBy(s => s.Name, StringComparer.Ordinal).ToArray());
        SelectDefaultSubtree = selectDefaultSubtree;
        _directive = directive ?? (items.Length == 0 ? ProjectionDirective.All : new ProjectionDirective(items));
    }

    /// <summary>Selects an explicit collection subtree, including <see cref="ProjectionDirective.Empty"/> without expanding the mapping.</summary>
    public static ProjectionSelection Subtree(string name, ProjectionDirective directive)
    {
        if (directive is null) throw new ArgumentNullException(nameof(directive));
        return new ProjectionSelection(name, directive.Selections, directive.IsAll, directive);
    }

    /// <summary>Normalized catalog name.</summary>
    public string Name { get; }
    /// <summary>Explicit nested selections.</summary>
    public IReadOnlyList<ProjectionSelection> Children { get; }
    /// <summary>Whether to select the complete configured collection subtree.</summary>
    public bool SelectDefaultSubtree { get; }
    /// <summary>Whether this selection explicitly describes a collection rather than a scalar, including an empty subtree.</summary>
    public bool IsCollectionSubtree => SelectDefaultSubtree || Children.Count > 0 || ReferenceEquals(_directive, ProjectionDirective.Empty);
    /// <summary>Gets the directive for a selected collection.</summary>
    public ProjectionDirective ToDirective() => _directive;
}
