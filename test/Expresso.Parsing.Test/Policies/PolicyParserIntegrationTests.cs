using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Filtering;
using Expresso.Core.Policies;
using Expresso.Parsing.Policies;
using Expresso.Parsing.Policies.Catalog;

namespace Expresso.Parsing.Test.Policies;

public class PolicyParserIntegrationTests
{
    public static IEnumerable<object[]> Corpus() => FunctionCatalog.All.SelectMany(f =>
        new[] { f.Minimum, Math.Min(4, f.Maximum) }.Distinct().Select(n => new object[] { f.NodeType.Name, n }));

    [Theory, MemberData(nameof(Corpus))]
    public void NoPolicyAndDefaultAllowProduceIdenticalTrees(string name, int count)
    {
        var function = FunctionCatalog.All.Single(f => f.NodeType.Name == name);
        var query = FunctionCatalogTests.Query(function, count);
        var model = FunctionCatalogTests.Model();
        var policies = QueryPolicyCompiler.Compile(new() { Rules = "default allow" }, model, model);
        if (function.Signatures.All(s => s.Result == TypeKind.Boolean))
            Assert.Equal(new FilterParser().Parse(query, model).Expression, new FilterParser().Parse(query, policies.Filter).Expression);
        if (!function.IsQuantifier)
        {
            var plain = new SortDirectiveParser().Parse(query + ",asc", model);
            var guarded = new SortDirectiveParser().Parse(query + ",asc", policies.Sort);
            Assert.Equal(plain.Items[0].Expression, guarded.Items[0].Expression);
            Assert.Equal(plain.Items[0].Direction, guarded.Items[0].Direction);
        }
    }

    [Fact]
    public void MismatchedCatalogAndForeignHandleAreProgrammingErrors()
    {
        var models = PolicyTestModels.Compile("default allow");
        var other = new QueryModel(new[] { ("title", typeof(string)) }).WithPolicy(models.Filter.Policy);
        Assert.Throws<InvalidOperationException>(() => new FilterParser().Parse("eq(title,\"x\")", other));
        Assert.Throws<InvalidOperationException>(() => new SortDirectiveParser().Parse("title,asc", other));
        Assert.Throws<InvalidOperationException>(() => new FilterParser().Parse("eq(title,\"x\")", PolicyTestModels.Model().WithPolicy(new ForeignPolicy())));
    }
    private sealed class ForeignPolicy : QueryPolicy { }

    [Fact]
    public void ArrayOverloadAndNestedModelIgnorePolicies()
    {
        var denied = PolicyTestModels.Compile("");
        new FilterParser().Parse("eq(title,\"x\")", denied.Filter.Fields);
        var root = new QueryModel(null, new[] { new CollectionModel("items", denied.Filter) });
        new FilterParser().Parse("any(items,eq(title,\"x\"))", root);
    }

    [Fact]
    public void ParseErrorsPrecedeEnforcement()
    {
        var denied = PolicyTestModels.Compile("");
        Assert.IsNotType<QueryPolicyException>(Record.Exception(() => new FilterParser().Parse("eq(missing,1)", denied.Filter)));
        Assert.IsNotType<QueryPolicyException>(Record.Exception(() => new FilterParser().Parse("title", denied.Filter)));
        Assert.IsNotType<QueryPolicyException>(Record.Exception(() => new SortDirectiveParser().Parse("title,nonsense", denied.Sort)));
    }

    [Theory]
    [InlineData(QueryPolicyErrorDetail.Generic)]
    [InlineData(QueryPolicyErrorDetail.Detailed)]
    public void MessagesHideRulesByDefault(QueryPolicyErrorDetail detail)
    {
        var models = PolicyTestModels.Compile("default allow deny len(~title)", detail: detail);
        var e = Assert.Throws<QueryPolicyException>(() => new FilterParser().Parse("eq(len(title),1)", models.Filter));
        Assert.IsAssignableFrom<ArgumentException>(e);
        Assert.Equal("deny len(~title)", e.RuleText);
        Assert.Equal("eq > len", e.Path);
        Assert.Equal(detail == QueryPolicyErrorDetail.Detailed, e.Message.Contains("title"));
    }

    [Fact]
    public void DenyRuleTextExcludesCommentsEvenInDetailedErrors()
    {
        // A policy author places an internal note after a deny. Only rule tokens may reach client-visible detail.
        var models = PolicyTestModels.Compile("default allow\ndeny len(~title) # internal note: title probing\ndeny not(*)",
            detail: QueryPolicyErrorDetail.Detailed);
        var error = Assert.Throws<QueryPolicyException>(() => new FilterParser().Parse("eq(len(title),1)", models.Filter));
        Assert.Equal("deny len(~title)", error.RuleText);
        Assert.DoesNotContain("internal note", error.Message);
    }

    [Theory]
    [InlineData("eq(title,publisher)", "eq > publisher")]
    [InlineData("eq(len(title),1)", "eq > len")]
    [InlineData("or(eq(title,\"a\"),eq(title,\"b\"))", "or")]
    public void FailureLocationIsDeterministic(string query, string path)
    {
        var models = PolicyTestModels.Compile("filter := eq(title,?)");
        for (int i = 0; i < 5; i++) Assert.Equal(path,
            Assert.Throws<QueryPolicyException>(() => new FilterParser().Parse(query, models.Filter)).Path);
    }

    [Fact]
    public void CompiledPolicySnapshotsMutableOptions()
    {
        var definition = new QueryPolicyDefinition { Rules = "default allow" };
        var model = PolicyTestModels.Model(); var compiled = QueryPolicyCompiler.Compile(definition, model, model);
        definition.Rules = ""; definition.Limits.MaxNodes = 1; definition.ErrorDetail = QueryPolicyErrorDetail.Detailed;
        new FilterParser().Parse("eq(title,\"x\")", compiled.Filter);
    }
}
