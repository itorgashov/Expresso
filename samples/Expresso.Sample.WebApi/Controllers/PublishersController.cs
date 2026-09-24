using Expresso.Core.Filtering;
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
/// <param name="requestFieldsProvider">Publisher field catalog.</param>
public sealed class PublishersController(
    IRepository<Publisher> repository,
    IFilterParser filterParser,
    ISortDirectiveParser sortDirectiveParser,
    IRequestFieldsInfoProvider requestFieldsProvider) : ControllerBase
{
    [HttpGet]
    /// <summary>Returns publishers that match <paramref name="filter"/>, ordered by <paramref name="sort"/>.</summary>
    /// <param name="filter">Expresso filter, or <see langword="null"/> to return every publisher.</param>
    /// <param name="sort">Expresso sort, or <see langword="null"/> for the repository default.</param>
    /// <param name="cancellationToken">Token that cancels the query.</param>
    /// <returns>200 with the publishers, or 400 when the query string is invalid.</returns>
    public async Task<ActionResult<IReadOnlyList<PublisherViewModel>>> GetAll(
        [FromQuery] string? filter,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        var parsed = QueryParametersParser.Parse(filter, sort, "publisher", filterParser, sortDirectiveParser, requestFieldsProvider);
        if (parsed.IsBadRequest)
        {
            return BadRequest();
        }

        var publishers = await repository.GetAllAsync(parsed.FilterCriteria, parsed.SortDirective, cancellationToken);
        return Ok(publishers.Select(ViewModelMapper.ToViewModel).ToList());
    }

    [HttpGet("{id:int}")]
    /// <summary>Returns one publisher.</summary>
    /// <param name="id">Publisher primary key.</param>
    /// <param name="cancellationToken">Token that cancels the query.</param>
    /// <returns>200 with the publisher, or 404 when it does not exist.</returns>
    public async Task<ActionResult<PublisherViewModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var publisher = await repository.GetByIdAsync(id, cancellationToken);
        if (publisher is null)
        {
            return NotFound();
        }

        return Ok(ViewModelMapper.ToViewModel(publisher));
    }
}
