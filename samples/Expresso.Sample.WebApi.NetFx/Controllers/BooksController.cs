using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Expresso.Core.Filtering;
using Expresso.Core.Projection;
using Expresso.Parsing;
using Expresso.Sample.Shared.DataAccess;
using Expresso.Sample.Shared.Filtering;
using Expresso.Sample.Shared.Models;
using Expresso.Sample.Shared.ViewModels;

namespace Expresso.Sample.WebApi.NetFx.Controllers;

[RoutePrefix("api/books")]
/// <summary>Lists books and returns one book by id. <c>filter</c> and <c>sort</c> are Expresso query strings.</summary>
public sealed class BooksController : ApiController
{
    private readonly IRepository<Book> _repository;
    private readonly IFilterParser _filterParser;
    private readonly ISortDirectiveParser _sortDirectiveParser;
    private readonly ControllerQueryModels<BooksController> _queryModels;
    private readonly IPagingDirectiveParser _pagingParser;
    private readonly IProjectionDirectiveParser _projectionParser;
    private readonly IRequestProjectionModelProvider _projectionModels;

    /// <summary>Creates the controller.</summary>
    /// <param name="repository">Book store.</param>
    /// <param name="filterParser">Expresso filter parser.</param>
    /// <param name="sortDirectiveParser">Expresso sort parser.</param>
    /// <param name="queryModels">Book query models and policy.</param>
    /// <param name="pagingParser">Expresso paging parser.</param>
    /// <param name="projectionParser">Expresso projection parser.</param>
    /// <param name="projectionModels">Public response projection catalogs.</param>
    public BooksController(
        IRepository<Book> repository,
        IFilterParser filterParser,
        ISortDirectiveParser sortDirectiveParser,
        ControllerQueryModels<BooksController> queryModels,
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
    [System.Web.Http.Description.ResponseType(typeof(IReadOnlyList<BookViewModel>))]
    [Route("")]
    /// <summary>Returns books that match <paramref name="filter"/>, ordered by <paramref name="sort"/>, optionally paged.</summary>
    /// <param name="filter">Expresso filter, or <see langword="null"/> to return every book.</param>
    /// <param name="sort">Expresso sort, or <see langword="null"/> for the repository default.</param>
    /// <param name="page">1-based page. Requires <paramref name="pagesize"/>.</param>
    /// <param name="pagesize">Page size. Without <paramref name="page"/>, this is the first page.</param>
    /// <param name="skip">Rows to skip when page size is omitted.</param>
    /// <param name="take">Maximum rows when page size is omitted.</param>
    /// <param name="fields">Selected response paths; null selects all public fields.</param>
    /// <param name="cancellationToken">Token that cancels the query.</param>
    /// <returns>200 with the books, or 400 when the query string is invalid or both paging styles are set.</returns>
    public async Task<IHttpActionResult> GetAll(string? filter = null, string? sort = null, string? page = null, string? pagesize = null, string? skip = null, string? take = null, string? fields = null, CancellationToken cancellationToken = default)
    {
        var parsed = QueryParametersParser.Parse(filter, sort, _filterParser, _sortDirectiveParser, _queryModels.Filter, _queryModels.Sort);
        var paging = QueryParametersParser.ParsePaging(page, pagesize, skip, take, _pagingParser);
        var projection = QueryParametersParser.ParseProjection(fields, _projectionParser,
            _projectionModels.GetProjectionModel("book"), isPresent: ProjectionRequest.HasFields(Request));
        if (parsed.IsBadRequest || paging.IsBadRequest || projection.IsBadRequest)
        {
            if (parsed.PolicyViolation is { } violation)
                System.Diagnostics.Trace.TraceWarning("Query policy rejected {0}: {1}; path={2}; rule={3}; limit={4}",
                    violation.Target, violation.Kind, violation.Path, violation.RuleText, violation.LimitName);
            return BadRequest();
        }

        var books = await _repository.GetAllAsync(parsed.FilterCriteria, parsed.SortDirective, paging.Paging, projection.Projection, cancellationToken);
        var body = books;
        if (paging.Paging.IsEmpty)
        {
            return Ok(body);
        }

        var total = await _repository.CountAsync(parsed.FilterCriteria, cancellationToken);
        return ResponseMessage(PagingHttp.WithHeaders(Request, body, paging.Paging, total));
    }

    [HttpGet]
    [Route("{id:int}")]
    [System.Web.Http.Description.ResponseType(typeof(BookViewModel))]
    /// <summary>Returns one book.</summary>
    /// <param name="id">Book primary key.</param>
    /// <param name="fields">Selected response paths; null selects all public fields.</param>
    /// <param name="cancellationToken">Token that cancels the query.</param>
    /// <returns>200 with the book, 400 for invalid fields, or 404 when it does not exist.</returns>
    public async Task<IHttpActionResult> GetById(int id, string? fields = null, CancellationToken cancellationToken = default)
    {
        var projection = QueryParametersParser.ParseProjection(fields, _projectionParser,
            _projectionModels.GetProjectionModel("book"), isPresent: ProjectionRequest.HasFields(Request));
        if (projection.IsBadRequest) return BadRequest();
        var book = await _repository.GetByIdAsync(id, projection.Projection, cancellationToken);
        if (book is null)
        {
            return NotFound();
        }

        return Ok(book);
    }

}
