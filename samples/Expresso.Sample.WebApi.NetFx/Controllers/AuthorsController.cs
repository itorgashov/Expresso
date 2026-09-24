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

[RoutePrefix("api/authors")]
/// <summary>Lists authors and returns one author by id. <c>filter</c> and <c>sort</c> are Expresso query strings.</summary>
public sealed class AuthorsController : ApiController
{
    private readonly IRepository<Author> _repository;
    private readonly IFilterParser _filterParser;
    private readonly ISortDirectiveParser _sortDirectiveParser;
    private readonly IRequestFieldsInfoProvider _requestFieldsProvider;

    /// <summary>Creates the controller.</summary>
    /// <param name="repository">Author store.</param>
    /// <param name="filterParser">Expresso filter parser.</param>
    /// <param name="sortDirectiveParser">Expresso sort parser.</param>
    /// <param name="requestFieldsProvider">Author field catalog.</param>
    public AuthorsController(
        IRepository<Author> repository,
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
    /// <summary>Returns authors that match <paramref name="filter"/>, ordered by <paramref name="sort"/>.</summary>
    /// <param name="filter">Expresso filter, or <see langword="null"/> to return every author.</param>
    /// <param name="sort">Expresso sort, or <see langword="null"/> for the repository default.</param>
    /// <param name="cancellationToken">Token that cancels the query.</param>
    /// <returns>200 with the authors, or 400 when the query string is invalid.</returns>
    public async Task<IHttpActionResult> GetAll(string? filter = null, string? sort = null, CancellationToken cancellationToken = default)
    {
        var parsed = QueryParametersParser.Parse(filter, sort, "author", _filterParser, _sortDirectiveParser, _requestFieldsProvider);
        if (parsed.IsBadRequest)
        {
            return BadRequest();
        }

        var authors = await _repository.GetAllAsync(parsed.FilterCriteria, parsed.SortDirective, cancellationToken);
        return Ok(authors.Select(ViewModelMapper.ToViewModel).ToList());
    }

    [HttpGet]
    [Route("{id:int}")]
    /// <summary>Returns one author.</summary>
    /// <param name="id">Author primary key.</param>
    /// <param name="cancellationToken">Token that cancels the query.</param>
    /// <returns>200 with the author, or 404 when it does not exist.</returns>
    public async Task<IHttpActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var author = await _repository.GetByIdAsync(id, cancellationToken);
        if (author is null)
        {
            return NotFound();
        }

        return Ok(ViewModelMapper.ToViewModel(author));
    }
}
