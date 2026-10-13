using System;
using System.Linq;
using System.Net.Http;

namespace Expresso.Sample.WebApi.NetFx.Controllers;

internal static class ProjectionRequest
{
    internal static bool HasFields(HttpRequestMessage request) =>
        request.GetQueryNameValuePairs().Any(p => string.Equals(p.Key, "fields", StringComparison.OrdinalIgnoreCase));
}

