using Expresso.Core.Filtering;
using Expresso.Core.Policies;

namespace Expresso.Parsing.Policies;

/// <summary>Filter and sort catalogs carrying the same compiled endpoint policy.</summary>
public sealed class QueryPolicyModels
{
    internal QueryPolicyModels(QueryModel filter, QueryModel sort, IEnumerable<QueryPolicyDiagnostic> warnings)
    {
        Filter = filter;
        Sort = sort;
        Warnings = Array.AsReadOnly(warnings.ToArray());
    }
    /// <summary>The filter catalog with its compiled policy.</summary>
    public QueryModel Filter { get; }
    /// <summary>The sort catalog with its compiled policy.</summary>
    public QueryModel Sort { get; }
    /// <summary>Startup warnings, in source order.</summary>
    public IReadOnlyList<QueryPolicyDiagnostic> Warnings { get; }
}
