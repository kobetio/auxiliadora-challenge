using RentalPipeline.Domain.Entities;
using RentalPipeline.Domain.Enums;
using RentalPipeline.Domain.Exceptions;
using RentalPipeline.Domain.StateMachine;

namespace RentalPipeline.UnitTests.Domain.Entities;

public class RentalProposalTests
{
    private readonly ProposalStateMachine _stateMachine = new();

    [Fact]
    public void Constructor_NewProposal_StartsAsNewAndRecordsInitialHistoryEntry()
    {
        // Rule 3 & Rule 8: creation itself is recorded in history, with a null PreviousStatus
        // (there is nothing to transition from).
        var proposal = new RentalProposal(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(ProposalStatus.New, proposal.Status);
        var historyEntry = Assert.Single(proposal.StatusHistory);
        Assert.Null(historyEntry.PreviousStatus);
        Assert.Equal(ProposalStatus.New, historyEntry.NewStatus);
    }

    [Fact]
    public void ChangeStatus_ValidTransition_UpdatesStatusAndAddsHistoryEntry()
    {
        // Rule 8.
        var proposal = new RentalProposal(Guid.NewGuid(), Guid.NewGuid());

        proposal.ChangeStatus(ProposalStatus.CreditAnalysis, _stateMachine);

        Assert.Equal(ProposalStatus.CreditAnalysis, proposal.Status);
        Assert.Equal(2, proposal.StatusHistory.Count); // initial creation entry + this transition.
        var historyEntry = proposal.StatusHistory.Last();
        Assert.Equal(ProposalStatus.New, historyEntry.PreviousStatus);
        Assert.Equal(ProposalStatus.CreditAnalysis, historyEntry.NewStatus);
    }

    [Fact]
    public void ChangeStatus_MultipleTransitions_AccumulatesHistoryInOrder()
    {
        var proposal = new RentalProposal(Guid.NewGuid(), Guid.NewGuid());

        proposal.ChangeStatus(ProposalStatus.CreditAnalysis, _stateMachine);
        proposal.ChangeStatus(ProposalStatus.ContractIssued, _stateMachine);
        proposal.ChangeStatus(ProposalStatus.Signed, _stateMachine);
        proposal.ChangeStatus(ProposalStatus.Active, _stateMachine);

        Assert.Equal(ProposalStatus.Active, proposal.Status);
        Assert.Equal(5, proposal.StatusHistory.Count); // initial creation entry + 4 transitions.
        Assert.Equal(
            [ProposalStatus.New, ProposalStatus.CreditAnalysis, ProposalStatus.ContractIssued, ProposalStatus.Signed, ProposalStatus.Active],
            proposal.StatusHistory.Select(h => h.NewStatus));
    }

    [Fact]
    public void ChangeStatus_InvalidTransition_ThrowsDomainExceptionAndDoesNotChangeState()
    {
        // Rule 4: New -> Active is not allowed (skips states). Safety-net exception, never
        // expected to be hit in normal operation because the Application Service checks
        // ProposalStateMachine.CanTransition first.
        var proposal = new RentalProposal(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<DomainException>(() => proposal.ChangeStatus(ProposalStatus.Active, _stateMachine));
        Assert.Equal(ProposalStatus.New, proposal.Status);
        Assert.Single(proposal.StatusHistory); // only the initial creation entry remains.
    }

    [Theory]
    [InlineData(ProposalStatus.Rejected)]
    [InlineData(ProposalStatus.Cancelled)]
    public void ChangeStatus_FromNewToRejectedOrCancelled_Succeeds(ProposalStatus target)
    {
        // Rule 5.
        var proposal = new RentalProposal(Guid.NewGuid(), Guid.NewGuid());

        proposal.ChangeStatus(target, _stateMachine);

        Assert.Equal(target, proposal.Status);
    }
}
