using Expresso.Core.Policies;
using Expresso.Parsing.Policies.Syntax;

namespace Expresso.Parsing.Test.Policies;

public class PolicySyntaxParserTests
{
    public static IEnumerable<object[]> ValidPatterns()
    {
        var patterns = new[] { "?", "*", "title", "authors/title", "$p", "~title", "~(title | year)",
            "(title | year)", "\"\"", "-1", "1e3", "eq(sort,?)", "sortfor(authors/awards,title)",
            "{eq,@comparison}(title,?)", "@arithmetic(*)", "*(?)", "eq(default,deny | filter | allow)" };
        foreach (var pattern in patterns)
            foreach (var head in new[] { "filter := ", "sort := ", "$p := ", "deny ", "deny filter: ", "deny sort: " })
                foreach (var suffix in new[] { "", "; # end" })
                    yield return new object[] { head + pattern + suffix };
    }

    [Theory, MemberData(nameof(ValidPatterns))]
    public void AcceptsProductions(string source) => Assert.Single(new PolicySyntaxParser(source).Parse());

    public static IEnumerable<object[]> Bounds()
    {
        foreach (var bound in new[] { "", "[1]", "[1,2]", "[1,]", "[,2]", "[ 1 , 2 ]" })
            foreach (var args in new[] { "...", "*...", "$p...", "?,$p...", "?,title,$p...", "title | year..." })
                yield return new object[] { "filter := f(" + args + bound + ")" };
    }
    [Theory, MemberData(nameof(Bounds))]
    public void AcceptsBounds(string source) => Assert.Single(new PolicySyntaxParser(source).Parse());

    [Theory]
    [InlineData("filter := f()")]
    [InlineData("filter := f(?...,?)")]
    [InlineData("filter := f(?...,?...) ")]
    [InlineData("filter := f(...,?,? ,...)")]
    [InlineData("filter := f(...,?,...)[2]")]
    [InlineData("filter := f(...,?,...[2])")]
    [InlineData("filter := f(?...[])")]
    [InlineData("filter := f(?...[,])")]
    [InlineData("filter := f(?...[2,1])")]
    [InlineData("filter := f(?...[1,2,3])")]
    [InlineData("filter := f(?...[a])")]
    [InlineData("filter := f(?...[-1])")]
    [InlineData("filter := f(?...[1.5])")]
    [InlineData("filter := f(?...[1e3])")]
    [InlineData("filter := f(?...[99999999999])")]
    [InlineData("filter := f(?")]
    [InlineData("filter := )")]
    [InlineData("filter := ? |")]
    [InlineData("filter := | ?")]
    [InlineData("filter := ~")]
    [InlineData("filter := {}(?)")]
    [InlineData("filter := {eq,}(?)")]
    [InlineData("filter := authors/")]
    [InlineData("default maybe")]
    [InlineData("foo := ?")]
    public void RejectsMalformedSyntax(string source) =>
        Assert.Throws<QueryPolicyCompileException>(() => new PolicySyntaxParser(source).Parse());

    [Theory]
    [InlineData("default allow default sort: deny")]
    [InlineData("deny sort deny sort: title")]
    [InlineData("sort := title filter := eq(title,?)")]
    [InlineData("$p := eq(title,?)\n| eq(year,?)\n$p |= eq(price,?)")]
    public void StatementBoundaries(string source) => Assert.Equal(2, new PolicySyntaxParser(source).Parse().Count);

    [Fact]
    public void ExistsHasOnePattern()
    {
        var pattern = new PolicySyntaxParser("deny f(..., title | year, ...)").Parse()[0].Pattern!;
        Assert.Equal(ArgumentShape.Exists, pattern.Shape);
        Assert.Single(pattern.Children);
    }

    [Fact]
    public void DeepNestingFailsWithoutStackOverflow() => Assert.Throws<QueryPolicyCompileException>(() =>
        new PolicySyntaxParser("filter := " + new string('(', 1000) + "?" + new string(')', 1000)).Parse());

    [Fact]
    public void CollectsSyntaxErrors() => Assert.Equal(2, Assert.Throws<QueryPolicyCompileException>(() =>
        new PolicySyntaxParser("filter := f(); sort := f()").Parse()).Diagnostics.Count);
}
