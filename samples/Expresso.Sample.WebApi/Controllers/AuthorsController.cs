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
/// <summary>Lists authors and returns one author by id. <c>filter</c> and <c>sort</c> are Expresso query strings.</summary>
/// <param name="repository">Author store.</param>
/// <param name="filterParser">Expresso filter parser.</param>
/// <param name="sortDirectiveParser">Expresso sort parser.</param>
/// <param name="requestFieldsProvider">Author field catalog.</param>
public sealed class AuthorsController(
    IRepository<Author> repository,
    IFilterParser filterParser,
    ISortDirectiveParser sortDirectiveParser,
    IRequestFieldsInfoProvider requestFieldsProvider) : ControllerBase
{
    [HttpGet]
    /// <summary>Returns authors that match <paramref name="filter"/>, ordered by <paramref name="sort"/>.</summary>
    /// <param name="filter">Expresso filter, or <see langword="null"/> to return every author.</param>
    /// <param name="sort">Expresso sort, or <see langword="null"/> for the repository default.</param>
    /// <param name="cancellationToken">Token that cancels the query.</param>
    /// <returns>200 with the authors, or 400 when the query string is invalid.</returns>
    public async Task<ActionResult<IReadOnlyList<AuthorViewModel>>> GetAll(
        [FromQuery] string? filter,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        var parsed = QueryParametersParser.Parse(filter, sort, "author", filterParser, sortDirectiveParser, requestFieldsProvider);
        if (parsed.IsBadRequest)
        {
            return BadRequest();
        }

        var authors = await repository.GetAllAsync(parsed.FilterCriteria, parsed.SortDirective, cancellationToken);
        return Ok(authors.Select(ViewModelMapper.ToViewModel).ToList());
    }

    [HttpGet("{id:int}")]
    /// <summary>Returns one author.</summary>
    /// <param name="id">Author primary key.</param>
    /// <param name="cancellationToken">Token that cancels the query.</param>
    /// <returns>200 with the author, or 404 when it does not exist.</returns>
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
