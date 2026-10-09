using Expresso.Core.CriteriaExpressions;
using Expresso.Core.Policies;

namespace Expresso.Parsing.Test.Policies;

public class PolicyEvaluatorSortTests
{
    private const string Rules = "sort := title | year | sortfor(authors,lastname) | sortfor(authors,firstname) | sortfor(authors/awards,title|year)";

    public static IEnumerable<object[]> Counts()
    {
        foreach (var limit in new[] { 1, 2, 3, 5 })
            foreach (var count in Enumerable.Range(Math.Max(1, limit - 1), limit == 1 ? 2 : 3))
                foreach (var mode in new[] { "default allow", Rules, Rules + " deny sort: ~title" })
                    foreach (var keys in new[] { "title,asc", "sortfor(authors,lastname),desc", "sortfor(authors/awards,year),asc" })
                        yield return new object[] { limit, count, mode, keys };
    }
    [Theory, MemberData(nameof(Counts))]
    public void RawSortCountIncludesDuplicatesAndNestedKeys(int limit, int count, string rules, string key)
    {
        var models = PolicyTestModels.Compile(rules, new() { MaxSortKeys = limit });
        var query = string.Join(",", Enumerable.Repeat(key, count));
        if (count > limit)
        {
            var e = Assert.Throws<QueryPolicyException>(() => new SortDirectiveParser().Parse(query, models.Sort));
            Assert.Equal(QueryPolicyViolationKind.LimitExceeded, e.Kind);
            Assert.Equal(QueryPolicyTarget.Sort, e.Target);
            Assert.Equal("MaxSortKeys", e.LimitName);
        }
        else if (rules.Contains("deny") && key == "title,asc")
            Assert.Equal(QueryPolicyViolationKind.DenyRuleMatched, Assert.Throws<QueryPolicyException>(() => new SortDirectiveParser().Parse(query, models.Sort)).Kind);
        else Assert.Equal(count, new SortDirectiveParser().Parse(query, models.Sort).TotalSortKeyCount());
    }

    [Theory]
    [InlineData("year,desc,sortfor(authors,lastname),asc,sortfor(authors/awards,year),desc", true)]
    [InlineData("sortfor(authors,firstname),asc", true)]
    [InlineData("sortfor(authors,title),asc", false)]
    [InlineData("sortfor(editors,lastname),asc", false)]
    [InlineData("price,asc", false)]
    public void PathAlternativesAreExact(string query, bool accepted)
    {
        var models = PolicyTestModels.Compile(Rules, new() { MaxSortKeys = 3 });
        if (accepted) new SortDirectiveParser().Parse(query, models.Sort);
        else Assert.Throws<QueryPolicyException>(() => new SortDirectiveParser().Parse(query, models.Sort));
    }

    [Fact]
    public void MixedCountPrecedesNotAllowedAndHostChangesAreLater()
    {
        const string query = "year,desc,sortfor(authors,lastname),asc,sortfor(authors/awards,year),desc";
        var limited = PolicyTestModels.Compile("sort := title", new() { MaxSortKeys = 2 });
        Assert.Equal("MaxSortKeys", Assert.Throws<QueryPolicyException>(() => new SortDirectiveParser().Parse(query, limited.Sort)).LimitName);
        var models = PolicyTestModels.Compile("sort := title", new() { MaxSortKeys = 1 });
        var sort = new SortDirectiveParser().Parse("title,asc", models.Sort).RemoveDuplicates().ThenBy(new Field("id", typeof(Guid)));
        Assert.Equal(2, sort.TotalSortKeyCount());
        Assert.Equal(1, new SortDirectiveParser().Parse("title,asc,title,desc", PolicyTestModels.Model()).RemoveDuplicates().TotalSortKeyCount());
    }

    [Fact]
    public void FilterOnlyPoliciesRejectSortAndSortLimitNeverAffectsFilter()
    {
        var models = PolicyTestModels.Compile("filter := *", new() { MaxSortKeys = 1 });
        new FilterParser().Parse("and(eq(year,1),eq(year,2),eq(year,3))", models.Filter);
        Assert.Equal(QueryPolicyViolationKind.NotAllowed, Assert.Throws<QueryPolicyException>(() => new SortDirectiveParser().Parse("title,asc", models.Sort)).Kind);
    }

    [Fact]
    public void NestedTreeLimitsAreIndependent()
    {
        var models = PolicyTestModels.Compile("default allow", new() { MaxNodes = 2, MaxDepth = 2 });
        new SortDirectiveParser().Parse("lower(title),asc,sortfor(authors,lower(lastname)),asc", models.Sort);
        Assert.Equal("MaxDepth", Assert.Throws<QueryPolicyException>(() => new SortDirectiveParser().Parse("sortfor(authors,lower(lower(lastname))),asc", models.Sort)).LimitName);
    }
}
