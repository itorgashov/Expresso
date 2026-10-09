using Expresso.Core.Policies;

namespace Expresso.Parsing.Policies.Syntax;

internal sealed partial class PolicySyntaxParser
{
    private readonly IReadOnlyList<PolicyToken> _tokens;
    private readonly string _source;
    private int _index, _depth;
    private PolicyToken Current => _tokens[_index];
    private PolicyToken Next => _tokens[Math.Min(_index + 1, _tokens.Count - 1)];
    internal PolicySyntaxParser(string source) { _source = source; _tokens = PolicyLexer.Lex(source); }

    internal IReadOnlyList<StatementAst> Parse()
    {
        var result = new List<StatementAst>();
        var errors = new List<QueryPolicyDiagnostic>();
        while (Current.Kind != TokenKind.End)
        {
            int start = _index;
            try { result.Add(Statement()); Take(";"); }
            catch (QueryPolicyCompileException ex)
            {
                errors.AddRange(ex.Diagnostics);
                if (_index == start) _index++;
                while (Current.Kind != TokenKind.End && !Current.Is(";") &&
                    !(Current.Kind == TokenKind.Rule && (Next.Is(":=") || Next.Is("|="))) &&
                    !Current.Is("default") && !Current.Is("deny") &&
                    !((Current.Is("filter") || Current.Is("sort")) && Next.Is(":="))) _index++;
                Take(";");
            }
        }
        if (errors.Count != 0) throw new QueryPolicyCompileException(errors);
        return result;
    }

    private StatementAst Statement()
    {
        var token = Current;
        _index++;
        StatementAst statement;
        if (token.Is("default") || token.Is("deny"))
        {
            statement = new(token, token.Is("default") ? StatementKind.Default : StatementKind.Deny);
            if ((Current.Is("filter") || Current.Is("sort")) && Next.Is(":"))
            { statement.Target = Current.Text.ToLowerInvariant(); _index += 2; }
            if (token.Is("default"))
            {
                if (!Current.Is("allow") && !Current.Is("deny")) Fail("P03", "Expected allow or deny.");
                statement.Allow = Take("allow");
                if (!statement.Allow) Expect("deny");
            }
            else statement.Pattern = Pattern();
        }
        else
        {
            if (token.Kind != TokenKind.Rule && !token.Is("filter") && !token.Is("sort"))
                throw Error(token, "P03", "Expected a rule, default, or deny statement.");
            statement = new(token, StatementKind.Rule) { Name = token.Text.ToLowerInvariant() };
            statement.Extend = Take("|=");
            if (!statement.Extend) Expect(":=");
            statement.Pattern = Pattern();
        }
        var last = _tokens[_index - 1];
        statement.Text = _source.Substring(token.Offset, last.Offset + last.Text.Length - token.Offset).Trim();
        return statement;
    }

    private PatternAst Pattern()
    {
        var first = Term();
        if (!Take("|")) return first;
        var union = new PatternAst(PatternKind.Union, first.Token);
        union.Children.Add(first);
        do { union.Children.Add(Term()); } while (Take("|"));
        return union;
    }

    private PatternAst Term()
    {
        // Bound recursive descent before touching the process stack on hostile input.
        if (++_depth > 128) { _depth--; Fail("P03", "Policy pattern nesting exceeds 128 levels."); }
        try { return ReadTerm(); }
        finally { _depth--; }
    }

    private PatternAst ReadTerm()
    {
        var token = Current;
        if (Take("(")) { var group = Pattern(); Expect(")"); return group; }
        if (Take("~"))
        { var contains = new PatternAst(PatternKind.Contains, token); contains.Children.Add(Term()); return contains; }
        if (Take("?")) return new(PatternKind.Hole, token);
        if (token.Kind is TokenKind.String or TokenKind.Number)
        { _index++; return new(PatternKind.Constant, token) { Name = token.Text }; }
        if (token.Kind == TokenKind.Rule)
        { _index++; return new(PatternKind.Reference, token) { Name = token.Text.ToLowerInvariant() }; }
        if (token.Is("sortfor") && Next.Is("("))
        {
            _index += 2;
            var sortFor = new PatternAst(PatternKind.SortFor, token) { Name = FieldPath() };
            Expect(","); sortFor.Children.Add(Pattern()); Expect(")"); return sortFor;
        }
        if (token.Kind == TokenKind.Name && !Next.Is("("))
            return new(PatternKind.Field, token) { Name = FieldPath() };
        if (token.Is("*") && !Next.Is("(")) { _index++; return new(PatternKind.Any, token); }
        var call = new PatternAst(PatternKind.Call, token);
        if (Take("{"))
        {
            do
            {
                if (Current.Kind is not TokenKind.Name and not TokenKind.Category) Fail("P03", "Expected a function or category in the set.");
                call.Callees.Add(Current); _index++;
            } while (Take(","));
            Expect("}");
        }
        else if (token.Kind is TokenKind.Name or TokenKind.Category || token.Is("*"))
        { call.Callees.Add(token); _index++; }
        else Fail("P03", "Expected a pattern.");
        Expect("("); Arguments(call); Expect(")"); return call;
    }

    private string FieldPath()
    {
        var names = new List<string>();
        do
        {
            if (Current.Kind != TokenKind.Name) Fail("P03", "Expected a field or collection name.");
            names.Add(Current.Text.ToLowerInvariant()); _index++;
        } while (Take("/"));
        return string.Join("/", names);
    }
    private bool Take(string text) { if (!Current.Is(text)) return false; _index++; return true; }
    private void Expect(string text) { if (!Take(text)) Fail("P03", $"Expected '{text}', found '{Current.Text}'."); }
    private void Fail(string code, string message) => throw Error(Current, code, message);
    private static QueryPolicyCompileException Error(PolicyToken token, string code, string message) => new(new[] { token.Error(code, message) });
}
