using Expresso.Core.Filtering;
using Expresso.Core.Paging;
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
/// <param name="queryModels">Book query models and policy.</param>
/// <param name="logger">Policy violation logger.</param>
/// <param name="pagingParser">Expresso paging parser.</param>
public sealed class BooksController(
    IRepository<Book> repository,
    IFilterParser filterParser,
    ISortDirectiveParser sortDirectiveParser,
    ControllerQueryModels<BooksController> queryModels,
    ILogger<BooksController> logger,
    IPagingDirectiveParser pagingParser) : ControllerBase
{
    [HttpGet]
    /// <summary>Returns books that match <paramref name="filter"/>, ordered by <paramref name="sort"/>, optionally paged.</summary>
    /// <param name="filter">Expresso filter, or <see langword="null"/> to return every book.</param>
    /// <param name="sort">Expresso sort, or <see langword="null"/> for the repository default.</param>
    /// <param name="page">1-based page. Requires <paramref name="pageSize"/>.</param>
    /// <param name="pageSize">Page size. Without <paramref name="page"/>, this is the first page.</param>
    /// <param name="skip">Rows to skip when page size is omitted.</param>
    /// <param name="take">Maximum rows when page size is omitted.</param>
    /// <param name="cancellationToken">Token that cancels the query.</param>
    /// <returns>200 with the books, or 400 when the query string is invalid or both paging styles are set.</returns>
    public async Task<ActionResult<IReadOnlyList<BookViewModel>>> GetAll(
        [FromQuery] string? filter,
        [FromQuery] string? sort,
        [FromQuery] string? page,
        [FromQuery(Name = "pagesize")] string? pageSize,
        [FromQuery] string? skip,
        [FromQuery] string? take,
        CancellationToken cancellationToken)
    {
        var parsed = QueryParametersParser.Parse(filter, sort, filterParser, sortDirectiveParser, queryModels.Filter, queryModels.Sort);
        var paging = QueryParametersParser.ParsePaging(page, pageSize, skip, take, pagingParser);
        if (parsed.IsBadRequest || paging.IsBadRequest)
        {
            if (parsed.PolicyViolation is { } violation)
                logger.LogWarning("Query policy rejected {Target}: {Kind}; path={Path}; rule={Rule}; limit={Limit}",
                    violation.Target, violation.Kind, violation.Path, violation.RuleText, violation.LimitName);
            return BadRequest();
        }

        var books = await repository.GetAllAsync(parsed.FilterCriteria, parsed.SortDirective, paging.Paging, cancellationToken);
        await WritePagingHeadersAsync(paging.Paging, parsed.FilterCriteria, cancellationToken);
        return Ok(books.Select(ViewModelMapper.ToViewModel).ToList());
    }

    private async Task WritePagingHeadersAsync(PagingDirective paging, FilterCriteria? filter, CancellationToken cancellationToken)
    {
        if (paging.IsEmpty)
        {
            return;
        }

        var total = await repository.CountAsync(filter, cancellationToken);
        foreach (var header in PagingHeaders.Values(paging, total))
        {
            Response.Headers[header.Name] = header.Value;
        }
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
