using Expresso.Core.Policies;
using System.Text;

namespace Expresso.Core.Filtering;

public sealed partial class QueryModel
{
    private readonly Lazy<string> _catalogFingerprint;

    /// <summary>The optional compiled endpoint policy. Null preserves unrestricted parsing.</summary>
    public QueryPolicy? Policy { get; }

    /// <summary>A lazy, canonical identity of the complete field and collection catalog.</summary>
    public string CatalogFingerprint => _catalogFingerprint.Value;

    /// <summary>Returns a new model sharing this catalog and carrying the supplied policy.</summary>
    public QueryModel WithPolicy(QueryPolicy? policy) => new(this, policy);

    private QueryModel(QueryModel source, QueryPolicy? policy)
    {
        // Dictionaries are private and immutable after construction. Do not rebuild from
        // the public arrays, which callers can mutate without changing name resolution.
        _fields = source._fields;
        _collections = source._collections;
        Fields = _fields.Select(p => (p.Key, p.Value)).ToArray();
        Collections = _collections.Values.ToArray();
        _catalogFingerprint = source._catalogFingerprint;
        Policy = policy;
    }

    private string BuildCatalogFingerprint()
    {
        var text = new StringBuilder();
        void Append(string value) => text.Append(value.Length).Append(':').Append(value);
        foreach (var pair in _fields.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            text.Append('F');
            Append(pair.Key);
            Append(pair.Value.AssemblyQualifiedName ?? pair.Value.FullName ?? pair.Value.Name);
        }
        foreach (var pair in _collections.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            text.Append('C');
            Append(pair.Key);
            Append(pair.Value.Items.CatalogFingerprint);
        }
        return text.ToString();
    }
}
