using Expresso.Core.Policies;

namespace Expresso.Parsing.Policies.Syntax;

internal enum TokenKind { Name, Rule, Category, String, Number, Punctuation, End }

internal sealed class PolicyToken
{
    internal PolicyToken(TokenKind kind, string text, int line, int column, int offset)
    {
        Kind = kind;
        Text = text;
        Line = line;
        Column = column;
        Offset = offset;
    }
    internal TokenKind Kind { get; }
    internal string Text { get; }
    internal int Line { get; }
    internal int Column { get; }
    internal int Offset { get; }
    internal bool Is(string text) => Text.Equals(text, StringComparison.OrdinalIgnoreCase);
    internal QueryPolicyDiagnostic Error(string code, string message) =>
        new(code, QueryPolicySeverity.Error, Line, Column, message);
}
