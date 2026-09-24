using Expresso.Core.Filtering;
using Expresso.Parsing;
using Expresso.Sample.Shared.DataAccess;
using Expresso.Sample.Shared.Filtering;
using Expresso.Sample.Shared.Models;
using Expresso.Sample.Shared.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Expresso.Sample.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
/// <summary>Lists books and returns one book by id. <c>filter</c> and <c>sort</c> are Expresso query strings.</summary>
/// <param name="repository">Book store.</param>
/// <param name="filterParser">Expresso filter parser.</param>
/// <param name="sortDirectiveParser">Expresso sort parser.</param>
/// <param name="requestFieldsProvider">Book field catalog.</param>
public sealed class BooksController(
    IRepository<Book> repository,
    IFilterParser filterParser,
    ISortDirectiveParser sortDirectiveParser,
    IRequestFieldsInfoProvider requestFieldsProvider) : ControllerBase
{
    [HttpGet]
    /// <summary>Returns books that match <paramref name="filter"/>, ordered by <paramref name="sort"/>.</summary>
    /// <param name="filter">Expresso filter, or <see langword="null"/> to return every book.</param>
    /// <param name="sort">Expresso sort, or <see langword="null"/> for the repository default.</param>
    /// <param name="cancellationToken">Token that cancels the query.</param>
    /// <returns>200 with the books, or 400 when the query string is invalid.</returns>
    public async Task<ActionResult<IReadOnlyList<BookViewModel>>> GetAll(
        [FromQuery] string? filter,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        var parsed = QueryParametersParser.Parse(filter, sort, "book", filterParser, sortDirectiveParser, requestFieldsProvider);
        if (parsed.IsBadRequest)
        {
            return BadRequest();
        }

        var books = await repository.GetAllAsync(parsed.FilterCriteria, parsed.SortDirective, cancellationToken);
        return Ok(books.Select(ViewModelMapper.ToViewModel).ToList());
    }

    [HttpGet("{id:int}")]
    /// <summary>Returns one book.</summary>
    /// <param name="id">Book primary key.</param>
    /// <param name="cancellationToken">Token that cancels the query.</param>
    /// <returns>200 with the book, or 404 when it does not exist.</returns>
    public async Task<ActionResult<BookViewModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var book = await repository.GetByIdAsync(id, cancellationToken);
        if (book is null)
        {
            return NotFound();
        }

        return Ok(ViewModelMapper.ToViewModel(book));
    }
}
