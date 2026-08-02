using RentalPipeline.Domain.Enums;
using RentalPipeline.Domain.Exceptions;

namespace RentalPipeline.Domain.StateMachine;

/// <summary>
/// Centralizes the <see cref="ProposalStatus"/> transition rules (Rule 4 and Rule 5), so
/// Application Services never implement their own ad-hoc if/else transition logic.
/// </summary>
public class ProposalStateMachine
{
    private static readonly IReadOnlyDictionary<ProposalStatus, ProposalStatus[]> Transitions =
        new Dictionary<ProposalStatus, ProposalStatus[]>
        {
            // Rule 4: sequential path only, no skipping states.
            // Rule 5: Rejected/Cancelled are reachable from any state before Active.
            [ProposalStatus.New] = [ProposalStatus.CreditAnalysis, ProposalStatus.Rejected, ProposalStatus.Cancelled],
            [ProposalStatus.CreditAnalysis] = [ProposalStatus.ContractIssued, ProposalStatus.Rejected, ProposalStatus.Cancelled],
            [ProposalStatus.ContractIssued] = [ProposalStatus.Signed, ProposalStatus.Rejected, ProposalStatus.Cancelled],
            [ProposalStatus.Signed] = [ProposalStatus.Active, ProposalStatus.Rejected, ProposalStatus.Cancelled],

            // Active, Rejected and Cancelled are terminal states — no further transitions.
            [ProposalStatus.Active] = [],
            [ProposalStatus.Rejected] = [],
            [ProposalStatus.Cancelled] = [],
        };

    public bool CanTransition(ProposalStatus current, ProposalStatus target)
        => Transitions.TryGetValue(current, out var allowed) && allowed.Contains(target);

    /// <summary>
    /// Throws if the transition is invalid. This is a defense-in-depth safety net: callers
    /// (Application Services) are expected to check <see cref="CanTransition"/> first and
    /// translate an invalid transition into a <c>Result&lt;T&gt;</c> failure (400) instead of
    /// ever hitting this exception in normal operation.
    /// </summary>
    public void ValidateTransition(ProposalStatus current, ProposalStatus target)
    {
        if (!CanTransition(current, target))
        {
            throw new DomainException($"Cannot transition proposal from '{current}' to '{target}'.");
        }
    }

    public IReadOnlyList<ProposalStatus> GetAllowedTransitions(ProposalStatus current)
        => Transitions.TryGetValue(current, out var allowed) ? allowed : [];
}
