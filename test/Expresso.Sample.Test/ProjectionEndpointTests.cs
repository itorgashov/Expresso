using System.Net;
using System.Reflection;
using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;
using Expresso.Parsing;
#if NETFRAMEWORK
using System.Net.Http;
using System.Web.Http;
#else
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
#endif

namespace Expresso.Sample.Test;

public sealed class ProjectionEndpointTests
{
    public static IEnumerable<object[]> Endpoints()
    {
#if NETFRAMEWORK
        var hosts = new[] { "Expresso.Sample.WebApi.NetFx", "Expresso.Sample.WebApi.NetFx.Ef6" };
#else
        var hosts = new[] { "Expresso.Sample.WebApi", "Expresso.Sample.WebApi.EfCore" };
#endif
        foreach (var host in hosts)
            foreach (var context in new[] { "book", "author", "publisher" })
                foreach (var byId in new[] { false, true })
                    yield return new object[] { host, context, byId };
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public void ReturnContract_MatchesSparsePayload_WithExplicitSupersetSchema(string host, string context, bool byId)
    {
        // Scenario: all 24 actions expose honest CLR contracts while documenting the available JSON field superset separately.
        var entity = char.ToUpperInvariant(context[0]) + context.Substring(1);
        var controller = Type.GetType(host + ".Controllers." + entity + "sController, " + host, true)!;
        var method = controller.GetMethod(byId ? "GetById" : "GetAll")!;
        var models = host.EndsWith(".EfCore", StringComparison.Ordinal) || host.EndsWith(".Ef6", StringComparison.Ordinal)
            ? host : "Expresso.Sample.Shared";
        var dto = Type.GetType(models + ".ViewModels." + entity + "ViewModel, " + models, true)!;
        var schema = byId ? dto : typeof(IReadOnlyList<>).MakeGenericType(dto);
#if NETFRAMEWORK
        Assert.Equal(typeof(Task<IHttpActionResult>), method.ReturnType);
        Assert.Equal(schema, method.GetCustomAttribute<System.Web.Http.Description.ResponseTypeAttribute>()?.ResponseType);
#else
        var payload = byId ? typeof(IReadOnlyDictionary<string, object?>) : typeof(IReadOnlyList<IReadOnlyDictionary<string, object?>>);
        Assert.Equal(typeof(Task<>).MakeGenericType(typeof(ActionResult<>).MakeGenericType(payload)), method.ReturnType);
        Assert.Equal(schema, method.GetCustomAttributes<ProducesResponseTypeAttribute>().Single(a => a.StatusCode == 200).Type);
#endif
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task InvalidFields_Return400WithoutRepository(string host, string context, bool byId)
    {
        // Scenario: model binding may turn a present empty value into null; the request must still be rejected.
        foreach (var fields in new string?[] { null, "", " ", "missing", "id.", "id,,id", "id.child" })
        {
            var (controller, probe) = Create(host, context, fields, present: true);
            Assert.Equal(400, await Invoke(controller, byId, fields));
            Assert.Equal(0, probe.Calls);
        }
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task FieldsPresence_IsCaseInsensitive_AndDoesNotMatchOtherParameters(string host, string context, bool byId)
    {
        // Scenario: a present uppercase empty parameter must fail before I/O; an unrelated parameter must preserve the default response.
        foreach (var query in new[] { "?FIELDS=", "?otherfields=" })
        {
            var (controller, probe) = Create(host, context, null, present: false);
#if NETFRAMEWORK
            ((ApiController)controller).Request.RequestUri = new Uri("http://localhost/api/" + query);
#else
            ((ControllerBase)controller).Request.QueryString = new QueryString(query);
#endif
            var present = query.StartsWith("?FIELDS", StringComparison.Ordinal);
            Assert.Equal(present ? 400 : 200, await Invoke(controller, byId, null));
            Assert.Equal(present ? 0 : 1, probe.Calls);
        }
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task MissingFields_SelectsAll_ExplicitFieldsArePassedToRepository(string host, string context, bool byId)
    {
        var (all, allProbe) = Create(host, context, null, present: false);
        Assert.Equal(200, await Invoke(all, byId, null));
        Assert.False(allProbe.Projection!.IsAll);
        Assert.NotNull(allProbe.Projection.Find("id"));
        Assert.True(allProbe.Projection.Selections.Count > 1);
        var (selected, probe) = Create(host, context, " ID , id ", present: true);
        Assert.Equal(200, await Invoke(selected, byId, " ID , id "));
        Assert.Equal("id", Assert.Single(probe.Projection!.Selections).Name);
        Assert.Equal(1, probe.Calls);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task NestedBookSelectionAndBareCollectionAreAccepted(string host, string context, bool byId)
    {
        if (context != "book") return;
        foreach (var fields in new[] { "authors.lastname,authors.awards.title", "authors" })
        {
            var (controller, probe) = Create(host, context, fields, present: true);
            Assert.Equal(200, await Invoke(controller, byId, fields));
            Assert.NotNull(probe.Projection!.Find("authors"));
        }
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task MissingEntity_Returns404AfterProjectionValidation(string host, string context, bool byId)
    {
        if (!byId) return;
        var (controller, _) = Create(host, context, "id", present: true);
        Assert.Equal(404, await Invoke(controller, true, "id", id: 0));
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Projection_PreservesFilterSortPagingAndCountHeaders(string host, string context, bool byId)
    {
        // Scenario: projection combines with filter/sort/skip/take; count sees the filter but not the page and response keeps its headers.
        if (byId) return;
        var (controller, probe) = Create(host, context, "id", present: true);
        var field = context == "book" ? "title" : context == "author" ? "lastname" : "name";
        var response = await InvokeResponse(controller, false, "id", extras: new Dictionary<string, object?> {
            ["filter"] = "eq(" + field + ",\"value\")", ["sort"] = field + ",desc", ["skip"] = "2", ["take"] = "3" });
        Assert.Equal(200, response.Status);
        Assert.Equal("1", response.Headers["X-Total-Count"]);
        Assert.Equal(1, probe.CountCalls);
        Assert.Equal(2, probe.Paging!.Offset);
        Assert.Equal(3, probe.Paging.Limit);
        Assert.NotNull(probe.Filter);
        Assert.NotNull(probe.Sort);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task SparseJson_PreservesHostNamingAndSelectedNulls(string host, string context, bool byId)
    {
        // Scenario: sparse dictionary serialization retains each host's original names and keeps selected nulls, but omits unselected fields.
        var (all, _) = Create(host, context, null, present: false);
        var response = await InvokeResponse(all, byId, null);
        var nullName = context == "book" ? "isbn" : context == "author" ? "dateOfBirth" : "location";
#if NETFRAMEWORK
        var idName = "Id";
        nullName = char.ToUpperInvariant(nullName[0]) + nullName.Substring(1);
#else
        var idName = "id";
#endif
        Assert.Contains("\"" + idName + "\"", response.Json);
        Assert.Contains("\"" + nullName + "\":null", response.Json);
        var (selected, _) = Create(host, context, "id", present: true);
        var sparse = await InvokeResponse(selected, byId, "id");
        Assert.Contains("\"" + idName + "\"", sparse.Json);
        Assert.DoesNotContain("\"" + nullName + "\"", sparse.Json);
    }

    private static (object Controller, IProbe Probe) Create(string host, string context, string? fields, bool present)
    {
        var controllerName = char.ToUpperInvariant(context[0]) + context.Substring(1) + "sController";
        var controllerType = Type.GetType(host + ".Controllers." + controllerName + ", " + host, throwOnError: true)!;
        var providerType = Type.GetType(host + ".Filtering.RequestFieldsInfoProvider, " + host, true)!;
        var providerConstructor = providerType.GetConstructors().Single();
        var provider = (IRequestQueryModelProvider)providerConstructor.Invoke(providerConstructor.GetParameters().Select(p =>
            p.ParameterType.IsEnum ? Enum.Parse(p.ParameterType, "Sqlite") : throw new InvalidOperationException()).ToArray());
        var projectionProvider = provider as IRequestProjectionModelProvider;
        if (projectionProvider is null)
        {
            var projectionType = Type.GetType(host + ".Filtering.RequestProjectionModelProvider, " + host, true)!;
            var mappings = new Expresso.Sample.Shared.DataAccess.SampleProjectionMappings(
                Expresso.Sample.Shared.DataAccess.SampleSqlFactory.Create(Expresso.Sample.Shared.DataAccess.SampleEngine.Sqlite));
            projectionProvider = (IRequestProjectionModelProvider)Activator.CreateInstance(projectionType, mappings)!;
        }
        var constructor = controllerType.GetConstructors().Single();
        IProbe? probe = null;
        var arguments = constructor.GetParameters().Select(p =>
        {
            if (p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition().Name == "IRepository`1")
                return (object)(probe = (IProbe)Activator.CreateInstance(typeof(Repository<>).MakeGenericType(p.ParameterType.GetGenericArguments()[0]))!);
            if (p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition().Name == "ControllerQueryModels`1")
                return Activator.CreateInstance(p.ParameterType, provider.GetFilterModel(context), provider.GetSortModel(context))!;
#if !NETFRAMEWORK
            if (p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition().Name == "ILogger`1")
                return Activator.CreateInstance(typeof(NullLogger<>).MakeGenericType(controllerType))!;
#endif
            if (p.ParameterType == typeof(IFilterParser)) return new FilterParser();
            if (p.ParameterType == typeof(ISortDirectiveParser)) return new SortDirectiveParser();
            if (p.ParameterType == typeof(IPagingDirectiveParser)) return new PagingDirectiveParser();
            if (p.ParameterType == typeof(IProjectionDirectiveParser)) return new ProjectionDirectiveParser();
            if (p.ParameterType == typeof(IRequestProjectionModelProvider)) return (object)projectionProvider;
            throw new InvalidOperationException(p.ParameterType.FullName);
        }).ToArray();
        var controller = constructor.Invoke(arguments);
#if NETFRAMEWORK
        var api = (ApiController)controller;
        api.Request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/" + context + "s" +
            (present ? "?fields=" + Uri.EscapeDataString(fields ?? "") : ""));
        api.Configuration = new HttpConfiguration();
        api.Configuration.Formatters.JsonFormatter.SerializerSettings.ContractResolver =
            (Newtonsoft.Json.Serialization.IContractResolver)Activator.CreateInstance(
                Type.GetType(host + ".Infrastructure.ProjectionJsonContractResolver, " + host, true)!)!;
#else
        var api = (ControllerBase)controller;
        var http = new DefaultHttpContext();
        http.Request.QueryString = new QueryString(present ? "?fields=" + Uri.EscapeDataString(fields ?? "") : "");
        api.ControllerContext = new ControllerContext { HttpContext = http };
#endif
        return (controller, probe!);
    }

    private static async Task<int> Invoke(object controller, bool byId, string? fields, int id = 1) =>
        (await InvokeResponse(controller, byId, fields, id)).Status;

    private static async Task<(int Status, string Json, IReadOnlyDictionary<string, string> Headers)> InvokeResponse(object controller, bool byId, string? fields, int id = 1, Dictionary<string, object?>? extras = null)
    {
        var method = controller.GetType().GetMethod(byId ? "GetById" : "GetAll")!;
        var arguments = method.GetParameters().Select(p => extras is not null && extras.TryGetValue(p.Name!, out var supplied) ? supplied : p.Name == "fields" ? (object?)fields :
            p.Name == "id" ? id : p.ParameterType == typeof(CancellationToken) ? CancellationToken.None : null).ToArray();
        var task = (Task)method.Invoke(controller, arguments)!;
        await task;
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
#if NETFRAMEWORK
        using var response = await ((IHttpActionResult)result).ExecuteAsync(CancellationToken.None);
        return ((int)response.StatusCode, response.Content is null ? "" : await response.Content.ReadAsStringAsync(),
            response.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase));
#else
        var action = (IActionResult)result.GetType().GetProperty("Result")!.GetValue(result)!;
        var status = action switch { ObjectResult value => value.StatusCode ?? 200, StatusCodeResult value => value.StatusCode, _ => 200 };
        return (status, action is ObjectResult body
            ? System.Text.Json.JsonSerializer.Serialize(body.Value, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
            : "", ((ControllerBase)controller).Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase));
#endif
    }

    public interface IProbe
    {
        int Calls { get; }
        int CountCalls { get; }
        ProjectionDirective? Projection { get; }
        PagingDirective? Paging { get; }
        FilterCriteria? Filter { get; }
        SortDirective? Sort { get; }
    }
    public sealed class Repository<T> : IProbe, Expresso.Sample.Shared.DataAccess.IRepository<T>,
#if NETFRAMEWORK
        Expresso.Sample.WebApi.NetFx.Ef6.DataAccess.IRepository<T>
#else
        Expresso.Sample.WebApi.EfCore.DataAccess.IRepository<T>
#endif
    {
        public int Calls { get; private set; }
        public ProjectionDirective? Projection { get; private set; }
        public int CountCalls { get; private set; }
        public PagingDirective? Paging { get; private set; }
        public FilterCriteria? Filter { get; private set; }
        public SortDirective? Sort { get; private set; }
        private static IReadOnlyDictionary<string, object?> Row(ProjectionDirective projection)
        {
            var row = new Dictionary<string, object?> { ["id"] = 1 };
            var nullField = typeof(T).Name == "Book" ? "isbn" : typeof(T).Name == "Author" ? "dateofbirth" : "location";
            if (projection.IsAll || projection.Find(nullField) is not null)
            {
                var nullName = typeof(T).Name == "Book" ? "isbn" : typeof(T).Name == "Author" ? "dateOfBirth" : "location";
                row.Add(nullName, null);
            }
            return row;
        }
        public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllAsync(FilterCriteria? filter, SortDirective? sort,
            PagingDirective paging, ProjectionDirective projection, CancellationToken token = default)
        { Calls++; Projection = projection; Paging = paging; Filter = filter; Sort = sort; return Task.FromResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(new[] { Row(projection) }); }
        public Task<IReadOnlyDictionary<string, object?>?> GetByIdAsync(int id, ProjectionDirective projection, CancellationToken token = default)
        { Calls++; Projection = projection; return Task.FromResult(id == 0 ? null : Row(projection)); }
        public Task<long> CountAsync(FilterCriteria? filter, CancellationToken token = default)
        {
            Assert.Same(Filter, filter);
            CountCalls++;
            return Task.FromResult(1L);
        }
    }
}
