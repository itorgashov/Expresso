using Microsoft.AspNetCore.Http;

namespace Expresso.Sample.WebApi.Controllers;

internal static class ProjectionRequest
{
    internal static bool HasFields(HttpRequest request) =>
        request.Query.ContainsKey("fields");
}

