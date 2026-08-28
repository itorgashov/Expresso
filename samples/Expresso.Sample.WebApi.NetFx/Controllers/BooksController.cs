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

[RoutePrefix("api/books")]
public sealed class BooksController : ApiController
{
    private readonly IRepository<Book> _repository;
    private readonly IFilterParser _filterParser;
    private readonly ISortDirectiveParser _sortDirectiveParser;
    private readonly IRequestFieldsInfoProvider _requestFieldsProvider;

    public BooksController(
        IRepository<Book> repository,
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
    public async Task<IHttpActionResult> GetAll(string? filter = null, string? sort = null, CancellationToken cancellationToken = default)
    {
        var parsed = QueryParametersParser.Parse(filter, sort, "book", _filterParser, _sortDirectiveParser, _requestFieldsProvider);
        if (parsed.IsBadRequest)
        {
            return BadRequest();
        }

        var books = await _repository.GetAllAsync(parsed.FilterCriteria, parsed.SortDirective, cancellationToken);
        return Ok(books.Select(ViewModelMapper.ToViewModel).ToList());
    }

    [HttpGet]
    [Route("{id:int}")]
    public async Task<IHttpActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var book = await _repository.GetByIdAsync(id, cancellationToken);
        if (book is null)
        {
            return NotFound();
        }

        return Ok(ViewModelMapper.ToViewModel(book));
    }
}
