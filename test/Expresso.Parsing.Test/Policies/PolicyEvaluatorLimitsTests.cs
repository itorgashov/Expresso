using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Policies;
using Expresso.Parsing.Policies.Runtime;

namespace Expresso.Parsing.Test.Policies;

public class PolicyEvaluatorLimitsTests
{
    public static IEnumerable<object[]> Boundaries()
    {
        foreach (var function in new[] { "and", "or", "concat", "in" })
            foreach (var bounds in new[] { "", "[2,4]", "[3]", "[,3]", "[2,]" })
                foreach (var count in new[] { 2, 3, 4, 5 })
                    yield return new object[] { function, bounds, count };
    }

    [Theory, MemberData(nameof(Boundaries))]
    public void RepetitionBoundsOverrideOnlyTheLocalDefault(string function, string bounds, int count)
    {
        // A three-item default is replaced only by the stated tail bounds; each generated query is accepted exactly within them.
        bool literal = function is "in" or "concat";
        string tail = literal ? "?" : "eq(year,?)";
        string pattern = function + "(" + (function == "in" ? "title," : "") + tail + "..." + bounds + ")";
        string expression = function + "(" + (function == "in" ? "title," : "") +
            string.Join(",", Enumerable.Repeat(literal ? "\"x\"" : "eq(year,1)", count)) + ")";
        if (function == "concat") { pattern = "eq(" + pattern + ",?)"; expression = "eq(" + expression + ",\"x\")"; }
        var models = PolicyTestModels.Compile("filter := " + pattern, new() { MaxArgs = 3, MaxInItems = 3 });
        bool allowed = bounds == "[2,4]" ? count <= 4 : bounds == "[3]" ? count == 3 : count <= 3;
        if (allowed) new FilterParser().Parse(expression, models.Filter);
        else Assert.Throws<QueryPolicyException>(() => new FilterParser().Parse(expression, models.Filter));
    }

    [Theory]
    [InlineData("default allow")]
    [InlineData("filter := *")]
    [InlineData("filter := and(...)")]
    [InlineData("filter := and(...,eq(year,?),...)")]
    public void WildcardsAndExistsRespectDefaultCounts(string source)
    {
        var models = PolicyTestModels.Compile(source, new() { MaxArgs = 2 });
        new FilterParser().Parse("and(eq(year,1),eq(year,2))", models.Filter);
        Assert.Throws<QueryPolicyException>(() => new FilterParser().Parse("and(eq(year,1),eq(year,2),eq(year,3))", models.Filter));
    }

    [Fact]
    public void DenyCannotBeBypassedByAnExplicitlyRelaxedList()
    {
        // Scenario: an explicit allow admits a large list; the deny wildcard must
        // ignore default repetition limits and still reject the surrounding NOT.
        var models = PolicyTestModels.Compile("filter := not(in(year,?...[1,10])) deny not(*)", new() { MaxInItems = 2 });
        var e = Assert.Throws<QueryPolicyException>(() => new FilterParser().Parse("not(in(year,1,2,3,4,5))", models.Filter));
        Assert.Equal(QueryPolicyViolationKind.DenyRuleMatched, e.Kind);
    }

    [Theory]
    [InlineData("MaxDepth", 2)] [InlineData("MaxDepth", 3)] [InlineData("MaxDepth", 4)]
    [InlineData("MaxNodes", 3)] [InlineData("MaxNodes", 4)] [InlineData("MaxNodes", 5)]
    [InlineData("MaxStringLength", 2)] [InlineData("MaxStringLength", 3)] [InlineData("MaxStringLength", 4)]
    public void TreeLimitsApplyToFiltersAndEachSortKey(string name, int limit)
    {
        // One filter tree and two separate sort-key trees have the same depth, node count, and literal length thresholds.
        var limits = new QueryPolicyLimits(); typeof(QueryPolicyLimits).GetProperty(name)!.SetValue(limits, limit);
        var models = PolicyTestModels.Compile("default allow", limits);
        const string query = "eq(lower(title),\"abc\")"; // depth 3, nodes 4, string length 3
        int required = name == "MaxNodes" ? 4 : 3;
        foreach (bool sort in new[] { false, true })
        {
            Action parse = sort ? () => new SortDirectiveParser().Parse(query + ",asc," + query + ",desc", models.Sort) :
                () => new FilterParser().Parse(query, models.Filter);
            if (limit >= required) parse();
            else
            {
                var e = Assert.Throws<QueryPolicyException>(parse);
                Assert.Equal(QueryPolicyViolationKind.LimitExceeded, e.Kind);
                Assert.Equal(name, e.LimitName);
            }
        }
    }

    [Fact]
    public void StringLimitCountsUtf16AndPrecedesDeny()
    {
        var models = PolicyTestModels.Compile("default allow deny eq(...)", new() { MaxStringLength = 1 });
        var e = Assert.Throws<QueryPolicyException>(() => new FilterParser().Parse("eq(title,\"\ud83d\ude00\")", models.Filter));
        Assert.Equal("MaxStringLength", e.LimitName);
    }

    [Fact]
    public void HostileDepthIsCheckedIteratively()
    {
        var models = PolicyTestModels.Compile("default allow", new() { MaxDepth = 100, MaxNodes = int.MaxValue });
        AbstractExpression node = new EqFunc(new Literal(1), new Literal(1));
        for (int i = 0; i < 10000; i++) node = new NotFunc(node);
        Assert.Equal("MaxDepth", Assert.Throws<QueryPolicyException>(() => PolicyEnforcer.EnforceFilter(models.Filter, node)).LimitName);
    }

    [Theory]
    [InlineData("in(year,1,2,3)", "MaxInItems")]
    [InlineData("and(eq(year,1),eq(year,2),eq(year,3))", "MaxArgs")]
    public void DefaultModeReportsCountLimit(string query, string limit)
    {
        var models = PolicyTestModels.Compile("default allow", new() { MaxArgs = 2, MaxInItems = 2 });
        Assert.Equal(limit, Assert.Throws<QueryPolicyException>(() => new FilterParser().Parse(query, models.Filter)).LimitName);
    }
}
