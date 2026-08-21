using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Parsing;
using Expresso.Sample.WebApi.DataAccess;
using Expresso.Sample.WebApi.Models;
using Expresso.Sample.WebApi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Expresso.Sample.WebApi.Controllers;

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
        FilterCriteria? filterCriteria = null;
        if (filter is not null)
        {
            try
            {
                var validFields = requestFieldsProvider.GetValidFilterFields("author");
                filterCriteria = filterParser.Parse(filter, validFields);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        SortDirective? sortDirective = null;
        if (sort is not null)
        {
            try
            {
                var validFields = requestFieldsProvider.GetValidSortFields("author");
                var rawSortDirective = sortDirectiveParser.Parse(sort, validFields);
                sortDirective = rawSortDirective.RemoveDuplicates();
                if (sortDirective.Items.Count < rawSortDirective.Items.Count)
                {
                    return BadRequest();
                }
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        var authors = await repository.GetAllAsync(filterCriteria, sortDirective, cancellationToken);
        var viewModels = authors.Select(a => new AuthorViewModel
        {
            Id = a.Id,
            FirstName = a.FirstName,
            LastName = a.LastName,
            DisplayName = a.DisplayName,
            DateOfBirth = a.DateOfBirth,
            DateOfDeath = a.DateOfDeath,
            CreatedAt = a.CreatedAt,
        }).ToList();

        return Ok(viewModels);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AuthorViewModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var author = await repository.GetByIdAsync(id, cancellationToken);
        if (author is null)
        {
            return NotFound();
        }

        return Ok(new AuthorViewModel
        {
            Id = author.Id,
            FirstName = author.FirstName,
            LastName = author.LastName,
            DisplayName = author.DisplayName,
            DateOfBirth = author.DateOfBirth,
            DateOfDeath = author.DateOfDeath,
            CreatedAt = author.CreatedAt,
        });
    }
}
