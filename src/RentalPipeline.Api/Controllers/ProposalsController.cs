using Microsoft.AspNetCore.Mvc;
using RentalPipeline.Api.ProblemDetails;
using RentalPipeline.Application.DTOs;
using RentalPipeline.Application.Interfaces;

namespace RentalPipeline.Api.Controllers;

/// <summary>
/// Manages the rental proposal pipeline: creation, status transitions and history.
/// </summary>
[ApiController]
[Route("proposals")]
[Produces("application/json")]
public class ProposalsController : ControllerBase
{
    private readonly IRentalProposalService _rentalProposalService;

    /// <summary>Creates the controller with its required <see cref="IRentalProposalService"/> dependency.</summary>
    public ProposalsController(IRentalProposalService rentalProposalService)
    {
        _rentalProposalService = rentalProposalService;
    }

    /// <summary>
    /// Creates a rental proposal. Validates that the property is <c>Available</c> and that the
    /// customer exists, reserves the property (<c>Available</c> → <c>InNegotiation</c>), and
    /// creates the proposal starting as <c>New</c>.
    /// </summary>
    /// <response code="201">The proposal was created.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">The property or the customer was not found.</response>
    /// <response code="409">The property is not available for a new proposal.</response>
    [HttpPost]
    [ProducesResponseType(typeof(RentalProposalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RentalProposalDto>> Create([FromBody] CreateProposalRequest request, CancellationToken cancellationToken)
    {
        var result = await _rentalProposalService.CreateAsync(request, cancellationToken);
        return this.ToCreatedResult(result, nameof(GetById), dto => new { id = dto.Id });
    }

    /// <summary>
    /// Applies a status transition. Validates the transition against the state machine, cascades
    /// the resulting property status change, records history, and publishes a
    /// <c>ContractActivated</c> event when the new status is <c>Active</c>.
    /// </summary>
    /// <response code="200">The updated proposal.</response>
    /// <response code="400">The requested status transition is not allowed.</response>
    /// <response code="404">No proposal exists with the given id.</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(RentalProposalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RentalProposalDto>> UpdateStatus(Guid id, [FromBody] UpdateProposalStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _rentalProposalService.UpdateStatusAsync(id, request, cancellationToken);
        return this.ToOkResult(result);
    }

    /// <summary>Returns a proposal by id.</summary>
    /// <response code="200">The proposal.</response>
    /// <response code="404">No proposal exists with the given id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RentalProposalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RentalProposalDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _rentalProposalService.GetByIdAsync(id, cancellationToken);
        return this.ToOkResult(result);
    }

    /// <summary>Returns every proposal.</summary>
    /// <response code="200">The list of proposals.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RentalProposalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RentalProposalDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _rentalProposalService.GetAllAsync(cancellationToken);
        return this.ToOkResult(result);
    }

    /// <summary>
    /// Returns every status transition of a proposal (including its initial creation), ordered by
    /// <c>ChangedAt</c> ascending.
    /// </summary>
    /// <response code="200">The proposal's status history.</response>
    /// <response code="404">No proposal exists with the given id.</response>
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<ProposalStatusHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProposalStatusHistoryDto>>> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await _rentalProposalService.GetHistoryAsync(id, cancellationToken);
        return this.ToOkResult(result);
    }
}
