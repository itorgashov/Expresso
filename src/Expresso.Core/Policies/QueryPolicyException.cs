namespace Expresso.Core.Policies;

/// <summary>A valid query rejected by its endpoint's policy.</summary>
public sealed class QueryPolicyException : ArgumentException
{
    /// <summary>Creates a violation with structured details for server-side logging.</summary>
    public QueryPolicyException(QueryPolicyTarget target, QueryPolicyViolationKind kind,
        string? path = null, string? ruleText = null, string? limitName = null,
        QueryPolicyErrorDetail errorDetail = QueryPolicyErrorDetail.Generic)
        : base(errorDetail == QueryPolicyErrorDetail.Generic
            ? "The query is not permitted by the endpoint policy."
            : $"Query policy rejected {target}: {kind}. Path: {path ?? "(root)"}. Rule: {ruleText ?? "(none)"}. Limit: {limitName ?? "(none)"}.")
    {
        Target = target;
        Kind = kind;
        Path = path;
        RuleText = ruleText;
        LimitName = limitName;
    }

    /// <summary>The rejected query component.</summary>
    public QueryPolicyTarget Target { get; }
    /// <summary>The rejection reason.</summary>
    public QueryPolicyViolationKind Kind { get; }
    /// <summary>A deterministic expression path, when available.</summary>
    public string? Path { get; }
    /// <summary>The matched deny statement, when available. Intended for logging.</summary>
    public string? RuleText { get; }
    /// <summary>The exceeded resource limit, when applicable.</summary>
    public string? LimitName { get; }
}
