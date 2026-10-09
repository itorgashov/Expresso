using System.Diagnostics;
using Expresso.Core.Policies;

namespace Expresso.Parsing.Test.Policies;

public class PolicyRobustnessTests
{
    [Theory]
    [InlineData(171)] [InlineData(414)] [InlineData(933)] [InlineData(1234)]
    public void CompilerFuzzHasOnlyPolicyDiagnostics(int seed)
    {
        var random = new Random(seed); var watch = Stopwatch.StartNew();
        var seeds = new[] { "filter := eq(title,?)", "filter := $p $p := eq(title,?) | and($p...[1,5])",
            "default allow deny @string-transform(~title,...)", "sort := title | sortfor(authors,lastname)" };
        const string alphabet = "$@~*?{}[]()|:=;,#/ -0123456789abcdef\"\n\r\t";
        for (int i = 0; i < 500; i++)
        {
            string source = seeds[random.Next(seeds.Length)];
            if (i % 3 == 0) source = new string(Enumerable.Range(0, random.Next(100)).Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray());
            else for (int mutation = 0; mutation < 3; mutation++)
            {
                int at = random.Next(source.Length + 1);
                source = at < source.Length && random.Next(2) == 0 ? source.Remove(at, 1) : source.Insert(at, alphabet[random.Next(alphabet.Length)].ToString());
            }
            var error = Record.Exception(() => PolicyTestModels.Compile(source));
            Assert.True(error == null || error is QueryPolicyCompileException, $"seed={seed}, source={source}, error={error}");
        }
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(30), watch.Elapsed.ToString());
    }

    [Fact]
    public void OnePolicyIsSafeAcrossConcurrentParsers()
    {
        var models = PolicyTestModels.Compile("filter := eq(title,?) | and(eq(title,?)...) deny len(~title)");
        var queries = new[] { "eq(title,\"a\")", "eq(len(title),1)", "or(eq(title,\"a\"),eq(title,\"b\"))", "and(eq(title,\"a\"),eq(title,\"b\"))" };
        var parser = new FilterParser();
        string Outcome(string query)
        {
            try { parser.Parse(query, models.Filter); return "accepted"; }
            catch (QueryPolicyException e) { return e.Kind + ":" + e.Path; }
        }
        var expected = queries.Select(Outcome).ToArray();
        Parallel.For(0, 1000, i => Assert.Equal(expected[i % 4], Outcome(queries[i % 4])));
    }

    [Theory]
    [InlineData(200, false)] [InlineData(200, true)] [InlineData(1000, false)]
    public void LongRuleChainsAndFanoutDoNotExpandExponentially(int count, bool fanout)
    {
        var source = "filter := $p0\n" + string.Join("\n", Enumerable.Range(0, count).Select(i =>
            "$p" + i + " := " + (i == count - 1 ? "eq(title,?)" : "$p" + (i + 1) + (fanout ? " | $p" + (i + 1) : ""))));
        var watch = Stopwatch.StartNew();
        var models = PolicyTestModels.Compile(source);
        new FilterParser().Parse("eq(title,\"x\")", models.Filter);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(30), watch.Elapsed.ToString());
    }

    [Fact]
    public void WideAlternationsAndLongNamesAreBounded()
    {
        var watch = Stopwatch.StartNew();
        var models = PolicyTestModels.Compile("filter := " + string.Join(" | ", Enumerable.Repeat("eq(title,?)", 5000)));
        new FilterParser().Parse("eq(title,\"x\")", models.Filter);
        Assert.Throws<QueryPolicyCompileException>(() => PolicyTestModels.Compile("filter := " + new string('a', 10000)));
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(30), watch.Elapsed.ToString());
    }
}
