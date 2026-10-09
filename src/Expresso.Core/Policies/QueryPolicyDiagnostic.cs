namespace Expresso.Core.Policies;

/// <summary>Severity of a policy compilation diagnostic.</summary>
public enum QueryPolicySeverity
{
    /// <summary>The policy can be compiled, but may need attention.</summary>
    Warning,
    /// <summary>The policy cannot be compiled.</summary>
    Error
}

/// <summary>An immutable diagnostic at a one-based source position.</summary>
public sealed class QueryPolicyDiagnostic
{
    /// <summary>Creates a diagnostic with a stable code and source location.</summary>
    public QueryPolicyDiagnostic(string code, QueryPolicySeverity severity, int line, int column, string message)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Severity = severity;
        Line = line;
        Column = column;
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }
    /// <summary>The stable diagnostic code.</summary>
    public string Code { get; }
    /// <summary>The severity.</summary>
    public QueryPolicySeverity Severity { get; }
    /// <summary>The one-based line, or 1 for envelope diagnostics.</summary>
    public int Line { get; }
    /// <summary>The one-based column, or 1 for envelope diagnostics.</summary>
    public int Column { get; }
    /// <summary>The explanation for the policy author.</summary>
    public string Message { get; }
    /// <summary>Formats the code, location, and explanation.</summary>
    public override string ToString() => $"{Code} ({Line},{Column}): {Message}";
}

/// <summary>A policy compilation failure containing all collected diagnostics in source order.</summary>
public sealed class QueryPolicyCompileException : ArgumentException
{
    /// <summary>Creates a compilation failure and snapshots its diagnostics.</summary>
    public QueryPolicyCompileException(IEnumerable<QueryPolicyDiagnostic> diagnostics)
        : this((diagnostics ?? throw new ArgumentNullException(nameof(diagnostics)))
            .OrderBy(d => d.Line).ThenBy(d => d.Column).ToArray()) { }

    private QueryPolicyCompileException(QueryPolicyDiagnostic[] diagnostics)
        : base("Query policy compilation failed: " + string.Join("; ", diagnostics.Select(d => d.ToString())))
    {
        Diagnostics = Array.AsReadOnly(diagnostics);
    }
    /// <summary>A read-only snapshot ordered by line and column.</summary>
    public IReadOnlyList<QueryPolicyDiagnostic> Diagnostics { get; }
}
