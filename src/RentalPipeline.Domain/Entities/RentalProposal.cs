using RentalPipeline.Domain.Enums;
using RentalPipeline.Domain.StateMachine;

namespace RentalPipeline.Domain.Entities;

/// <summary>
/// Represents a rental proposal. This is the Aggregate Root of the rental pipeline:
/// it owns its own status transitions and the creation of <see cref="ProposalStatusHistory"/>
/// records. Cross-aggregate side effects (updating the related <see cref="Property"/> status,
/// publishing events) are coordinated by the Application layer, since a single aggregate must
/// not directly mutate another aggregate.
/// </summary>
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

        // Rule 8: the initial status is itself recorded as a history entry (PreviousStatus null,
        // since there is nothing to transition from), so the full lifecycle — including creation —
        // is always visible via GET /proposals/{id}/history.
        _statusHistory.Add(new ProposalStatusHistory(Id, previousStatus: null, newStatus: Status));
    }

    /// <summary>
    /// Transitions the proposal to <paramref name="newStatus"/> and records the transition in
    /// <see cref="StatusHistory"/> (Rule 8). The transition itself is validated by
    /// <paramref name="stateMachine"/> (Rule 4/5) as a defense-in-depth safety net — callers are
    /// expected to have already checked <see cref="ProposalStateMachine.CanTransition"/> and
    /// turned an invalid transition into a <c>Result&lt;T&gt;</c> failure before reaching here.
    /// </summary>
    public void ChangeStatus(ProposalStatus newStatus, ProposalStateMachine stateMachine)
    {
        stateMachine.ValidateTransition(Status, newStatus);

        var previousStatus = Status;
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        _statusHistory.Add(new ProposalStatusHistory(Id, previousStatus, newStatus));
    }
}
