using RentalPipeline.Domain.Enums;

namespace RentalPipeline.Domain.Entities;

/// <summary>
/// Represents a rental proposal. This is the Aggregate Root of the rental pipeline:
/// it coordinates status transitions, the related <see cref="Property"/> status,
/// and the creation of <see cref="ProposalStatusHistory"/> records.
/// </summary>
/// <remarks>
/// Status transition behavior (respecting the state machine and generating
/// history records) is added in a later implementation phase, once
/// <c>ProposalStateMachine</c> exists. This phase only establishes the shape
/// of the aggregate and its persistence mapping.
/// </remarks>
public class RentalProposal
{
    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public ProposalStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Optimistic concurrency token. Mapped by the Npgsql EF Core provider to the
    /// PostgreSQL <c>xmin</c> system column, which is updated automatically on every row change.
    /// </summary>
    public uint RowVersion { get; private set; }

    private readonly List<ProposalStatusHistory> _statusHistory = [];

    /// <summary>
    /// All recorded status transitions for this proposal, in the order they were added.
    /// </summary>
    public IReadOnlyCollection<ProposalStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    private RentalProposal()
    {
        // Required by EF Core.
    }

    public RentalProposal(Guid propertyId, Guid customerId)
    {
        Id = Guid.NewGuid();
        PropertyId = propertyId;
        CustomerId = customerId;
        Status = ProposalStatus.New; // Rule 3: a newly created proposal starts as New.
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }
}
