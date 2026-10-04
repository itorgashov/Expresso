using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Rendering.Linq;
using Expresso.Sample.WebApi.NetFx.Ef6.Entities;

namespace Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

public sealed class BookRepository : IRepository<Book>
{
    private readonly SampleEf6Context _db;
    private readonly IExpressionToLinqTransformer _transformer;

    public BookRepository(SampleEf6Context db, IExpressionToLinqTransformer transformer)
    {
        _db = db;
        _transformer = transformer;
    }

    public async Task<IReadOnlyList<Book>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Book> query = _db.Books;

        if (filterCriteria is not null)
        {
            query = query.Where(_transformer, filterCriteria, BookLinqMappings.Books);
        }

        if (sortDirective is not null && sortDirective.Items.Count > 0)
        {
            query = query.OrderBy(_transformer, sortDirective, BookLinqMappings.Books);
        }
        else
        {
            query = query.OrderBy(b => b.Id);
        }

        query = query.Include("Publisher").Include("Authors.Awards");

        var books = await query.ToListAsync(cancellationToken);

        if (sortDirective is not null && sortDirective.Nested.Count > 0)
        {
            ApplyNestedSort(books, sortDirective);
        }

        return books;
    }

    public async Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Books
            .Include("Publisher")
            .Include("Authors.Awards")
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    private void ApplyNestedSort(List<Book> books, SortDirective sortDirective)
    {
        foreach (var book in books)
        {
            var authors = book.Authors
                .OrderByNested(_transformer, sortDirective, BookLinqMappings.Authors, "authors")
                .ToList();
            foreach (var author in authors)
            {
                author.Awards = author.Awards
                    .OrderByNested(_transformer, sortDirective, BookLinqMappings.Awards, "authors", "awards")
                    .ToList();
            }

            book.Authors = authors;
        }
    }
}
