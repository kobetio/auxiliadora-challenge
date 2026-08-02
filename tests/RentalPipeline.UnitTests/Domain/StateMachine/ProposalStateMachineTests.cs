using RentalPipeline.Domain.Enums;
using RentalPipeline.Domain.Exceptions;
using RentalPipeline.Domain.StateMachine;

namespace RentalPipeline.UnitTests.Domain.StateMachine;

public class ProposalStateMachineTests
{
    private readonly ProposalStateMachine _sut = new();

    [Theory]
    [InlineData(ProposalStatus.New, ProposalStatus.CreditAnalysis)]
    [InlineData(ProposalStatus.CreditAnalysis, ProposalStatus.ContractIssued)]
    [InlineData(ProposalStatus.ContractIssued, ProposalStatus.Signed)]
    [InlineData(ProposalStatus.Signed, ProposalStatus.Active)]
    // Rule 5: Rejected/Cancelled are reachable from any state before Active.
    [InlineData(ProposalStatus.New, ProposalStatus.Rejected)]
    [InlineData(ProposalStatus.New, ProposalStatus.Cancelled)]
    [InlineData(ProposalStatus.CreditAnalysis, ProposalStatus.Rejected)]
    [InlineData(ProposalStatus.ContractIssued, ProposalStatus.Cancelled)]
    [InlineData(ProposalStatus.Signed, ProposalStatus.Rejected)]
    public void CanTransition_AllowedTransitions_ReturnsTrue(ProposalStatus from, ProposalStatus to)
    {
        Assert.True(_sut.CanTransition(from, to));
    }

    [Theory]
    // Rule 4: no skipping states.
    [InlineData(ProposalStatus.New, ProposalStatus.ContractIssued)]
    [InlineData(ProposalStatus.New, ProposalStatus.Signed)]
    [InlineData(ProposalStatus.New, ProposalStatus.Active)]
    [InlineData(ProposalStatus.CreditAnalysis, ProposalStatus.Signed)]
    [InlineData(ProposalStatus.CreditAnalysis, ProposalStatus.Active)]
    // No going backwards.
    [InlineData(ProposalStatus.CreditAnalysis, ProposalStatus.New)]
    [InlineData(ProposalStatus.Signed, ProposalStatus.CreditAnalysis)]
    // Terminal states never transition anywhere.
    [InlineData(ProposalStatus.Active, ProposalStatus.Rejected)]
    [InlineData(ProposalStatus.Active, ProposalStatus.Cancelled)]
    [InlineData(ProposalStatus.Rejected, ProposalStatus.New)]
    [InlineData(ProposalStatus.Cancelled, ProposalStatus.New)]
    public void CanTransition_DisallowedTransitions_ReturnsFalse(ProposalStatus from, ProposalStatus to)
    {
        Assert.False(_sut.CanTransition(from, to));
    }

    [Fact]
    public void ValidateTransition_InvalidTransition_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => _sut.ValidateTransition(ProposalStatus.New, ProposalStatus.Active));
    }

    [Fact]
    public void ValidateTransition_ValidTransition_DoesNotThrow()
    {
        var exception = Record.Exception(() => _sut.ValidateTransition(ProposalStatus.New, ProposalStatus.CreditAnalysis));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(ProposalStatus.Active)]
    [InlineData(ProposalStatus.Rejected)]
    [InlineData(ProposalStatus.Cancelled)]
    public void GetAllowedTransitions_TerminalState_ReturnsEmpty(ProposalStatus terminalStatus)
    {
        var allowed = _sut.GetAllowedTransitions(terminalStatus);

        Assert.Empty(allowed);
    }

    [Fact]
    public void GetAllowedTransitions_New_ReturnsCreditAnalysisRejectedAndCancelled()
    {
        var allowed = _sut.GetAllowedTransitions(ProposalStatus.New);

        Assert.Equal(
            [ProposalStatus.CreditAnalysis, ProposalStatus.Rejected, ProposalStatus.Cancelled],
            allowed);
    }
}
