using RentalPipeline.Domain.Enums;

namespace RentalPipeline.Domain.Entities;

/// <summary>
/// Records a single status transition of a <see cref="RentalProposal"/>.
/// </summary>
public class ProposalStatusHistory
{
    public Guid Id { get; private set; }
    public Guid ProposalId { get; private set; }

    /// <summary>
    /// The status the proposal transitioned from, or <c>null</c> when this record represents the
    /// proposal's initial creation (there is no real "previous" status in that case).
    /// </summary>
    public ProposalStatus? PreviousStatus { get; private set; }

    public ProposalStatus NewStatus { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private ProposalStatusHistory()
    {
        // Required by EF Core.
    }

    public ProposalStatusHistory(Guid proposalId, ProposalStatus? previousStatus, ProposalStatus newStatus)
    {
        Id = Guid.NewGuid();
        ProposalId = proposalId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedAt = DateTime.UtcNow;
    }
}
