using Expresso.Core.Paging;
using Expresso.Core.Projection;
using Expresso.Parsing;
using Expresso.Rendering.EntityFrameworkCore;
using Expresso.Sample.WebApi.EfCore.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Sample.Test;

public sealed class EfCoreProjectionRepositoryTests
{
    [Theory]
    [InlineData("book")]
    [InlineData("author")]
    [InlineData("publisher")]
    public async Task RealRepositories_ProjectPage_CountFilter_AndGetMissingId_WithFakeAdoCommands(string context)
    {
        // Scenario: real EF context, SQL translation, repository and async materializer; only the ADO command boundary is replaced.
        var io = new AdoProjectionRepositoryTests.FakeDb();
        using var connection = await io.OpenAsync();
        using var db = new SampleDbContext(new DbContextOptionsBuilder<SampleDbContext>().UseSqlServer(connection).Options);
        var transformer = new EfCoreExpressionToLinqTransformer(db.Database.ProviderName);
        var field = context == "book" ? "title" : context == "author" ? "lastname" : "name";
        var responseName = context == "author" ? "lastName" : field;
        var model = new Expresso.Sample.WebApi.EfCore.Filtering.RequestFieldsInfoProvider().GetProjectionModel(context);
        var projection = new ProjectionDirectiveParser().Parse(field, model);
        var filter = new FilterParser().Parse("eq(" + field + ",\"value\")", new[] { (field, typeof(string)) });
        var sort = new SortDirectiveParser().Parse(field + ",desc", new[] { (field, typeof(string)) });
        object repository = context == "book" ? new BookRepository(db, transformer) : context == "author"
            ? new AuthorRepository(db, transformer) : new PublisherRepository(db, transformer);
        io.Rows.Enqueue(new[] { new object?[] { "value", 1 } });
        var rows = await (Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>>)repository.GetType().GetMethod("GetAllAsync")!
            .Invoke(repository, new object?[] { filter, sort, new PagingDirective(skip: 1, take: 2), projection, CancellationToken.None })!;
        Assert.Equal("value", Assert.Single(rows)[responseName]);
        Assert.Single(rows[0]);
        Assert.Contains("OFFSET", Assert.Single(io.Commands).Sql);
        Assert.Contains(" DESC", io.Commands[0].Sql);
        Assert.Empty(db.ChangeTracker.Entries());
        io.Rows.Enqueue(new[] { new object?[] { 7L } });
        var total = await (Task<long>)repository.GetType().GetMethod("CountAsync")!.Invoke(repository, new object?[] { filter, CancellationToken.None })!;
        Assert.Equal(7, total);
        Assert.Contains("COUNT_BIG", io.Commands.Last().Sql);
        Assert.Contains("WHERE", io.Commands.Last().Sql);
        Assert.DoesNotContain("OFFSET", io.Commands.Last().Sql);
        io.Rows.Enqueue(Array.Empty<object?[]>());
        var single = await (Task<IReadOnlyDictionary<string, object?>?>)repository.GetType().GetMethod("GetByIdAsync")!
            .Invoke(repository, new object?[] { 999, projection, CancellationToken.None })!;
        Assert.Null(single);
        Assert.Equal(3, io.Commands.Count);
    }
}
