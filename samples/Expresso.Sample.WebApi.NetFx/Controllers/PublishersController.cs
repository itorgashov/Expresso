using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Expresso.Core.Filtering;
using Expresso.Parsing;
using Expresso.Sample.Shared.DataAccess;
using Expresso.Sample.Shared.Filtering;
using Expresso.Sample.Shared.Models;
using Expresso.Sample.Shared.ViewModels;

namespace Expresso.Sample.WebApi.NetFx.Controllers;

[RoutePrefix("api/publishers")]
/// <summary>Lists publishers and returns one publisher by id. <c>filter</c> and <c>sort</c> are Expresso query strings.</summary>
public sealed class PublishersController : ApiController
{
    private readonly IRepository<Publisher> _repository;
    private readonly IFilterParser _filterParser;
    private readonly ISortDirectiveParser _sortDirectiveParser;
    private readonly IRequestFieldsInfoProvider _requestFieldsProvider;

    /// <summary>Creates the controller.</summary>
    /// <param name="repository">Publisher store.</param>
    /// <param name="filterParser">Expresso filter parser.</param>
    /// <param name="sortDirectiveParser">Expresso sort parser.</param>
    /// <param name="requestFieldsProvider">Publisher field catalog.</param>
    public PublishersController(
        IRepository<Publisher> repository,
        IFilterParser filterParser,
        ISortDirectiveParser sortDirectiveParser,
        IRequestFieldsInfoProvider requestFieldsProvider)
    {
        _repository = repository;
        _filterParser = filterParser;
        _sortDirectiveParser = sortDirectiveParser;
        _requestFieldsProvider = requestFieldsProvider;
    }

    [HttpGet]
    [Route("")]
    /// <summary>Returns publishers that match <paramref name="filter"/>, ordered by <paramref name="sort"/>.</summary>
    /// <param name="filter">Expresso filter, or <see langword="null"/> to return every publisher.</param>
    /// <param name="sort">Expresso sort, or <see langword="null"/> for the repository default.</param>
    /// <param name="cancellationToken">Token that cancels the query.</param>
    /// <returns>200 with the publishers, or 400 when the query string is invalid.</returns>
    public async Task<IHttpActionResult> GetAll(string? filter = null, string? sort = null, CancellationToken cancellationToken = default)
    {
        var parsed = QueryParametersParser.Parse(filter, sort, "publisher", _filterParser, _sortDirectiveParser, _requestFieldsProvider);
        if (parsed.IsBadRequest)
        {
            return BadRequest();
        }

        var publishers = await _repository.GetAllAsync(parsed.FilterCriteria, parsed.SortDirective, cancellationToken);
        return Ok(publishers.Select(ViewModelMapper.ToViewModel).ToList());
    }

    [HttpGet]
    [Route("{id:int}")]
    /// <summary>Returns one publisher.</summary>
    /// <param name="id">Publisher primary key.</param>
    /// <param name="cancellationToken">Token that cancels the query.</param>
    /// <returns>200 with the publisher, or 404 when it does not exist.</returns>
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
