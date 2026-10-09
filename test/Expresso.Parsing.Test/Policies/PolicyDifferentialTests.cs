using Expresso.Core.Policies;
using Expresso.Parsing.Policies;
using Expresso.Parsing.Policies.Runtime;

namespace Expresso.Parsing.Test.Policies;

public class PolicyDifferentialTests
{
    [Theory]
    [InlineData(17)] [InlineData(241)] [InlineData(9001)]
    public void RandomPolicySyntaxAgreesWithParsedTreesAndSortKeys(int seed)
    {
        // Random allow/deny policies are interpreted from syntax against real parsed trees.
        // This checks field scopes, typed constants, tails, and nested sort binding without using bound patterns.
        var random = new Random(seed);
        var model = PolicyTestModels.Model();
        var filters = new[] { "eq(title,?)", "gt(year,?)", "any(authors,eq(displayname,?))",
            "in(title,?...[1,3])", "eq(title,\"War\")", "and(eq(title,?),gt(year,?))", "eq(len(title),?)" };
        var sorts = new[] { "title", "year", "sortfor(authors,lastname | firstname)",
            "sortfor(authors/awards,title)" };
        var deny = new[] { "", "deny filter: eq(title,\"War\")", "deny filter: gt(year,?)",
            "deny sort: year", "deny sort: authors/awards/title" };
        var filterQueries = new[] { "eq(title,\"War\")", "eq(title,\"Peace\")", "gt(year,2000)",
            "lt(year,2000)", "any(authors,eq(displayname,\"Leo\"))", "in(title,\"a\",\"b\")",
            "and(eq(title,\"War\"),gt(year,2000))", "eq(len(title),3)" };
        var sortQueries = new[] { "title,asc", "year,desc", "sortfor(authors,lastname),asc",
            "sortfor(authors,firstname),desc", "sortfor(authors/awards,title),asc", "title,asc,year,desc" };
        var filterParser = new FilterParser();
        var sortParser = new SortDirectiveParser();

        for (int sample = 0; sample < 60; sample++)
        {
            string source = "filter := " + string.Join(" | ", filters.OrderBy(_ => random.Next()).Take(random.Next(1, 5))) +
                "\nsort := " + string.Join(" | ", sorts.OrderBy(_ => random.Next()).Take(random.Next(1, 5))) +
                "\n" + deny[random.Next(deny.Length)];
            var oracle = new AstPolicyMatcher(source, model);
            var compiled = QueryPolicyCompiler.Compile(new() { Rules = source }, model, model);
            foreach (string query in filterQueries)
            {
                var tree = filterParser.Parse(query, model).Expression!;
                bool expected = oracle.AllowsFilter(tree);
                var error = Record.Exception(() => filterParser.Parse(query, compiled.Filter));
                Assert.True(expected == (error == null), $"seed={seed}; policy={source}; filter={query}; expected={expected}; error={error}");
            }
            foreach (string query in sortQueries)
            {
                var directive = sortParser.Parse(query, model);
                bool expected = oracle.AllowsSort(directive);
                var error = Record.Exception(() => sortParser.Parse(query, compiled.Sort));
                Assert.True(expected == (error == null), $"seed={seed}; policy={source}; sort={query}; expected={expected}; error={error}");
            }
        }
    }

    [Theory]
    [InlineData(741)] [InlineData(42)] [InlineData(129)] [InlineData(2026)] [InlineData(833)]
    public void SeededPoliciesAndTreesAgreeWithIndependentInterpreter(int seed)
    {
        // Scenario: compare the bitset evaluator with a top-down oracle; randomly
        // vary bounds, rules, denies, and trees; every decision must agree.
        var random = new Random(seed);
        var limits = new QueryPolicyLimits { MaxDepth = 12, MaxNodes = 100, MaxArgs = 3, MaxInItems = 3 };
        for (int p = 0; p < 20; p++)
        {
            string source = (p % 4) switch
            {
                0 => "default allow",
                1 => "filter := $p | and($p...[2," + random.Next(2, 7) + "]) $p := eq(title,?) | in(year,?...[1,6]) | any(authors,eq(title,?))",
                2 => "filter := $p $p := eq(title,?) | and($p...) | not($p) | or(...,eq(year,?),...)",
                _ => "filter := ~title | any(authors,~title)"
            };
            if (random.Next(2) == 0) source += random.Next(2) == 0 ? " deny not(*)" : " deny len(~title)";
            var models = PolicyTestModels.Compile(source, limits);
            var policy = (CompiledQueryPolicy)models.Filter.Policy!;
            for (int t = 0; t < 100; t++)
            {
                string query = Tree(random, 0);
                var tree = new FilterParser().Parse(query, PolicyTestModels.Model()).Expression!;
                var expected = new ReferencePolicyMatcher(limits).Evaluate(tree, policy);
                QueryPolicyViolationKind? actual = null;
                try { new FilterParser().Parse(query, models.Filter); }
                catch (QueryPolicyException e) { actual = e.Kind; }
                Assert.True(expected == actual, $"seed={seed}; policy={source}; tree={query}; expected={expected}; actual={actual}");
            }
        }
    }

    internal static string Tree(Random r, int depth)
    {
        int choice = r.Next(depth >= 3 ? 5 : 9);
        return choice switch
        {
            0 => "eq(title,\"" + (r.Next(2) == 0 ? "a" : "b") + "\")",
            1 => "eq(year," + r.Next(5) + ")",
            2 => "in(year," + string.Join(",", Enumerable.Range(0, r.Next(1, 7))) + ")",
            3 => "eq(len(lower(title)),1)",
            4 => "any(authors,eq(title,\"a\"))",
            5 => "not(" + Tree(r, depth + 1) + ")",
            _ => (choice == 6 ? "and(" : "or(") + string.Join(",", Enumerable.Range(0, r.Next(2, 5)).Select(_ => Tree(r, depth + 1))) + ")"
        };
    }
}
