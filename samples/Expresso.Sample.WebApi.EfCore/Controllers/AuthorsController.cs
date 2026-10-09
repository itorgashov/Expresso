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
public sealed class AuthorsController(
    IRepository<Author> repository,
    IFilterParser filterParser,
    ISortDirectiveParser sortDirectiveParser,
    ControllerQueryModels<AuthorsController> queryModels,
    ILogger<AuthorsController> logger,
    IPagingDirectiveParser pagingParser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuthorViewModel>>> GetAll(
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

        var authors = await repository.GetAllAsync(parsed.FilterCriteria, parsed.SortDirective, paging.Paging, cancellationToken);
        if (!paging.Paging.IsEmpty)
        {
            var total = await repository.CountAsync(parsed.FilterCriteria, cancellationToken);
            foreach (var header in PagingHeaders.Values(paging.Paging, total))
            {
                Response.Headers[header.Name] = header.Value;
            }
        }

        return Ok(authors.Select(ViewModelMapper.ToViewModel).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AuthorViewModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var author = await repository.GetByIdAsync(id, cancellationToken);
        if (author is null)
        {
            return NotFound();
        }

        return Ok(ViewModelMapper.ToViewModel(author));
    }
}
