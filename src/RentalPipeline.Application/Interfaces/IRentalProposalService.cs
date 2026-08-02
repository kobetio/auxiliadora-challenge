using FluentResults;
using RentalPipeline.Application.DTOs;

namespace RentalPipeline.Application.Interfaces;

/// <summary>
/// Application service for the rental proposal pipeline.
/// </summary>
/// <remarks>
/// <c>UpdateStatusAsync</c> (backing <c>PATCH /proposals/{id}/status</c>) is intentionally not
/// part of this interface yet: it depends on <c>ProposalStateMachine</c> and the
/// status-changing behavior on the <c>RentalProposal</c> aggregate, both introduced in Phase 4
/// together with their implementation, to avoid a half-built method now.
/// </remarks>
public interface IRentalProposalService
{
    Task<Result<RentalProposalDto>> CreateAsync(CreateProposalRequest request, CancellationToken cancellationToken = default);

    Task<Result<RentalProposalDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<RentalProposalDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the proposal's status transitions ordered by <c>ChangedAt</c> ascending
    /// (<c>GET /proposals/{id}/history</c>).
    /// </summary>
    Task<Result<IReadOnlyList<ProposalStatusHistoryDto>>> GetHistoryAsync(Guid proposalId, CancellationToken cancellationToken = default);
}
