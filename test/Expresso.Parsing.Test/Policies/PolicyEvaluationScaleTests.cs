using System.Diagnostics;
using Expresso.Core.Policies;

namespace Expresso.Parsing.Test.Policies;

public class PolicyEvaluationScaleTests
{
    [Fact]
    public void LargeEnumerationKeepsRepeatedEvaluationBounded()
    {
        // A server policy lists 1,500 allowed constants; repeated multi-node requests
        // should not scan the entire enumeration at every expression node.
        string rules = "filter := * | " + string.Join(" | ",
            Enumerable.Range(0, 1500).Select(value => "eq(year," + value + ")"));
        var limits = new QueryPolicyLimits { MaxArgs = 40, MaxNodes = 100 };
        var models = PolicyTestModels.Compile(rules, limits);
        string query = "and(" + string.Join(",", Enumerable.Repeat("eq(year,1)", 20)) + ")";
        var parser = new FilterParser();
        parser.Parse(query, models.Filter);

        var timer = Stopwatch.StartNew();
        for (int i = 0; i < 200; i++) parser.Parse(query, models.Filter);
        timer.Stop();
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(5), $"200 requests took {timer.Elapsed}.");
    }
}
