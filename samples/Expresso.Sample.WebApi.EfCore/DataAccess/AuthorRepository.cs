using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Rendering.Linq;
using Expresso.Sample.WebApi.EfCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Sample.WebApi.EfCore.DataAccess;

public sealed class AuthorRepository : IRepository<Author>
{
    private readonly SampleDbContext _db;
    private readonly IExpressionToLinqTransformer _transformer;

    public AuthorRepository(SampleDbContext db, IExpressionToLinqTransformer transformer)
    {
        _db = db;
        _transformer = transformer;
    }

    public async Task<IReadOnlyList<Author>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Author> query = _db.Authors;

        if (filterCriteria is not null)
        {
            query = query.Where(_transformer, filterCriteria, BookLinqMappings.Authors);
        }

        if (sortDirective is not null && sortDirective.Items.Count > 0)
        {
            query = query.OrderBy(_transformer, sortDirective, BookLinqMappings.Authors);
        }
        else
        {
            query = query.OrderBy(a => a.Id);
        }

        var authors = await query.Include(a => a.Awards).ToListAsync(cancellationToken);

        if (sortDirective is not null && sortDirective.Nested.Count > 0)
        {
            foreach (var author in authors)
            {
                author.Awards = author.Awards
                    .OrderByNested(_transformer, sortDirective, BookLinqMappings.Awards, "awards")
                    .ToList();
            }
        }

        return authors;
    }

    public async Task<Author?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Authors.Include(a => a.Awards).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
}
