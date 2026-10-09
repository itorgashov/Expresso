using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Parsing.Policies;

namespace Expresso.Parsing.Test.Policies;

public class PolicyNoPolicyParityTests
{
    public static IEnumerable<object[]> Corpus() => PolicyParserCorpus.Part0().Concat(PolicyParserCorpus.Part1());

    [Theory, MemberData(nameof(Corpus))]
    public void LegacyQueryLiteralsKeepTheirTreesOrParseErrors(string query, bool sort, string catalog)
    {
        var fields = catalog.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(entry =>
        {
            var pair = entry.Split(':');
            return (pair[0], ResolveType(pair[1]));
        }).ToArray();
        var model = new QueryModel(fields, PolicyTestModels.Model().Collections);
        var guarded = QueryPolicyCompiler.Compile(new() {
            Rules = "default allow",
            Limits = new() { MaxDepth = 100, MaxNodes = int.MaxValue, MaxArgs = int.MaxValue,
                MaxInItems = int.MaxValue, MaxSortKeys = int.MaxValue, MaxStringLength = int.MaxValue }
        }, model, model);
        object[]? plainResult = null, policyResult = null;
        object[] Parse(QueryModel m) => sort ? Flatten(new SortDirectiveParser().Parse(query, m), "").ToArray() :
            new object[] { new FilterParser().Parse(query, m).Expression! };
        var plainError = Record.Exception(() => plainResult = Parse(model));
        var policyError = Record.Exception(() => policyResult = Parse(sort ? guarded.Sort : guarded.Filter));
        Assert.Equal(plainError?.GetType(), policyError?.GetType());
        Assert.Equal(plainError?.Message, policyError?.Message);
        Assert.Equal(plainResult, policyResult);
    }

    private static IEnumerable<object> Flatten(SortDirective sort, string path)
    {
        foreach (var item in sort.Items) { yield return path; yield return item.Expression; yield return item.Direction; }
        foreach (var collection in sort.Nested)
            foreach (var item in Flatten(collection.Directive, path + "/" + collection.Name)) yield return item;
    }
    private static Type ResolveType(string type) => type switch
    {
        "byte" => typeof(byte), "int" => typeof(int), "double" => typeof(double), "string" => typeof(string),
        "bool" => typeof(bool), "Guid" => typeof(Guid), "TimeSpan" => typeof(TimeSpan),
#if NET6_0_OR_GREATER
        "DateOnly" => typeof(DateOnly), "TimeOnly" => typeof(TimeOnly),
#else
        "DateOnly" => typeof(DateTime), "TimeOnly" => typeof(TimeSpan),
#endif
        _ => typeof(DateTime)
    };
}
