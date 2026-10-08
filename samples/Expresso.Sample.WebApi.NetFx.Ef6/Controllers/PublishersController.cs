using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Expresso.Core.Filtering;
using Expresso.Parsing;
using Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;
using Expresso.Sample.WebApi.NetFx.Ef6.Entities;
using Expresso.Sample.WebApi.NetFx.Ef6.Filtering;
using Expresso.Sample.WebApi.NetFx.Ef6.ViewModels;

namespace Expresso.Sample.WebApi.NetFx.Ef6.Controllers;

[RoutePrefix("api/publishers")]
public sealed class PublishersController : ApiController
{
    private readonly IRepository<Publisher> _repository;
    private readonly IFilterParser _filterParser;
    private readonly ISortDirectiveParser _sortDirectiveParser;
    private readonly IRequestFieldsInfoProvider _requestFieldsProvider;
    private readonly IPagingDirectiveParser _pagingParser;

    public PublishersController(
        IRepository<Publisher> repository,
        IFilterParser filterParser,
        ISortDirectiveParser sortDirectiveParser,
        IRequestFieldsInfoProvider requestFieldsProvider,
        IPagingDirectiveParser pagingParser)
    {
        _repository = repository;
        _filterParser = filterParser;
        _sortDirectiveParser = sortDirectiveParser;
        _requestFieldsProvider = requestFieldsProvider;
        _pagingParser = pagingParser;
    }

    [HttpGet]
    [Route("")]
    public async Task<IHttpActionResult> GetAll(string? filter = null, string? sort = null, string? page = null, string? pagesize = null, string? skip = null, string? take = null, CancellationToken cancellationToken = default)
    {
        var parsed = QueryParametersParser.Parse(filter, sort, "publisher", _filterParser, _sortDirectiveParser, _requestFieldsProvider);
        var paging = QueryParametersParser.ParsePaging(page, pagesize, skip, take, _pagingParser);
        if (parsed.IsBadRequest || paging.IsBadRequest)
        {
            return BadRequest();
        }

        var publishers = await _repository.GetAllAsync(parsed.FilterCriteria, parsed.SortDirective, paging.Paging, cancellationToken);
        var body = publishers.Select(ViewModelMapper.ToViewModel).ToList();
        if (paging.Paging.IsEmpty)
        {
            return Ok(body);
        }

        var total = await _repository.CountAsync(parsed.FilterCriteria, cancellationToken);
        return ResponseMessage(PagingHttp.WithHeaders(Request, body, paging.Paging, total));
    }

    [HttpGet]
    [Route("{id:int}")]
    public async Task<IHttpActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var publisher = await _repository.GetByIdAsync(id, cancellationToken);
        if (publisher is null)
        {
            return NotFound();
        }

        return Ok(ViewModelMapper.ToViewModel(publisher));
    }
}
