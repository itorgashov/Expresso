using Expresso.Core.Filtering;
using Expresso.Core.Policies;
using Expresso.Parsing.Policies;

namespace Expresso.Parsing.Test.Policies;

internal static class PolicyTestModels
{
    internal static QueryModel Model()
    {
        var awards = new QueryModel(new[] { ("title", typeof(string)), ("year", typeof(int)) });
        var authors = new QueryModel(new[] { ("title", typeof(string)), ("firstname", typeof(string)),
            ("lastname", typeof(string)), ("displayname", typeof(string)), ("age", typeof(int)), ("dateofbirth", typeof(DateTime)) },
            new[] { new CollectionModel("awards", awards) });
        return new QueryModel(new[] { ("title", typeof(string)), ("status", typeof(string)), ("publisher", typeof(string)),
            ("isbn", typeof(string)), ("year", typeof(int)), ("age", typeof(int)), ("price", typeof(double)),
            ("rating", typeof(double)), ("createdat", typeof(DateTime)), ("id", typeof(Guid)), ("clock", typeof(TimeSpan)),
            ("sort", typeof(string)), ("filter", typeof(string)), ("default", typeof(string)), ("deny", typeof(string)), ("allow", typeof(string)) },
            new[] { new CollectionModel("authors", authors), new CollectionModel("editors", authors) });
    }
    internal static QueryPolicyModels Compile(string rules, QueryPolicyLimits? limits = null,
        QueryPolicyErrorDetail detail = QueryPolicyErrorDetail.Generic, QueryModel? model = null) =>
        QueryPolicyCompiler.Compile(new() { Rules = rules, Limits = limits ?? new(), ErrorDetail = detail }, model ?? Model(), model ?? Model());
}
