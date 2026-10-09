using System.Text.RegularExpressions;
using Expresso.Core.Policies;

namespace Expresso.Parsing.Test.Policies;

public class DocsConformanceTests
{
    [Fact]
    public void PolicyExamplesAndExpectedDecisionsStayInSync()
    {
        string document = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Docs", "query-policy.md"));
        var blocks = Regex.Matches(document, @"```policy\r?\n(.*?)```\s*(?<intro>[^\r\n`]+)\r?\n\s*```cases\r?\n(.*?)```", RegexOptions.Singleline);
        Assert.Equal(3, blocks.Count);
        foreach (Match block in blocks)
        {
            Assert.Contains("client", block.Groups["intro"].Value);
            var models = PolicyTestModels.Compile(block.Groups[1].Value);
            foreach (string line in block.Groups[2].Value.Split('\n').Select(s => s.Trim()).Where(s => s.Length != 0))
            {
                var entry = Regex.Match(line, @"^(accept|reject) (filter|sort): (.+)$");
                Assert.True(entry.Success, line);
                string query = entry.Groups[3].Value;
                Action parse = entry.Groups[2].Value == "filter" ? () => new FilterParser().Parse(query, models.Filter) :
                    () => new SortDirectiveParser().Parse(query, models.Sort);
                if (entry.Groups[1].Value == "accept") parse();
                else Assert.Throws<QueryPolicyException>(parse);
            }
        }
    }
}
