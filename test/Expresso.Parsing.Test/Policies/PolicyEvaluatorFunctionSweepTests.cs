using Expresso.Core.Policies;
using Expresso.Parsing.Policies;
using Expresso.Parsing.Policies.Catalog;

namespace Expresso.Parsing.Test.Policies;

public class PolicyEvaluatorFunctionSweepTests
{
    public enum SweepCase
    {
        Exact, Set, Category, Alias, CaseInsensitive, BareTail, Wildcard,
        DefaultAllow, DefaultDeny, DenyName, AllowWildcardDenyAlias, DenyOnly, TargetedDeny
    }

    public static IEnumerable<object[]> Cases() => FunctionCatalog.All.SelectMany(f =>
        Enum.GetValues(typeof(SweepCase)).Cast<SweepCase>().Select(scenario => new object[] { f.NodeType.Name, scenario }));

    [Theory, MemberData(nameof(Cases))]
    public void EveryFunctionObeysAllowAndDeny(string type, SweepCase scenario)
    {
        // Each catalogued function is parsed under every allow and deny form; the decision must match the form.
        var f = FunctionCatalog.All.Single(x => x.NodeType.Name == type);
        var query = FunctionCatalogTests.Query(f, f.Minimum);
        var head = f.Signatures.All(s => s.Result == TypeKind.Boolean) ? "filter" : "sort";
        var pattern = scenario switch
        {
            SweepCase.Set => "{" + f.Names[0] + "}" + query.Substring(query.IndexOf('(')),
            SweepCase.Category => "@" + f.Category + query.Substring(query.IndexOf('(')),
            SweepCase.Alias => f.Names.Last() + query.Substring(query.IndexOf('(')),
            SweepCase.CaseInsensitive => query.ToUpperInvariant(),
            SweepCase.BareTail => f.Names[0] + "(...)",
            SweepCase.Wildcard => "*",
            _ => query
        };
        string source = scenario switch
        {
            <= SweepCase.Wildcard => head + " := " + pattern,
            SweepCase.DefaultAllow => "default allow",
            SweepCase.DefaultDeny => "",
            SweepCase.DenyName => "default allow deny " + f.Names[0] + "(...)",
            SweepCase.AllowWildcardDenyAlias => head + " := * deny " + f.Names.Last().ToUpperInvariant() + "(...)",
            SweepCase.DenyOnly => "deny " + f.Names[0] + "(...)",
            _ => "default allow deny " + head + ": " + f.Names[0] + "(...)"
        };
        var model = FunctionCatalogTests.Model();
        var result = QueryPolicyCompiler.Compile(new() { Rules = source }, model, model);
        Action parse = head == "filter" ? () => new FilterParser().Parse(query, result.Filter) :
            () => new SortDirectiveParser().Parse(query + ",asc", result.Sort);
        if (scenario < SweepCase.DefaultDeny) parse();
        else
        {
            var e = Assert.Throws<QueryPolicyException>(parse);
            Assert.Equal(scenario == SweepCase.DefaultDeny ? QueryPolicyViolationKind.NotAllowed : QueryPolicyViolationKind.DenyRuleMatched, e.Kind);
        }
    }
}
