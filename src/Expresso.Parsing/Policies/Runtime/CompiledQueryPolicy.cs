using Expresso.Core.Policies;
using Expresso.Parsing.Policies.Binding;

namespace Expresso.Parsing.Policies.Runtime;

internal sealed class CompiledQueryPolicy : QueryPolicy
{
    internal CompiledQueryPolicy(PatternTable filter, PatternTable sort, QueryPolicyLimits limits,
        QueryPolicyErrorDetail errorDetail, string filterFingerprint, string sortFingerprint)
    {
        Filter = filter;
        Sort = sort;
        Limits = limits;
        ErrorDetail = errorDetail;
        FilterFingerprint = filterFingerprint;
        SortFingerprint = sortFingerprint;
    }
    internal PatternTable Filter { get; }
    internal PatternTable Sort { get; }
    internal QueryPolicyLimits Limits { get; }
    internal QueryPolicyErrorDetail ErrorDetail { get; }
    internal string FilterFingerprint { get; }
    internal string SortFingerprint { get; }
}
