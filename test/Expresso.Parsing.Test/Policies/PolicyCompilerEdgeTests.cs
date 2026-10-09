using System.Globalization;
using Expresso.Core.Filtering;
using Expresso.Core.Policies;
using Expresso.Parsing.Policies;

namespace Expresso.Parsing.Test.Policies;

public class PolicyCompilerEdgeTests
{
    [Fact]
    public void ConstantsShareCustomDateFormatsWithParser()
    {
        var options = new LiteralParseOptions { CultureName = "nl-NL", DateTimeFormats = new[] { "dd-MM-yyyy" } };
        var model = PolicyTestModels.Model();
        var policy = QueryPolicyCompiler.Compile(new() { Rules = "filter := eq(createdat,\"31-12-1899\")" }, model, model, options);
        new FilterParser(options).Parse("eq(createdat,\"31-12-1899\")", policy.Filter);
        Assert.Throws<QueryPolicyCompileException>(() => PolicyTestModels.Compile("filter := eq(createdat,\"not a date\")"));
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void DateOnlyAndTimeOnlyConstantsAreTypedFromFields()
    {
        var model = new QueryModel(new[] { ("day", typeof(DateOnly)), ("clock", typeof(TimeOnly)) });
        var policy = QueryPolicyCompiler.Compile(new() { Rules = "filter := eq(day,\"2026-10-08\") | eq(clock,\"09:00\")" }, model, model);
        new FilterParser().Parse("eq(day,\"2026-10-08\")", policy.Filter);
        new FilterParser().Parse("eq(clock,\"09:00\")", policy.Filter);
    }
#endif

    [Theory]
    [InlineData("filter := title")]
    [InlineData("sort := authors")]
    [InlineData("sort := any(authors)")]
    [InlineData("sort := sortfor(missing,title)")]
    [InlineData("sort := sortfor(title,title)")]
    [InlineData("filter := eq(authors/missing,?)")]
    [InlineData("filter := eq(missing/title,?)")]
    [InlineData("filter := any(authors,$p) $p := eq(price,?)")]
    [InlineData("filter := eq(price,1e9999)")]
    public void InvalidTargetScopeAndConstantAreRejected(string source)
    {
        // Non-finite doubles follow the existing parser, which may admit infinity on
        // modern runtimes. Test the literal factory contract without inventing a rule.
        if (source.Contains("1e9999"))
        {
            bool parserAccepts = Record.Exception(() => new FilterParser().Parse("eq(price,1e9999)", PolicyTestModels.Model())) == null;
            Assert.Equal(parserAccepts, Record.Exception(() => PolicyTestModels.Compile(source)) == null);
        }
        else Assert.Throws<QueryPolicyCompileException>(() => PolicyTestModels.Compile(source));
    }

    [Theory]
    [InlineData("sort := substring(title | year...[3])", "substring(title,year,year),asc")]
    [InlineData("sort := substring(title,year...[2,10])", "substring(title,year,year),asc")]
    [InlineData("filter := ~any(authors/awards,eq(title,?))", "any(authors,any(awards,eq(title,\"x\")))")]
    public void PositionalTailsAndQualifiedCollections(string source, string query)
    {
        var models = PolicyTestModels.Compile(source);
        if (source.StartsWith("sort")) new SortDirectiveParser().Parse(query, models.Sort);
        else new FilterParser().Parse(query, models.Filter);
    }
}
