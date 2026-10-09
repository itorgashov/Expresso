using Expresso.Core.Policies;
using Expresso.Parsing.Policies.Syntax;

namespace Expresso.Parsing.Test.Policies;

public class PolicyLexerTests
{
    public static IEnumerable<object[]> Tokens()
    {
        var groups = new[] {
            (TokenKind.Name, "eq Title _field deny default sort filter allow"),
            (TokenKind.Rule, "$p $Pred $_a"), (TokenKind.Category, "@arithmetic @DateTime-Add"),
            (TokenKind.Number, "0 -1 1.5 1e3 2E-2 3e+1 2147483648"),
            (TokenKind.Punctuation, "( ) , | { } [ ] ... ~ ? * / := |= : ;") };
        foreach (var group in groups)
            foreach (var token in group.Item2.Split(' '))
                foreach (var prefix in new[] { "", "# comment\r\n\t" })
                    yield return new object[] { prefix, token, (int)group.Item1 };
    }

    [Theory, MemberData(nameof(Tokens))]
    public void TokenKindsAndLocations(string prefix, string token, int kind)
    {
        var tokens = PolicyLexer.Lex(prefix + token);
        Assert.Equal(2, tokens.Count);
        Assert.Equal((TokenKind)kind, tokens[0].Kind);
        Assert.Equal(token, tokens[0].Text);
        Assert.Equal(prefix.Length, tokens[0].Offset);
        Assert.Equal(prefix.Length == 0 ? 1 : 2, tokens[0].Line);
        Assert.Equal(prefix.Length == 0 ? 1 : 2, tokens[0].Column);
        Assert.Equal(TokenKind.End, tokens[1].Kind);
    }

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"unicode: \u03b1\u00e9\"")]
    [InlineData("\"eq(title,?) # not a comment\"")]
    public void QuotedStrings(string source) => Assert.Equal(source, PolicyLexer.Lex(source)[0].Text);

    [Theory]
    [InlineData("$")]
    [InlineData("@")]
    [InlineData("@a-")]
    [InlineData("@a--b")]
    [InlineData("!")]
    [InlineData(".")]
    [InlineData("1e")]
    [InlineData("1e+")]
    [InlineData("\"unclosed")]
    [InlineData("\"line\nbreak\"")]
    [InlineData("\"line\rbreak\"")]
    public void InvalidTokensHaveLocations(string source)
    {
        var e = Assert.Throws<QueryPolicyCompileException>(() => PolicyLexer.Lex("# comment\n  " + source));
        Assert.Equal(2, e.Diagnostics[0].Line);
        Assert.True(e.Diagnostics[0].Column >= 3);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    [InlineData("# \u03b1\u00e9 comment\r\n# second")]
    public void EmptyInput(string source) => Assert.Single(PolicyLexer.Lex(source));

    [Fact]
    public void LongestMatch() => Assert.Equal(new[] { "1", "...", ":=", ":", "|=", "|", "" },
        PolicyLexer.Lex("1... := : |= |").Select(t => t.Text));
}
