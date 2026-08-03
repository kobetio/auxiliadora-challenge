using Microsoft.AspNetCore.Mvc;
using RentalPipeline.Api.ProblemDetails;
using RentalPipeline.Application.DTOs;
using RentalPipeline.Application.Interfaces;

namespace RentalPipeline.Api.Controllers;

/// <summary>
/// Manages customers who can submit rental proposals.
/// </summary>
[ApiController]
[Route("customers")]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    /// <summary>Creates the controller with its required <see cref="ICustomerService"/> dependency.</summary>
    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>Creates a new customer.</summary>
    /// <response code="201">The customer was created.</response>
    /// <response code="400">The request failed validation.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await _customerService.CreateAsync(request, cancellationToken);
        return this.ToCreatedResult(result, nameof(GetById), dto => new { id = dto.Id });
    }

    /// <summary>Returns every customer.</summary>
    /// <response code="200">The list of customers.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _customerService.GetAllAsync(cancellationToken);
        return this.ToOkResult(result);
    }

    /// <summary>Returns a customer by id.</summary>
    /// <response code="200">The customer.</response>
    /// <response code="404">No customer exists with the given id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _customerService.GetByIdAsync(id, cancellationToken);
        return this.ToOkResult(result);
    }

    /// <summary>Updates a customer's editable details.</summary>
    /// <response code="200">The updated customer.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No customer exists with the given id.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await _customerService.UpdateAsync(id, request, cancellationToken);
        return this.ToOkResult(result);
    }

    /// <summary>
    /// Deletes a customer. Fails with <c>409 Conflict</c> if the customer has associated rental
    /// proposals.
    /// </summary>
    /// <response code="204">The customer was deleted.</response>
    /// <response code="404">No customer exists with the given id.</response>
    /// <response code="409">The customer has associated rental proposals and cannot be deleted.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _customerService.DeleteAsync(id, cancellationToken);
        return this.ToNoContentResult(result);
    }
}
