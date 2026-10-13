namespace Expresso.Core.Projection;

/// <summary>An immutable allow-listed selection of response fields and collection subtrees.</summary>
public sealed class ProjectionDirective
{
    private readonly Dictionary<string, ProjectionSelection> _byName = new Dictionary<string, ProjectionSelection>(StringComparer.OrdinalIgnoreCase);
    /// <summary>Selects the mapping's complete default subtree.</summary>
    public static ProjectionDirective All { get; } = new ProjectionDirective(true);

    /// <summary>Selects no public fields or collections. Internal identity can still be used by an execution plan.</summary>
    public static ProjectionDirective Empty { get; } = new ProjectionDirective(false);

    private ProjectionDirective(bool isAll)
    {
        IsAll = isAll;
        Selections = Array.AsReadOnly(Array.Empty<ProjectionSelection>());
    }

    /// <summary>Creates a non-empty explicit selection. Sibling names must be unique.</summary>
    public ProjectionDirective(IEnumerable<ProjectionSelection> selections)
    {
        if (selections is null) throw new ArgumentNullException(nameof(selections));
        var items = selections.ToArray();
        if (items.Length == 0) throw new ArgumentException("A projection must select at least one field.", nameof(selections));
        ValidateSiblings(items);
        Selections = Array.AsReadOnly(items.OrderBy(s => s.Name, StringComparer.Ordinal).ToArray());
        foreach (var item in items) _byName.Add(item.Name, item);
    }

    /// <summary>Whether all configured public fields and collections are selected.</summary>
    public bool IsAll { get; }
    /// <summary>Explicit selections; empty for <see cref="All"/> and <see cref="Empty"/>.</summary>
    public IReadOnlyList<ProjectionSelection> Selections { get; }

    /// <summary>Resolves an explicit selection by its case-insensitive catalog name.</summary>
    public ProjectionSelection? Find(string name) =>
        name is not null && _byName.TryGetValue(name, out var selection) ? selection : null;

    internal static void ValidateSiblings(ProjectionSelection[] items)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (item is null || !names.Add(item.Name))
                throw new ArgumentException("Selections must be non-null and have unique sibling names.");
        }
    }
}
