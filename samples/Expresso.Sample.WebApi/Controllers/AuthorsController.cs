using Expresso.Core.Filtering;
using Expresso.Core.Projection;
using Expresso.Parsing;
using Expresso.Sample.Shared.DataAccess;
using Expresso.Sample.Shared.Filtering;
using Expresso.Sample.Shared.Models;
using Expresso.Sample.Shared.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Expresso.Sample.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
/// <summary>Lists authors and returns one author by id. <c>filter</c> and <c>sort</c> are Expresso query strings.</summary>
/// <param name="repository">Author store.</param>
/// <param name="filterParser">Expresso filter parser.</param>
/// <param name="sortDirectiveParser">Expresso sort parser.</param>
/// <param name="queryModels">Author query models and policy.</param>
/// <param name="logger">Policy violation logger.</param>
/// <param name="pagingParser">Expresso paging parser.</param>
/// <param name="projectionParser">Expresso projection parser.</param>
/// <param name="projectionModels">Public response projection catalogs.</param>
public sealed class AuthorsController(
    IRepository<Author> repository,
    IFilterParser filterParser,
    ISortDirectiveParser sortDirectiveParser,
    ControllerQueryModels<AuthorsController> queryModels,
    ILogger<AuthorsController> logger,
    IPagingDirectiveParser pagingParser,
    IProjectionDirectiveParser projectionParser,
    IRequestProjectionModelProvider projectionModels) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AuthorViewModel>), 200)]
    /// <summary>Returns authors that match <paramref name="filter"/>, ordered by <paramref name="sort"/>, optionally paged.</summary>
    /// <param name="filter">Expresso filter, or <see langword="null"/> to return every author.</param>
    /// <param name="sort">Expresso sort, or <see langword="null"/> for the repository default.</param>
    /// <param name="page">1-based page. Requires <paramref name="pageSize"/>.</param>
    /// <param name="pageSize">Page size. Without <paramref name="page"/>, this is the first page.</param>
    /// <param name="skip">Rows to skip when page size is omitted.</param>
    /// <param name="take">Maximum rows when page size is omitted.</param>
    /// <param name="fields">Selected response paths; null selects all public fields.</param>
    /// <param name="cancellationToken">Token that cancels the query.</param>
    /// <returns>200 with the authors, or 400 when the query string is invalid or both paging styles are set.</returns>
    public async Task<ActionResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>> GetAll(
        [FromQuery] string? filter,
        [FromQuery] string? sort,
        [FromQuery] string? page,
        [FromQuery(Name = "pagesize")] string? pageSize,
        [FromQuery] string? skip,
        [FromQuery] string? take,
        [FromQuery] string? fields,
        CancellationToken cancellationToken)
    {
        var parsed = QueryParametersParser.Parse(filter, sort, filterParser, sortDirectiveParser, queryModels.Filter, queryModels.Sort);
        var paging = QueryParametersParser.ParsePaging(page, pageSize, skip, take, pagingParser);
        var projection = QueryParametersParser.ParseProjection(fields, projectionParser,
            projectionModels.GetProjectionModel("author"), isPresent: ProjectionRequest.HasFields(Request));
        if (parsed.IsBadRequest || paging.IsBadRequest || projection.IsBadRequest)
        {
            if (parsed.PolicyViolation is { } violation)
                logger.LogWarning("Query policy rejected {Target}: {Kind}; path={Path}; rule={Rule}; limit={Limit}",
                    violation.Target, violation.Kind, violation.Path, violation.RuleText, violation.LimitName);
            return BadRequest();
        }

        var authors = await repository.GetAllAsync(parsed.FilterCriteria, parsed.SortDirective, paging.Paging, projection.Projection, cancellationToken);
        if (!paging.Paging.IsEmpty)
        {
            var total = await repository.CountAsync(parsed.FilterCriteria, cancellationToken);
            foreach (var header in PagingHeaders.Values(paging.Paging, total))
            {
                Response.Headers[header.Name] = header.Value;
            }
        }

        return Ok(authors);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AuthorViewModel), 200)]
    /// <summary>Returns one author.</summary>
    /// <param name="id">Author primary key.</param>
    /// <param name="fields">Selected response paths; null selects all public fields.</param>
    /// <param name="cancellationToken">Token that cancels the query.</param>
    /// <returns>200 with the author, 400 for invalid fields, or 404 when it does not exist.</returns>
    public async Task<ActionResult<IReadOnlyDictionary<string, object?>>> GetById(int id, [FromQuery] string? fields, CancellationToken cancellationToken)
    {
        var projection = QueryParametersParser.ParseProjection(fields, projectionParser,
            projectionModels.GetProjectionModel("author"), isPresent: ProjectionRequest.HasFields(Request));
        if (projection.IsBadRequest) return BadRequest();
        var author = await repository.GetByIdAsync(id, projection.Projection, cancellationToken);
        if (author is null)
        {
            return NotFound();
        }

        return Ok(author);
    }

}
