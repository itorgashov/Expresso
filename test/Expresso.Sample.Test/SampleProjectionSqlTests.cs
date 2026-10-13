using Expresso.Core.Projection;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Sample.WebApi.EfCore.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Sample.Test;

public sealed class SampleProjectionSqlTests
{
    [Fact]
    public void FullResponse_DateAndTimeConversionsOnlySelectMappedScalarColumns()
    {
        // Scenario: DTO date/time representations differ from mapped DateOnly/TimeOnly columns; projection stays at the SQL boundary.
        using var db = new SampleDbContext(new DbContextOptionsBuilder<SampleDbContext>()
            .UseSqlServer("Server=unused;Database=unused").Options);
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        Assert.Contains("date_of_birth", db.Authors.Select(transformer.RenderProjection(ProjectionDirective.All,
            SampleProjectionMappings.Authors).Selector).ToQueryString());
        Assert.Contains("opens_at", db.Publishers.Select(transformer.RenderProjection(ProjectionDirective.All,
            SampleProjectionMappings.Publishers).Selector).ToQueryString());
        Assert.Contains("publisher", db.Books.Select(transformer.RenderProjection(ProjectionDirective.All,
            SampleProjectionMappings.Books).Selector).ToQueryString());
    }
}

