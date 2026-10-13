using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Expresso.Core.Filtering;
using Expresso.Core.Projection;
using Expresso.Parsing;
using Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;
using Expresso.Sample.WebApi.NetFx.Ef6.Entities;
using Expresso.Sample.WebApi.NetFx.Ef6.Filtering;
using Expresso.Sample.WebApi.NetFx.Ef6.ViewModels;

namespace Expresso.Sample.WebApi.NetFx.Ef6.Controllers;

[RoutePrefix("api/authors")]
public sealed class AuthorsController : ApiController
{
    private readonly IRepository<Author> _repository;
    private readonly IFilterParser _filterParser;
    private readonly ISortDirectiveParser _sortDirectiveParser;
    private readonly ControllerQueryModels<AuthorsController> _queryModels;
    private readonly IPagingDirectiveParser _pagingParser;
    private readonly IProjectionDirectiveParser _projectionParser;
    private readonly IRequestProjectionModelProvider _projectionModels;

    public AuthorsController(
        IRepository<Author> repository,
        IFilterParser filterParser,
        ISortDirectiveParser sortDirectiveParser,
        ControllerQueryModels<AuthorsController> queryModels,
        IPagingDirectiveParser pagingParser,
        IProjectionDirectiveParser projectionParser,
        IRequestProjectionModelProvider projectionModels)
    {
        _repository = repository;
        _filterParser = filterParser;
        _sortDirectiveParser = sortDirectiveParser;
        _queryModels = queryModels;
        _pagingParser = pagingParser;
        _projectionParser = projectionParser;
        _projectionModels = projectionModels;
    }

    [HttpGet]
    [System.Web.Http.Description.ResponseType(typeof(IReadOnlyList<AuthorViewModel>))]
    [Route("")]
    public async Task<IHttpActionResult> GetAll(string? filter = null, string? sort = null, string? page = null, string? pagesize = null, string? skip = null, string? take = null, string? fields = null, CancellationToken cancellationToken = default)
    {
        var parsed = QueryParametersParser.Parse(filter, sort, _filterParser, _sortDirectiveParser, _queryModels.Filter, _queryModels.Sort);
        var paging = QueryParametersParser.ParsePaging(page, pagesize, skip, take, _pagingParser);
        var projection = QueryParametersParser.ParseProjection(fields, _projectionParser,
            _projectionModels.GetProjectionModel("author"), isPresent: ProjectionRequest.HasFields(Request));
        if (parsed.IsBadRequest || paging.IsBadRequest || projection.IsBadRequest)
        {
            if (parsed.PolicyViolation is { } violation)
                System.Diagnostics.Trace.TraceWarning("Query policy rejected {0}: {1}; path={2}; rule={3}; limit={4}",
                    violation.Target, violation.Kind, violation.Path, violation.RuleText, violation.LimitName);
            return BadRequest();
        }

        var authors = await _repository.GetAllAsync(parsed.FilterCriteria, parsed.SortDirective, paging.Paging, projection.Projection, cancellationToken);
        var body = authors;
        if (paging.Paging.IsEmpty)
        {
            return Ok(body);
        }

        var total = await _repository.CountAsync(parsed.FilterCriteria, cancellationToken);
        return ResponseMessage(PagingHttp.WithHeaders(Request, body, paging.Paging, total));
    }

    [HttpGet]
    [Route("{id:int}")]
    [System.Web.Http.Description.ResponseType(typeof(AuthorViewModel))]
    public async Task<IHttpActionResult> GetById(int id, string? fields = null, CancellationToken cancellationToken = default)
    {
        var projection = QueryParametersParser.ParseProjection(fields, _projectionParser,
            _projectionModels.GetProjectionModel("author"), isPresent: ProjectionRequest.HasFields(Request));
        if (projection.IsBadRequest) return BadRequest();
        var author = await _repository.GetByIdAsync(id, projection.Projection, cancellationToken);
        if (author is null)
        {
            return NotFound();
        }

        return Ok(author);
    }

}
