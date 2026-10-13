using System.Globalization;
using Expresso.Core.Filtering;
using Expresso.Parsing;
#if NETFRAMEWORK
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.ValueProviders.Providers;
#else
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
#endif

namespace Expresso.Sample.Test;

public sealed class ProjectionModelBindingTests
{
    [Theory]
    [InlineData("id", "id")]
    [InlineData("", null)]
    public async Task RepeatedFields_BindsOnlyFirstValue_AndEmptyFirstValueRemainsInvalid(string first, string? expected)
    {
        // Scenario: the framework binds a repeated string parameter; the first value wins, including an empty first value.
        var query = "?fields=" + first + "&fields=unknown";
#if NETFRAMEWORK
        var action = new HttpActionContext {
            ControllerContext = new HttpControllerContext { Request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/" + query) } };
        var provider = new QueryStringValueProvider(action, CultureInfo.InvariantCulture);
        var context = new System.Web.Http.ModelBinding.ModelBindingContext {
            ModelMetadata = new System.Web.Http.Metadata.Providers.DataAnnotationsModelMetadataProvider().GetMetadataForType(null, typeof(string)),
            ModelName = "fields", ValueProvider = provider, ModelState = new System.Web.Http.ModelBinding.ModelStateDictionary() };
        Assert.True(new System.Web.Http.ModelBinding.Binders.TypeConverterModelBinder().BindModel(action, context));
        var bound = (string?)context.Model;
        await Task.CompletedTask;
#else
        var http = new DefaultHttpContext();
        http.Request.QueryString = new QueryString(query);
        var context = DefaultModelBindingContext.CreateBindingContext(
            new ActionContext(http, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()),
            new QueryStringValueProvider(BindingSource.Query, http.Request.Query, CultureInfo.InvariantCulture),
            new EmptyModelMetadataProvider().GetMetadataForType(typeof(string)), bindingInfo: null, modelName: "fields");
        await new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance).BindModelAsync(context);
        var bound = (string?)context.Result.Model;
#endif
        Assert.Equal(expected, bound);
        var parser = new ProjectionDirectiveParser();
        var model = new QueryModel(new[] { ("id", typeof(int)) });
        if (expected is null)
            Assert.Throws<ArgumentException>(() => parser.Parse(bound ?? "", model));
        else
            Assert.Equal("id", Assert.Single(parser.Parse(bound, model).Selections).Name);
    }
}
