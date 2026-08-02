using RentalPipeline.Domain.Entities;

namespace RentalPipeline.Domain.Interfaces;

public interface IRentalProposalRepository
{
    Task<RentalProposal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a proposal together with its <see cref="ProposalStatusHistory"/> records,
    /// needed by the <c>GET /proposals/{id}/history</c> endpoint.
    /// </summary>
    Task<RentalProposal?> GetByIdWithHistoryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RentalProposal>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(RentalProposal proposal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Used as a safe-delete guard: a <see cref="Entities.Property"/> with associated proposals
    /// must not be deleted (referential integrity, not part of the original spec).
    /// </summary>
    Task<bool> ExistsForPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Used as a safe-delete guard: a <see cref="Entities.Customer"/> with associated proposals
    /// must not be deleted (referential integrity, not part of the original spec).
    /// </summary>
    Task<bool> ExistsForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
}
