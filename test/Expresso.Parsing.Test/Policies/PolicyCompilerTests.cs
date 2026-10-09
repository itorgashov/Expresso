using Expresso.Core.Filtering;
using Expresso.Core.Policies;
using Expresso.Parsing.Policies;
using Expresso.Parsing.Policies.Catalog;

namespace Expresso.Parsing.Test.Policies;

public class PolicyCompilerTests
{
    public static IEnumerable<object[]> Functions() => FunctionCatalog.All.SelectMany(f =>
        new[] { f.Names[0], "{" + f.Names[0] + "}", "@" + f.Category, f.Names.Last().ToUpperInvariant() }
            .Select(name => new object[] { f.NodeType.Name, name }));

    [Theory, MemberData(nameof(Functions))]
    public void CompilesEveryFunction(string type, string name)
    {
        var f = FunctionCatalog.All.Single(x => x.NodeType.Name == type);
        var query = FunctionCatalogTests.Query(f, f.Minimum);
        var call = name + query.Substring(query.IndexOf('('));
        string head = f.Signatures.All(s => s.Result == TypeKind.Boolean) ? "filter" : "sort";
        var model = FunctionCatalogTests.Model();
        var result = QueryPolicyCompiler.Compile(new() { Rules = head + " := " + call }, model, model);
        Assert.Same(result.Filter.Policy, result.Sort.Policy);
    }

    [Theory]
    [InlineData("filter := eq(missing,?)", "S1")]
    [InlineData("filter := unknown(title,?)", "S2")]
    [InlineData("filter := eq(unknown(title),?)", "S2")]
    [InlineData("filter := @missing(title,?)", "S2")]
    [InlineData("filter := eq(title)", "S3")]
    [InlineData("filter := eq(len(age),?)", "S4")]
    [InlineData("filter := gt(title,?)", "S4")]
    [InlineData("filter := eq(year,10.5)", "S4")]
    [InlineData("filter := {eq,len}(title,?)", "S3")]
    [InlineData("filter := $missing", "S5")]
    [InlineData("filter := * filter := *", "S5")]
    [InlineData("$p |= * filter := $p", "S5")]
    [InlineData("filter := $a $a := not($a)", "S6")]
    [InlineData("filter := $a $a := $b $b := $a", "S6")]
    [InlineData("filter := sortfor(authors,title)", "S7")]
    [InlineData("sort := len(sortfor(authors,title))", "S7")]
    [InlineData("sort := ~sortfor(authors,title)", "S7")]
    [InlineData("default allow filter := *", "S8")]
    [InlineData("default allow default deny", "S8")]
    [InlineData("filter := @arithmetic(title)", "S9")]
    [InlineData("filter := and(*...[0,0])", "S10")]
    [InlineData("filter := not(*...[2])", "S10")]
    [InlineData("sort := substring(title...[3])", "S4")]
    [InlineData("filter := any(authors|missing,eq(title,?))", "S1")]
    [InlineData("sort := sortfor(authors,sortfor(awards,title))", "S7")]
    public void RejectsInvalidPolicies(string source, string code)
    {
        var e = Assert.Throws<QueryPolicyCompileException>(() => PolicyTestModels.Compile(source));
        Assert.Contains(e.Diagnostics, d => d.Code == code);
        Assert.All(e.Diagnostics, d => { Assert.True(d.Line > 0); Assert.True(d.Column > 0); });
    }

    [Theory]
    [InlineData("filter := eq(price,10)")]
    [InlineData("filter := eq(year,-10)")]
    [InlineData("filter := eq(createdat,\"2026-10-08\")")]
    [InlineData("filter := eq(id,\"11111111-1111-1111-1111-111111111111\")")]
    [InlineData("filter := eq(clock,\"12:30:00\")")]
    [InlineData("filter := eq(year | price,?)")]
    [InlineData("filter := eq(min(year,1),?) | eq(min(authors,age),?)")]
    [InlineData("filter := $p $p := eq(title,?) | and($p...) | not($p)")]
    [InlineData("filter := $a $a := $b $b := eq(title,?) | not($a)")]
    [InlineData("filter := $p $p := eq(title,?) $p |= eq(year,?)")]
    [InlineData("filter := any(authors,any(awards,eq(title,?)))")]
    [InlineData("filter := any(authors,$p) $p := eq(title,?) | any(awards,eq(title,?))")]
    [InlineData("filter := any(authors | editors, eq(displayname,?))")]
    [InlineData("filter := eq(sort | deny | allow | default | filter,?)")]
    [InlineData("filter := and(eq(title,?)...[1,])")]
    [InlineData("default allow default filter: deny filter := eq(title,?)")]
    [InlineData("sort := $s $s := title | sortfor(authors,lastname) | sortfor(authors/awards,title)")]
    [InlineData("default allow deny filter: len(~title)")]
    public void CompilesRulesScopesAndConstants(string source) => Assert.NotNull(PolicyTestModels.Compile(source).Filter.Policy);

