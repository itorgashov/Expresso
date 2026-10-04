using Expresso.Core.Filtering;
using Expresso.Parsing;
using Expresso.Sample.WebApi.EfCore.DataAccess;
using Expresso.Sample.WebApi.EfCore.Entities;
using Expresso.Sample.WebApi.EfCore.Filtering;
using Expresso.Sample.WebApi.EfCore.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Expresso.Sample.WebApi.EfCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthorsController(
    IRepository<Author> repository,
    IFilterParser filterParser,
    ISortDirectiveParser sortDirectiveParser,
    IRequestFieldsInfoProvider requestFieldsProvider) : ControllerBase
{
    [HttpGet]
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
