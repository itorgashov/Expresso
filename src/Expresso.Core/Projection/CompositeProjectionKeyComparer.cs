using System.Collections;

namespace Expresso.Core.Projection;

/// <summary>Compares ordered key components using application-supplied database-compatible equality rules.</summary>
/// <remarks>Comparers must match the actual key collations and implement consistent equality/hashing.
/// Null comparer entries use structural CLR equality, including binary content comparison. Original key values are not changed.</remarks>
public sealed class CompositeProjectionKeyComparer : IEqualityComparer<CompositeProjectionKey>
{
    private readonly IEqualityComparer?[] _components;

    /// <summary>Creates a comparer with one entry per ordered key component; null entries retain structural equality.</summary>
    public CompositeProjectionKeyComparer(params IEqualityComparer?[] componentComparers)
    {
        if (componentComparers is null) throw new ArgumentNullException(nameof(componentComparers));
        if (componentComparers.Length == 0) throw new ArgumentException("A key comparer requires at least one component.", nameof(componentComparers));
        _components = (IEqualityComparer?[])componentComparers.Clone();
    }

    /// <summary>Number of ordered components covered by this comparer.</summary>
    public int ComponentCount => _components.Length;

    /// <inheritdoc />
    public bool Equals(CompositeProjectionKey? left, CompositeProjectionKey? right)
    {
        if (left is null || right is null) return left is null && right is null;
        Validate(left);
        Validate(right);
        for (var index = 0; index < _components.Length; index++)
            if (!(_components[index] ?? StructuralComparisons.StructuralEqualityComparer).Equals(left[index], right[index])) return false;
        return true;
    }

    /// <inheritdoc />
    public int GetHashCode(CompositeProjectionKey key)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));
        Validate(key);
        unchecked
        {
            var hash = 17;
            for (var index = 0; index < _components.Length; index++)
            {
                var value = key[index];
                hash = hash * 31 + (value is null ? 0 : (_components[index] ?? StructuralComparisons.StructuralEqualityComparer).GetHashCode(value));
            }
            return hash;
        }
    }

    private void Validate(CompositeProjectionKey key)
    {
        if (key.Count != _components.Length) throw new ArgumentException("Key components must match the configured comparer.", nameof(key));
    }
}