    [Fact]
    public void SideSpecificCatalogsAndUntargetedDeny()
    {
        var filter = PolicyTestModels.Model(); var sort = new QueryModel(new[] { ("year", typeof(int)) });
        var result = QueryPolicyCompiler.Compile(new() { Rules = "default allow deny len(title)" }, filter, sort);
        Assert.Same(result.Filter.Policy, result.Sort.Policy);
        Assert.Throws<QueryPolicyCompileException>(() => QueryPolicyCompiler.Compile(new() { Rules = "sort := title" }, filter, sort));
        Assert.Throws<QueryPolicyCompileException>(() => QueryPolicyCompiler.Compile(new() { Rules = "deny len(missing)" }, filter, sort));
    }

    [Fact]
    public void TargetedDefaultsStartsAndDeniesStayWithTheirCatalogs()
    {
        // A filter default and targeted deny use the filter catalog; sort rules and denies use a separate sort catalog.
        var filter = new QueryModel(new[] { ("title", typeof(string)) });
        var sort = new QueryModel(new[] { ("year", typeof(int)), ("price", typeof(decimal)) });
        var models = QueryPolicyCompiler.Compile(new()
        {
            Rules = "default filter: allow sort := year | price deny filter: eq(title,?) deny sort: year"
        }, filter, sort);

        new FilterParser().Parse("eq(\"x\",\"x\")", models.Filter);
        Assert.Equal(QueryPolicyViolationKind.DenyRuleMatched,
            Assert.Throws<QueryPolicyException>(() => new FilterParser().Parse("eq(title,\"x\")", models.Filter)).Kind);
        new SortDirectiveParser().Parse("price,asc", models.Sort);
        Assert.Equal(QueryPolicyViolationKind.DenyRuleMatched,
            Assert.Throws<QueryPolicyException>(() => new SortDirectiveParser().Parse("year,asc", models.Sort)).Kind);
    }

    [Fact]
    public void WarningsAndMultipleErrors()
    {
        var result = PolicyTestModels.Compile("$unused := eq(title,?) filter := {eq,EQ}(title,?) | in(title,?...[200,300])");
        Assert.Equal(new[] { "W1", "W2", "W3", "W4" }, result.Warnings.Select(d => d.Code).Distinct().OrderBy(x => x));
        var e = Assert.Throws<QueryPolicyCompileException>(() => PolicyTestModels.Compile("filter := eq(missing,?)\nsort := lower(unknown)"));
        Assert.Contains(e.Diagnostics, d => d.Line == 1);
        Assert.Contains(e.Diagnostics, d => d.Line == 2);
    }

    public static IEnumerable<object[]> Limits() => typeof(QueryPolicyLimits).GetProperties()
        .SelectMany(p => new[] { -1, 0, 1, int.MaxValue }.Select(v => new object[] { p.Name, v }));

    [Theory, MemberData(nameof(Limits))]
    public void ValidatesEveryLimit(string name, int value)
    {
        var limits = new QueryPolicyLimits(); typeof(QueryPolicyLimits).GetProperty(name)!.SetValue(limits, value);
        if (value < 1 || name == "MaxDepth" && value > 100)
            Assert.Contains(Assert.Throws<QueryPolicyCompileException>(() => PolicyTestModels.Compile("default allow", limits)).Diagnostics, d => d.Code == "S11");
        else Assert.NotNull(PolicyTestModels.Compile("default allow", limits));
    }

    [Fact]
    public void NullInputsAndDepthCeiling()
    {
        Assert.Throws<QueryPolicyCompileException>(() => QueryPolicyCompiler.Compile(null!, null!, null!));
        Assert.Throws<QueryPolicyCompileException>(() => QueryPolicyCompiler.Compile(new() { Rules = null! }, QueryModel.Empty, QueryModel.Empty));
        Assert.Throws<QueryPolicyCompileException>(() => QueryPolicyCompiler.Compile(new() { Limits = null! }, QueryModel.Empty, QueryModel.Empty));
        Assert.NotNull(PolicyTestModels.Compile("default allow", new() { MaxDepth = 100 }));
        Assert.Throws<QueryPolicyCompileException>(() => PolicyTestModels.Compile("default allow", new() { MaxDepth = 101 }));
    }
}
