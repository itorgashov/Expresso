namespace Expresso.Parsing.Policies.Syntax;

internal enum PatternKind { Any, Hole, Constant, Field, Reference, Union, Contains, Call, SortFor }
internal enum ArgumentShape { Fixed, Tail, Exists }
internal enum StatementKind { Rule, Deny, Default }

internal sealed class PatternAst
{
    internal PatternAst(PatternKind kind, PolicyToken token)
    {
        Kind = kind;
        Token = token;
    }
    internal PatternKind Kind { get; }
    internal PolicyToken Token { get; }
    internal string Name { get; set; } = "";
    internal List<PatternAst> Children { get; } = new();
    internal List<PolicyToken> Callees { get; } = new();
    internal ArgumentShape Shape { get; set; }
    internal PatternAst? Tail { get; set; }
    internal int? Minimum { get; set; }
    internal int? Maximum { get; set; }
    internal bool ExplicitBounds { get; set; }
}

internal sealed class StatementAst
{
    internal StatementAst(PolicyToken token, StatementKind kind)
    {
        Token = token;
        Kind = kind;
    }
    internal PolicyToken Token { get; }
    internal StatementKind Kind { get; }
    internal string? Target { get; set; }
    internal string Name { get; set; } = "";
    internal bool Extend { get; set; }
    internal bool Allow { get; set; }
    internal PatternAst? Pattern { get; set; }
    internal string Text { get; set; } = "";
}
