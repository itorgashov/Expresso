using Newtonsoft.Json.Serialization;

namespace Expresso.Sample.WebApi.NetFx.Ef6.Infrastructure;

/// <summary>Preserves the host's existing PascalCase DTO property names for sparse dictionary responses.</summary>
public sealed class ProjectionJsonContractResolver : DefaultContractResolver
{
    /// <inheritdoc />
    protected override string ResolveDictionaryKey(string dictionaryKey) => string.IsNullOrEmpty(dictionaryKey)
        ? dictionaryKey : char.ToUpperInvariant(dictionaryKey[0]) + dictionaryKey.Substring(1);
}
