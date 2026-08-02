namespace RentalPipeline.Application.Contracts;

/// <summary>
/// Published when a rental proposal transitions to <c>Active</c> (Rule 6). Payload shape follows
/// the "Publishing Event / ContractActivated / ProposalId / PropertyId / OccurredAt" example from
/// Architecture.md's Event Driven Architecture section.
/// </summary>
public sealed record ContractActivatedEvent(Guid ProposalId, Guid PropertyId, DateTime OccurredAt);
