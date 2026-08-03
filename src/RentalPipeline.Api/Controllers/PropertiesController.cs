using Microsoft.AspNetCore.Mvc;
using RentalPipeline.Api.ProblemDetails;
using RentalPipeline.Application.DTOs;
using RentalPipeline.Application.Interfaces;

namespace RentalPipeline.Api.Controllers;

/// <summary>
/// Manages rental properties.
/// </summary>
[ApiController]
[Route("properties")]
[Produces("application/json")]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _propertyService;

    /// <summary>Creates the controller with its required <see cref="IPropertyService"/> dependency.</summary>
    public PropertiesController(IPropertyService propertyService)
    {
        _propertyService = propertyService;
    }

    /// <summary>Creates a new property. Its status always starts as <c>Available</c>.</summary>
    /// <response code="201">The property was created.</response>
    /// <response code="400">The request failed validation.</response>
    [HttpPost]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PropertyDto>> Create([FromBody] CreatePropertyRequest request, CancellationToken cancellationToken)
    {
        var result = await _propertyService.CreateAsync(request, cancellationToken);
        return this.ToCreatedResult(result, nameof(GetById), dto => new { id = dto.Id });
    }

    /// <summary>
    /// Returns every property currently available in the rental market (excludes <c>Rented</c>
    /// properties, which are permanently removed from the listing).
    /// </summary>
    /// <response code="200">The list of properties.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PropertyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PropertyDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _propertyService.GetAllAsync(cancellationToken);
        return this.ToOkResult(result);
    }

    /// <summary>Returns a property regardless of status, including <c>Rented</c>.</summary>
    /// <response code="200">The property.</response>
    /// <response code="404">No property exists with the given id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _propertyService.GetByIdAsync(id, cancellationToken);
        return this.ToOkResult(result);
    }

    /// <summary>Updates a property's editable details.</summary>
    /// <response code="200">The updated property.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No property exists with the given id.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyDto>> Update(Guid id, [FromBody] UpdatePropertyRequest request, CancellationToken cancellationToken)
    {
        var result = await _propertyService.UpdateAsync(id, request, cancellationToken);
        return this.ToOkResult(result);
    }

    /// <summary>
    /// Deletes a property. Fails with <c>409 Conflict</c> if the property has associated rental
    /// proposals.
    /// </summary>
    /// <response code="204">The property was deleted.</response>
    /// <response code="404">No property exists with the given id.</response>
    /// <response code="409">The property has associated rental proposals and cannot be deleted.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _propertyService.DeleteAsync(id, cancellationToken);
        return this.ToNoContentResult(result);
    }
}
