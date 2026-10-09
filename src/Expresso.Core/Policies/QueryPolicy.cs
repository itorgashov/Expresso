namespace Expresso.Core.Policies;

/// <summary>An opaque compiled policy handle. Use QueryPolicyCompiler in Expresso.Parsing to create a policy.</summary>
public abstract class QueryPolicy
{
    /// <summary>Initializes a policy handle. Parsers accept only compiler-produced implementations.</summary>
    protected QueryPolicy() { }
}

/// <summary>The rules and resource limits to compile against filter and sort catalogs.</summary>
public sealed class QueryPolicyDefinition
{
    /// <summary>Gets or sets the complete, multiline policy source.</summary>
    public string Rules { get; set; } = "";
    /// <summary>Gets or sets limits. Omitted values retain their defaults.</summary>
    public QueryPolicyLimits Limits { get; set; } = new();
    /// <summary>Gets or sets the amount of detail exposed in request exception messages.</summary>
    public QueryPolicyErrorDetail ErrorDetail { get; set; } = QueryPolicyErrorDetail.Generic;
}

/// <summary>Resource limits copied into a policy at compilation.</summary>
public sealed class QueryPolicyLimits
{
    /// <summary>Maximum tree depth, including operands (1 through 100). Defaults to 8.</summary>
    public int MaxDepth { get; set; } = 8;
    /// <summary>Maximum nodes per filter or individual sort key. Defaults to 100.</summary>
    public int MaxNodes { get; set; } = 100;
    /// <summary>Default maximum arguments of and, or, and concat. Defaults to 20.</summary>
    public int MaxArgs { get; set; } = 20;
    /// <summary>Default maximum candidates after the first argument of in. Defaults to 100.</summary>
    public int MaxInItems { get; set; } = 100;
    /// <summary>Maximum string literal length in UTF-16 code units. Defaults to 500.</summary>
    public int MaxStringLength { get; set; } = 500;
    /// <summary>Maximum raw sort keys, including duplicates and nested keys. Defaults to 5.</summary>
    public int MaxSortKeys { get; set; } = 5;
}

/// <summary>Controls the request exception message; structured properties always contain details.</summary>
public enum QueryPolicyErrorDetail
{
    /// <summary>Expose only a stable generic sentence.</summary>
    Generic = 0,
    /// <summary>Include the target, failure kind, rule, limit, and path.</summary>
    Detailed = 1
}

/// <summary>The query component being checked.</summary>
public enum QueryPolicyTarget
{
    /// <summary>A filter expression.</summary>
    Filter,
    /// <summary>A sort directive.</summary>
    Sort
}

/// <summary>The reason a query was rejected.</summary>
public enum QueryPolicyViolationKind
{
    /// <summary>A deny pattern matched a subtree.</summary>
    DenyRuleMatched,
    /// <summary>No allow pattern matched the root.</summary>
    NotAllowed,
    /// <summary>A resource limit was exceeded.</summary>
    LimitExceeded
}
