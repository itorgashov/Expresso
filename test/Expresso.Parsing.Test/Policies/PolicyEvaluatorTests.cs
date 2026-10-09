using Expresso.Core.Policies;

namespace Expresso.Parsing.Test.Policies;

public class PolicyEvaluatorTests
{
    public static IEnumerable<object[]> Cases()
    {
        var cases = new (string Policy, string Query, bool Accepted)[]
        {
            ("filter := eq(title,?)", "eq(title,\"x\")", true),
            ("filter := eq(title,?)", "eq(title,publisher)", false),
            ("filter := gt(age,?)", "gt(30,age)", false),
            ("filter := eq(title,\"a\" | \"b\")", "eq(title,\"a\")", true),
            ("filter := eq(title,\"a\" | \"b\")", "eq(title,\"A\")", false),
            ("filter := eq(title,\"\")", "eq(title,\"\")", true),
            ("filter := eq(year,-2)", "eq(year,-2)", true),
            ("filter := eq(price,10)", "eq(price,10.0)", true),
            ("filter := eq(price,10)", "eq(price,11)", false),
            ("filter := eq(createdat,\"2026-10-08\")", "eq(createdat,\"2026-10-08\")", true),
            ("filter := eq(id,\"11111111-1111-1111-1111-111111111111\")", "eq(id,\"11111111-1111-1111-1111-111111111111\")", true),
            ("filter := eq(clock,\"12:30:00\")", "eq(clock,\"12:30:00\")", true),
            ("filter := eq(30,age)", "eq(30,age)", true),
            ("filter := eq(title,publisher)", "eq(title,publisher)", true),
            ("filter := eq(title,?)", "eq(1,1)", false),
            ("default allow", "eq(1,1)", true),
            ("default allow deny eq(?,?)", "eq(1,1)", false),
            ("filter := $p | and($p...) $p := eq(title,?)", "and(eq(title,\"a\"),eq(title,\"b\"))", true),
            ("filter := $p | and($p...) $p := eq(title,?)", "or(eq(title,\"a\"),eq(title,\"b\"))", false),
            ("filter := $p | and($p...) $p := eq(title,?)", "and(eq(title,\"a\"),and(eq(title,\"b\"),eq(title,\"c\")))", false),
            ("filter := $p $p := eq(title,?) | and($p...) | not($p)", "not(and(eq(title,\"a\"),eq(title,\"b\")))", true),
            ("filter := $a $a := $b | not($a) $b := eq(title,?)", "not(not(eq(title,\"a\")))", true),
            ("filter := and(...,eq(year,?),...)", "and(eq(title,\"a\"),eq(year,1))", true),
            ("filter := and(...,eq(year,?),...)", "and(eq(year,1),eq(title,\"a\"))", true),
            ("filter := and(...,eq(year,?),...)", "and(eq(title,\"a\"),eq(year,1),eq(title,\"b\"))", true),
            ("filter := and(...,eq(year,?),...)", "and(eq(title,\"a\"),eq(title,\"b\"))", false),
            ("filter := any(authors,eq(displayname,?))", "any(authors,eq(displayname,\"a\"))", true),
            ("filter := any(authors,eq(displayname,?))", "any(editors,eq(displayname,\"a\"))", false),
            ("filter := any(authors|editors,eq(displayname,?))", "any(editors,eq(displayname,\"a\"))", true),
            ("filter := any(authors,any(awards,eq(title,?)))", "any(authors,any(awards,eq(title,\"a\")))", true),
            ("filter := ~title", "any(authors,any(awards,eq(title,\"a\")))", false),
            ("filter := ~authors/awards/title", "any(authors,any(awards,eq(title,\"a\")))", true),
            ("filter := any(authors,~displayname)", "any(authors,eq(lower(displayname),\"a\"))", true),
            ("default allow deny len(title)", "eq(len(lower(title)),2)", true),
            ("default allow deny len(~title)", "eq(len(lower(title)),2)", false),
            ("default allow deny substring(~title,...)", "eq(substr(title,1,2),\"x\")", false),
            ("filter := * deny filter: not(*)", "not(eq(title,\"x\"))", false),
            ("filter := * deny sort: not(*)", "not(eq(title,\"x\"))", true),
            ("filter := * deny any(authors,~title)", "any(authors,eq(title,\"x\"))", false),
            ("filter := * deny ~title", "any(authors,eq(title,\"x\"))", true),
            ("filter := * deny ~authors/title", "any(authors,eq(title,\"x\"))", false),
            ("filter := in(title,\"a\"|\"b\"...)", "in(title,\"a\",\"b\")", true),
            ("filter := in(title,\"a\"|\"b\"...)", "in(title,\"a\",\"c\")", false)
        };
        foreach (var c in cases) yield return new object[] { c.Policy, c.Query, c.Accepted };
    }

    [Theory, MemberData(nameof(Cases))]
    public void MatchesStructureScopesAndConstants(string policy, string query, bool accepted)
    {
        var models = PolicyTestModels.Compile(policy);
        if (accepted) new FilterParser().Parse(query, models.Filter);
        else Assert.Throws<QueryPolicyException>(() => new FilterParser().Parse(query, models.Filter));
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public void ContainsSurvivesWrappers(int count)
    {
        string field = "title";
        for (int i = 0; i < count; i++) field = "lower(" + field + ")";
        var models = PolicyTestModels.Compile("default allow deny len(~title)", new() { MaxDepth = 20 });
        var e = Assert.Throws<QueryPolicyException>(() => new FilterParser().Parse("eq(len(" + field + "),2)", models.Filter));
        Assert.Equal(QueryPolicyViolationKind.DenyRuleMatched, e.Kind);
        Assert.Equal("eq > len", e.Path);
    }

    [Theory]
    [InlineData("any")] [InlineData("all")] [InlineData("none")] [InlineData("count")]
    public void OptionalCollectionPredicates(string function)
    {
        var rule = function + "(authors,...)";
        var models = PolicyTestModels.Compile("filter := " + (function == "count" ? "eq(" + rule + ",?)" : rule));
        foreach (var argument in new[] { "authors", "authors,eq(title,\"x\")" })
        {
            var query = function + "(" + argument + ")";
            if (function == "count") query = "eq(" + query + ",1)";
            new FilterParser().Parse(query, models.Filter);
        }
    }

    [Theory]
    [InlineData("min")] [InlineData("max")] [InlineData("sum")] [InlineData("avg")]
    public void AggregateSelectors(string function)
    {
        var models = PolicyTestModels.Compile("filter := gt(" + function + "(authors,age),?)");
        new FilterParser().Parse("gt(" + function + "(authors,age),10)", models.Filter);
    }
}
