using System.Collections;

namespace Expresso.Core.Projection;

/// <summary>Immutable structural identity used to correlate projected rows, including composite keys.</summary>
public sealed class CompositeProjectionKey : IEquatable<CompositeProjectionKey>
{
    /// <summary>Creates a key from one or more ordered scalar components, copying the input.</summary>
    public CompositeProjectionKey(IEnumerable<object?> components)
    {
        if (components is null) throw new ArgumentNullException(nameof(components));
        var values = components.Select(v => v is Array a ? a.Clone() : v).ToArray();
        if (values.Length == 0) throw new ArgumentException("A key requires at least one component.", nameof(components));
        _values = values;
    }

    private readonly object?[] _values;
    /// <summary>Number of ordered components.</summary>
    public int Count => _values.Length;
    /// <summary>Gets a component. Arrays are copied to preserve immutability.</summary>
    public object? this[int index] => _values[index] is Array a ? a.Clone() : _values[index];
    /// <summary>Whether every component is non-null.</summary>
    public bool IsComplete => _values.All(v => v is not null);
    /// <inheritdoc />
    public bool Equals(CompositeProjectionKey? other) => other is not null &&
        StructuralComparisons.StructuralEqualityComparer.Equals(_values, other._values);
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is CompositeProjectionKey other && Equals(other);
    /// <inheritdoc />
    public override int GetHashCode() => StructuralComparisons.StructuralEqualityComparer.GetHashCode(_values);
}

