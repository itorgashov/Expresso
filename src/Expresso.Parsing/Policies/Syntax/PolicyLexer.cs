using Expresso.Core.Policies;

namespace Expresso.Parsing.Policies.Syntax;

internal static class PolicyLexer
{
    internal static IReadOnlyList<PolicyToken> Lex(string text)
    {
        var tokens = new List<PolicyToken>();
        var errors = new List<QueryPolicyDiagnostic>();
        int offset = 0, line = 1, column = 1;
        char Peek(int distance = 0) => offset + distance < text.Length ? text[offset + distance] : '\0';
        void Advance()
        {
            if (text[offset++] == '\n') { line++; column = 1; }
            else column++;
        }
        while (offset < text.Length)
        {
            char c = Peek();
            if (c is ' ' or '\t' or '\r' or '\n') { Advance(); continue; }
            if (c == '#') { while (offset < text.Length && Peek() != '\n') Advance(); continue; }
            int start = offset, startLine = line, startColumn = column;
            TokenKind kind = TokenKind.Punctuation;
            if (c == '"')
            {
                kind = TokenKind.String;
                Advance();
                while (offset < text.Length && Peek() != '"' && Peek() != '\r' && Peek() != '\n') Advance();
                if (Peek() != '"') errors.Add(new("P02", QueryPolicySeverity.Error, startLine, startColumn, "Unterminated string; line breaks are not allowed."));
                else Advance();
            }
            else if (IsNameStart(c) || c is '$' or '@')
            {
                kind = c == '$' ? TokenKind.Rule : c == '@' ? TokenKind.Category : TokenKind.Name;
                if (c is '$' or '@') Advance();
                bool category = kind == TokenKind.Category;
                if (!(category ? IsLetter(Peek()) : IsNameStart(Peek())))
                {
                    errors.Add(new("P01", QueryPolicySeverity.Error, startLine, startColumn, "Expected a name after the prefix."));
                }
                else
                {
                    Advance();
                    while (category ? IsLetter(Peek()) || Peek() == '-' : IsNameStart(Peek()) || IsDigit(Peek())) Advance();
                    if (category && (text[offset - 1] == '-' || text.Substring(start, offset - start).Contains("--")))
                        errors.Add(new("P01", QueryPolicySeverity.Error, startLine, startColumn, "Category segments must contain letters."));
                }
            }
            else if (IsDigit(c) || c == '-' && IsDigit(Peek(1)))
            {
                kind = TokenKind.Number;
                if (c == '-') Advance();
                while (IsDigit(Peek())) Advance();
                if (Peek() == '.' && IsDigit(Peek(1)))
                { Advance(); while (IsDigit(Peek())) Advance(); }
                if (Peek() is 'e' or 'E')
                {
                    Advance();
                    if (Peek() is '+' or '-') Advance();
                    if (!IsDigit(Peek())) errors.Add(new("P01", QueryPolicySeverity.Error, line, column, "Expected exponent digits."));
                    while (IsDigit(Peek())) Advance();
                }
            }
            else if (c == '.' && Peek(1) == '.' && Peek(2) == '.') { Advance(); Advance(); Advance(); }
            else if ((c == ':' || c == '|') && Peek(1) == '=') { Advance(); Advance(); }
            else if ("(),|{}[]~?*/:;".IndexOf(c) >= 0) Advance();
            else
            {
                errors.Add(new("P01", QueryPolicySeverity.Error, line, column, $"Unexpected character '{c}'."));
                Advance();
            }
            tokens.Add(new(kind, text.Substring(start, offset - start), startLine, startColumn, start));
        }
        tokens.Add(new(TokenKind.End, "", line, column, offset));
        if (errors.Count != 0) throw new QueryPolicyCompileException(errors);
        return tokens;
    }
    private static bool IsLetter(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
    private static bool IsNameStart(char c) => IsLetter(c) || c == '_';
    private static bool IsDigit(char c) => c is >= '0' and <= '9';
}
