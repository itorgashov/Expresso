using Expresso.Core.Filtering;
using Expresso.Parsing;
using Expresso.Sample.WebApi.EfCore.DataAccess;
using Expresso.Sample.WebApi.EfCore.Entities;
using Expresso.Sample.WebApi.EfCore.Filtering;
using Expresso.Sample.WebApi.EfCore.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Expresso.Sample.WebApi.EfCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BooksController(
    IRepository<Book> repository,
    IFilterParser filterParser,
    ISortDirectiveParser sortDirectiveParser,
    IRequestFieldsInfoProvider requestFieldsProvider,
    IPagingDirectiveParser pagingParser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookViewModel>>> GetAll(
        [FromQuery] string? filter,
        [FromQuery] string? sort,
        [FromQuery] string? page,
        [FromQuery(Name = "pagesize")] string? pageSize,
        [FromQuery] string? skip,
        [FromQuery] string? take,
        CancellationToken cancellationToken)
    {
        var parsed = QueryParametersParser.Parse(filter, sort, "book", filterParser, sortDirectiveParser, requestFieldsProvider);
        var paging = QueryParametersParser.ParsePaging(page, pageSize, skip, take, pagingParser);
        if (parsed.IsBadRequest || paging.IsBadRequest)
        {
            return BadRequest();
        }

        var books = await repository.GetAllAsync(parsed.FilterCriteria, parsed.SortDirective, paging.Paging, cancellationToken);
        if (!paging.Paging.IsEmpty)
        {
            var total = await repository.CountAsync(parsed.FilterCriteria, cancellationToken);
            foreach (var header in PagingHeaders.Values(paging.Paging, total))
            {
                Response.Headers[header.Name] = header.Value;
            }
        }

        return Ok(books.Select(ViewModelMapper.ToViewModel).ToList());
    }

    [HttpGet("{id:int}")]
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
