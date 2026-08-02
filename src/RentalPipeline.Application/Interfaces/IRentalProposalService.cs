using FluentResults;
using RentalPipeline.Application.DTOs;

namespace RentalPipeline.Application.Interfaces;

/// <summary>
/// Application service for the rental proposal pipeline.
/// </summary>
public interface IRentalProposalService
{
    Task<Result<RentalProposalDto>> CreateAsync(CreateProposalRequest request, CancellationToken cancellationToken = default);

    Task<Result<RentalProposalDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<RentalProposalDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a status transition (<c>PATCH /proposals/{id}/status</c>), enforcing Rule 4/5 via
    /// <c>ProposalStateMachine</c>, cascading Rule 6/7 to the related <see cref="RentalPipeline.Domain.Entities.Property"/>,
    /// recording history (Rule 8), and publishing a <c>ContractActivated</c> event when the new
    /// status is <c>Active</c>.
    /// </summary>
    Task<Result<RentalProposalDto>> UpdateStatusAsync(Guid id, UpdateProposalStatusRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the proposal's status transitions ordered by <c>ChangedAt</c> ascending
    /// (<c>GET /proposals/{id}/history</c>).
    /// </summary>
    Task<Result<IReadOnlyList<ProposalStatusHistoryDto>>> GetHistoryAsync(Guid proposalId, CancellationToken cancellationToken = default);
}
