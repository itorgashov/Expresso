using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Parsing;
using Expresso.Sample.WebApi.DataAccess;
using Expresso.Sample.WebApi.Models;
using Expresso.Sample.WebApi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Expresso.Sample.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BooksController(
    IRepository<Book> repository,
    IFilterParser filterParser,
    ISortDirectiveParser sortDirectiveParser,
    IRequestFieldsInfoProvider requestFieldsProvider) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookViewModel>>> GetAll(
        [FromQuery] string? filter,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        FilterCriteria? filterCriteria = null;
        if (filter is not null)
        {
            try
            {
                var validFields = requestFieldsProvider.GetValidFilterFields("book");
                filterCriteria = filterParser.Parse(filter, validFields);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        SortDirective? sortDirective = null;
        if (sort is not null)
        {
            try
            {
                var validFields = requestFieldsProvider.GetValidSortFields("book");
                var rawSortDirective = sortDirectiveParser.Parse(sort, validFields);
                sortDirective = rawSortDirective.RemoveDuplicates();
                if (sortDirective.Items.Count < rawSortDirective.Items.Count)
                {
                    return BadRequest();
                }
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        var books = await repository.GetAllAsync(filterCriteria, sortDirective, cancellationToken);
        var viewModels = books.Select(b => new BookViewModel
        {
            Id = b.Id,
            Title = b.Title,
            Year = b.Year,
            Isbn = b.Isbn,
            Price = b.Price,
            Rating = b.Rating,
            CreatedAt = b.CreatedAt,
            Publisher = b.Publisher,
            Authors = b.Authors,
        }).ToList();

        return Ok(viewModels);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookViewModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var book = await repository.GetByIdAsync(id, cancellationToken);
        if (book is null)
        {
            return NotFound();
        }

        return Ok(new BookViewModel
        {
            Id = book.Id,
            Title = book.Title,
            Year = book.Year,
            Isbn = book.Isbn,
            Price = book.Price,
            Rating = book.Rating,
            CreatedAt = book.CreatedAt,
            Publisher = book.Publisher,
            Authors = book.Authors,
        });
    }
}
