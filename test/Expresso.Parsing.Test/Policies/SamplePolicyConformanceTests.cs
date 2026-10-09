using System.Text.RegularExpressions;
using Expresso.Core.Filtering;
using Expresso.Core.Policies;
using Expresso.Parsing.Policies;
using Expresso.Sample.Shared.Filtering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#if NET6_0_OR_GREATER
using Expresso.Sample.WebApi.Filtering;
#else
using Expresso.Sample.WebApi.NetFx.Filtering;
#endif

namespace Expresso.Parsing.Test.Policies;

public class SamplePolicyConformanceTests
{
    private static readonly string[] Hosts = { "Expresso.Sample.WebApi", "Expresso.Sample.WebApi.NetFx", "Expresso.Sample.WebApi.EfCore", "Expresso.Sample.WebApi.NetFx.Ef6" };
    private static IConfigurationRoot Configuration(string host) => new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory).AddJsonFile("Samples/" + host + ".json").Build();
    private static QueryPolicyModels Compile(string host, string context)
    {
        var provider = new RequestFieldsInfoProvider();
        return QueryPolicyCompiler.Compile(SamplePolicySetup.Read(Configuration(host).GetSection("Expresso:Policies:" + context))!,
            provider.GetFilterModel(context), provider.GetSortModel(context));
    }

    [Fact]
    public void SharedPoliciesAgreeAcrossHostsAndPublisherExceptionIsNarrow()
    {
        foreach (string context in new[] { "book", "author" })
        {
            var expected = Configuration(Hosts[0]).GetSection("Expresso:Policies:" + context).AsEnumerable().OrderBy(p => p.Key).ToArray();
            foreach (string host in Hosts.Skip(1))
                Assert.Equal(expected, Configuration(host).GetSection("Expresso:Policies:" + context).AsEnumerable().OrderBy(p => p.Key).ToArray());
        }
        var publisher = Configuration(Hosts[0]).GetSection("Expresso:Policies:publisher:Rules").GetChildren()
            .Select(c => c.Value).ToArray();
        foreach (string host in Hosts.Skip(1).Take(2))
            Assert.Equal(publisher, Configuration(host).GetSection("Expresso:Policies:publisher:Rules").GetChildren().Select(c => c.Value).ToArray());
        var ef6 = Configuration(Hosts[3]).GetSection("Expresso:Policies:publisher:Rules").GetChildren().Select(c => c.Value).ToArray();
        Assert.DoesNotContain(ef6, line => line != null && line.Contains("{eq,neq}(*"));
        Assert.DoesNotContain(ef6, line => line != null && line.Contains("opens | closes"));
    }

    public static IEnumerable<object[]> Cases()
    {
        var corpus = new (string Context, string Query, bool Sort, bool Allow)[]
        {
            ("book", "gt(year,2000)", false, true), ("book", "contains(title,\"War\")", false, true),
            ("book", "startswith(publisher,\"North\")", false, true), ("book", "gte(createdat,\"2020-01-01\")", false, true),
            ("book", "any(authors,eq(displayname,\"Leo Tolstoy\"))", false, true), ("book", "eq(count(authors),2)", false, true),
            ("book", "and(gt(year,2020),any(authors,eq(displayname,\"Leo Tolstoy\")))", false, true),
            ("book", "any(authors,any(awards,eq(title,\"Nobel Prize\")))", false, true),
            ("book", "eq(lower(title),\"war\")", false, true), ("book", "in(isbn,\"a\",\"b\")", false, true),
            ("book", "not(eq(title,\"x\"))", false, false), ("book", "eq(len(title),2)", false, false),
            ("book", "gt(add(year,1),2000)", false, false), ("book", "eq(title,publisher)", false, false),
            ("book", "rating,desc,title,asc", true, true),
            ("book", "year,desc,sortfor(authors,lastname),asc,sortfor(authors/awards,year),desc", true, true),
            ("book", "lower(title),asc", true, false), ("book", "title,asc,year,asc,price,asc,rating,asc", true, false),
            ("author", "eq(firstname,\"George\")", false, true), ("author", "eq(lower(lastname),\"orwell\")", false, true),
            ("author", "any(awards,eq(title,\"Prize\"))", false, true), ("author", "eq(1,1)", false, true),
            ("author", "or(eq(firstname,\"a\"),eq(lastname,\"b\"))", false, false),
            ("author", "not(eq(firstname,\"a\"))", false, false), ("author", "eq(len(lower(firstname)),2)", false, false),
            ("author", "eq(substr(lastname,1,2),\"ab\")", false, false),
            ("author", "eq(indexof(\"x\",firstname),0)", false, false),
            ("author", "eq(replace(\"x\",firstname,\"y\"),\"y\")", false, false),
            ("author", "lastname,asc,sortfor(awards,title),asc", true, true), ("author", "len(firstname),asc", true, false),
            ("publisher", "eq(name,\"North\")", false, true),
            ("publisher", "contains(country,\"US\")", false, true), ("publisher", "startswith(location,\"New\")", false, true),
            ("publisher", "eq(len(name),3)", false, false), ("publisher", "eq(1,1)", false, false),
            ("publisher", "not(eq(name,\"x\"))", false, false), ("publisher", "or(eq(name,\"x\"),eq(country,\"US\"))", false, false),
            ("publisher", "name,asc,country,desc", true, true), ("publisher", "lower(name),asc", true, false)
        };
        foreach (var host in Hosts) foreach (var c in corpus)
            yield return new object[] { host, c.Context, c.Query, c.Sort, c.Allow };
        foreach (var host in Hosts.Take(3))
            yield return new object[] { host, "publisher", "eq(opens,\"09:00\")", false, true };
    }

    [Theory, MemberData(nameof(Cases))]
    public void ConfiguredPoliciesMatchTheirCorpora(string host, string context, string query, bool sort, bool allow)
    {
        // Every host must make the same allow/deny decision, except the EF6
        // publisher catalog cannot expose time fields on every provider.
        var models = Compile(host, context);
        Action parse = sort ? () => new SortDirectiveParser().Parse(query, models.Sort) : () => new FilterParser().Parse(query, models.Filter);
        if (allow) parse(); else Assert.Throws<QueryPolicyException>(parse);
    }

    [Fact]
    public void EveryDocumentedSampleQueryIsAccepted()
    {
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Docs", "sample-app.md"));
        int tested = 0;
        foreach (Match match in Regex.Matches(text, @"^GET /api/(books|authors|publishers)\?([^\r\n]+)", RegexOptions.Multiline))
        {
            var context = match.Groups[1].Value.TrimEnd('s'); var models = Compile(Hosts[0], context);
            foreach (var part in match.Groups[2].Value.Split('&'))
            {
                if (part.StartsWith("filter=")) { new FilterParser().Parse(part.Substring(7), models.Filter); tested++; }
                if (part.StartsWith("sort=")) { new SortDirectiveParser().Parse(part.Substring(5), models.Sort); tested++; }
            }
        }
        Assert.True(tested >= 15);
    }

    [Fact]
    public void ReaderKeepsOmittedDefaultsAndTypedRegistrationCompilesImmediately()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Expresso:Policies:book:Rules:0"] = "default allow", ["Expresso:Policies:book:Limits:MaxSortKeys"] = "3" }).Build();
        var services = new ServiceCollection();
        SamplePolicySetup.Register<SamplePolicyConformanceTests>(services, config, "book", new RequestFieldsInfoProvider(), _ => { });
        var models = services.BuildServiceProvider().GetRequiredService<ControllerQueryModels<SamplePolicyConformanceTests>>();
        new FilterParser().Parse("eq(title,\"x\")", models.Filter);
        var definition = SamplePolicySetup.Read(config.GetSection("Expresso:Policies:book"))!;
        Assert.Equal(3, definition.Limits.MaxSortKeys); Assert.Equal(8, definition.Limits.MaxDepth);
        Assert.Null(SamplePolicySetup.Read(config.GetSection("missing")));
        config["Expresso:Policies:book:Rules:0"] = "filter := missing";
        Assert.Contains("book", Assert.Throws<InvalidOperationException>(() => SamplePolicySetup.Register<object>(services, config, "book", new RequestFieldsInfoProvider(), _ => { })).Message);
    }

    [Fact]
    public void SampleParserExposesViolationWithoutChangingBadRequestContract()
    {
        var models = Compile(Hosts[0], "book");
        var result = QueryParametersParser.Parse("not(eq(title,\"x\"))", null, new FilterParser(), new SortDirectiveParser(), models.Filter, models.Sort);
        Assert.True(result.IsBadRequest); Assert.NotNull(result.PolicyViolation); Assert.Null(result.FilterCriteria);
        result = QueryParametersParser.Parse(null, "title,asc,title,desc,title,asc,title,desc", new FilterParser(), new SortDirectiveParser(), models.Filter, models.Sort);
        Assert.Equal("MaxSortKeys", result.PolicyViolation!.LimitName);
    }

    [Fact]
    public void PublisherPolicyCompilesAgainstReducedEf6Catalog()
    {
        var provider = new RequestFieldsInfoProvider();
        var model = new QueryModel(provider.GetValidFilterFields("publisher").Where(f => f.Item1 != "opens" && f.Item1 != "closes").ToArray());
        var compiled = QueryPolicyCompiler.Compile(SamplePolicySetup.Read(Configuration(Hosts[3]).GetSection("Expresso:Policies:publisher"))!, model, model);
        new FilterParser().Parse("eq(name,\"North\")", compiled.Filter);
    }
}
