using System.Net;
using System.Net.Http;
using Expresso.Core.Paging;
using Expresso.Sample.WebApi.NetFx.Ef6.Filtering;

namespace Expresso.Sample.WebApi.NetFx.Ef6.Controllers;

internal static class PagingHttp
{
    public static HttpResponseMessage WithHeaders(HttpRequestMessage request, object body, PagingDirective paging, long total)
    {
        var response = request.CreateResponse(HttpStatusCode.OK, body);
        foreach (var header in PagingHeaders.Values(paging, total))
        {
            response.Headers.TryAddWithoutValidation(header.Name, header.Value);
        }

        return response;
    }
}
