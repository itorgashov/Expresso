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
/// <summary>Lists publishers and returns one publisher by id. <c>filter</c> and <c>sort</c> are Expresso query strings.</summary>
/// <param name="repository">Publisher store.</param>
/// <param name="filterParser">Expresso filter parser.</param>
/// <param name="sortDirectiveParser">Expresso sort parser.</param>
/// <param name="queryModels">Publisher query models and policy.</param>
/// <param name="logger">Policy violation logger.</param>
/// <param name="pagingParser">Expresso paging parser.</param>
/// <param name="projectionParser">Expresso projection parser.</param>
/// <param name="projectionModels">Public response projection catalogs.</param>
public sealed class PublishersController(
    IRepository<Publisher> repository,
    IFilterParser filterParser,
    ISortDirectiveParser sortDirectiveParser,
    ControllerQueryModels<PublishersController> queryModels,
    ILogger<PublishersController> logger,
    IPagingDirectiveParser pagingParser,
    IProjectionDirectiveParser projectionParser,
    IRequestProjectionModelProvider projectionModels) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PublisherViewModel>), 200)]
    /// <summary>Returns publishers that match <paramref name="filter"/>, ordered by <paramref name="sort"/>, optionally paged.</summary>
    /// <param name="filter">Expresso filter, or <see langword="null"/> to return every publisher.</param>
    /// <param name="sort">Expresso sort, or <see langword="null"/> for the repository default.</param>
    /// <param name="page">1-based page. Requires <paramref name="pageSize"/>.</param>
    /// <param name="pageSize">Page size. Without <paramref name="page"/>, this is the first page.</param>
    /// <param name="skip">Rows to skip when page size is omitted.</param>
    /// <param name="take">Maximum rows when page size is omitted.</param>
    /// <param name="fields">Selected response paths; null selects all public fields.</param>
    /// <param name="cancellationToken">Token that cancels the query.</param>
    /// <returns>200 with the publishers, or 400 when the query string is invalid or both paging styles are set.</returns>
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
            projectionModels.GetProjectionModel("publisher"), isPresent: ProjectionRequest.HasFields(Request));
        if (parsed.IsBadRequest || paging.IsBadRequest || projection.IsBadRequest)
        {
            if (parsed.PolicyViolation is { } violation)
                logger.LogWarning("Query policy rejected {Target}: {Kind}; path={Path}; rule={Rule}; limit={Limit}",
                    violation.Target, violation.Kind, violation.Path, violation.RuleText, violation.LimitName);
            return BadRequest();
        }

        var publishers = await repository.GetAllAsync(parsed.FilterCriteria, parsed.SortDirective, paging.Paging, projection.Projection, cancellationToken);
        if (!paging.Paging.IsEmpty)
        {
            var total = await repository.CountAsync(parsed.FilterCriteria, cancellationToken);
            foreach (var header in PagingHeaders.Values(paging.Paging, total))
            {
                Response.Headers[header.Name] = header.Value;
            }
        }

        return Ok(publishers);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PublisherViewModel), 200)]
    /// <summary>Returns one publisher.</summary>
    /// <param name="id">Publisher primary key.</param>
    /// <param name="fields">Selected response paths; null selects all public fields.</param>
    /// <param name="cancellationToken">Token that cancels the query.</param>
    /// <returns>200 with the publisher, 400 for invalid fields, or 404 when it does not exist.</returns>
    public async Task<ActionResult<IReadOnlyDictionary<string, object?>>> GetById(int id, [FromQuery] string? fields, CancellationToken cancellationToken)
    {
        var projection = QueryParametersParser.ParseProjection(fields, projectionParser,
            projectionModels.GetProjectionModel("publisher"), isPresent: ProjectionRequest.HasFields(Request));
        if (projection.IsBadRequest) return BadRequest();
        var publisher = await repository.GetByIdAsync(id, projection.Projection, cancellationToken);
        if (publisher is null)
        {
            return NotFound();
        }

        return Ok(publisher);
    }

}
