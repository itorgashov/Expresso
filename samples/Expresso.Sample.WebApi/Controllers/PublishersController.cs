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
public sealed class PublishersController(
    IRepository<Publisher> repository,
    IFilterParser filterParser,
    ISortDirectiveParser sortDirectiveParser,
    IRequestFieldsInfoProvider requestFieldsProvider) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PublisherViewModel>>> GetAll(
        [FromQuery] string? filter,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        FilterCriteria? filterCriteria = null;
        if (filter is not null)
        {
            try
            {
                var validFields = requestFieldsProvider.GetValidFilterFields("publisher");
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
                var validFields = requestFieldsProvider.GetValidSortFields("publisher");
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

        var publishers = await repository.GetAllAsync(filterCriteria, sortDirective, cancellationToken);
        var viewModels = publishers.Select(p => new PublisherViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Country = p.Country,
            Location = p.Location,
        }).ToList();

        return Ok(viewModels);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PublisherViewModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var publisher = await repository.GetByIdAsync(id, cancellationToken);
        if (publisher is null)
        {
            return NotFound();
        }

        return Ok(new PublisherViewModel
        {
            Id = publisher.Id,
            Name = publisher.Name,
            Country = publisher.Country,
            Location = publisher.Location,
        });
    }
}
