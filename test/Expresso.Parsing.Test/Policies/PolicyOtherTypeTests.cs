using Expresso.Core.Filtering;
using Expresso.Core.Policies;
using Expresso.Parsing.Policies;

namespace Expresso.Parsing.Test.Policies;

public class PolicyOtherTypeTests
{
    private static QueryModel Model() => new(new[] { ("a", typeof(long)), ("b", typeof(long)) });

    [Fact]
    public void LongFieldsCanBeNamedInAllowRulesAndWildcards()
    {
        // Two long fields form a valid in-filter, and a long field is a valid sort key.
        var model = Model();
        var named = QueryPolicyCompiler.Compile(new() { Rules = "filter := in(a,b) sort := a" }, model, model);
        new FilterParser().Parse("in(a,b)", named.Filter);
        new SortDirectiveParser().Parse("a,asc", named.Sort);

        var wildcard = QueryPolicyCompiler.Compile(new() { Rules = "default allow" }, model, model);
        new FilterParser().Parse("in(a,b)", wildcard.Filter);
        new SortDirectiveParser().Parse("a,asc", wildcard.Sort);
    }

    [Fact]
    public void LongFieldsRemainVisibleToDenyRules()
    {
        // A deny must see both the in-call and its long-valued operands, including sort keys.
        var model = Model();
        var functions = QueryPolicyCompiler.Compile(new() { Rules = "default allow deny in(...) | isnull(*)" }, model, model);
        Assert.Equal(QueryPolicyViolationKind.DenyRuleMatched,
            Assert.Throws<QueryPolicyException>(() => new FilterParser().Parse("in(a,b)", functions.Filter)).Kind);

        var fields = QueryPolicyCompiler.Compile(new() { Rules = "default allow deny ~a" }, model, model);
        Assert.Equal(QueryPolicyViolationKind.DenyRuleMatched,
            Assert.Throws<QueryPolicyException>(() => new FilterParser().Parse("in(a,b)", fields.Filter)).Kind);
        Assert.Equal(QueryPolicyViolationKind.DenyRuleMatched,
            Assert.Throws<QueryPolicyException>(() => new SortDirectiveParser().Parse("a,asc", fields.Sort)).Kind);
    }

    [Fact]
    public void NestedLongSortFieldCanBeAllowedAndDenied()
    {
        // The unsupported CLR kind must remain visible through a collection item catalog.
        var model = new QueryModel(null, new[] { new CollectionModel("items", Model()) });
        var allow = QueryPolicyCompiler.Compile(new() { Rules = "sort := sortfor(items,a)" }, model, model);
        new SortDirectiveParser().Parse("sortfor(items,a),asc", allow.Sort);

        var deny = QueryPolicyCompiler.Compile(new() { Rules = "default allow deny sort: ~items/a" }, model, model);
        Assert.Equal(QueryPolicyViolationKind.DenyRuleMatched,
            Assert.Throws<QueryPolicyException>(() => new SortDirectiveParser().Parse("sortfor(items,a),asc", deny.Sort)).Kind);
    }
}
