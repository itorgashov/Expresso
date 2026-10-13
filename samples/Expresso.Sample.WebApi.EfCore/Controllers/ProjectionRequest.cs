using Microsoft.AspNetCore.Http;

namespace Expresso.Sample.WebApi.EfCore.Controllers;

internal static class ProjectionRequest
{
    internal static bool HasFields(HttpRequest request) =>
        request.Query.ContainsKey("fields");
}

