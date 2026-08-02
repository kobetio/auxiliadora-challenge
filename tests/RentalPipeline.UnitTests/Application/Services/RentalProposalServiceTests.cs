using NSubstitute;
using RentalPipeline.Application.Contracts;
using RentalPipeline.Application.DTOs;
using RentalPipeline.Application.Errors;
using RentalPipeline.Application.Interfaces;
using RentalPipeline.Application.Services;
using RentalPipeline.Domain.Entities;
using RentalPipeline.Domain.Enums;
using RentalPipeline.Domain.Interfaces;
using RentalPipeline.Domain.StateMachine;

namespace RentalPipeline.UnitTests.Application.Services;

public class RentalProposalServiceTests
{
    private readonly IRentalProposalRepository _rentalProposalRepository = Substitute.For<IRentalProposalRepository>();
    private readonly IPropertyRepository _propertyRepository = Substitute.For<IPropertyRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly ProposalStateMachine _stateMachine = new(); // real instance: pure, no dependencies.
    private readonly RentalProposalService _sut;

    public RentalProposalServiceTests()
    {
        _sut = new RentalProposalService(
            _rentalProposalRepository,
            _propertyRepository,
            _customerRepository,
            _unitOfWork,
            _stateMachine,
            _eventPublisher);
    }

    private static Property AvailableProperty() => new("Loft Centro", "Rua A, 123");

    private static Customer SomeCustomer() => new("Maria Silva", "maria@example.com", "+55 11 90000-0000");

    [Fact]
    public async Task CreateAsync_PropertyNotFound_ReturnsNotFoundError()
    {
        _propertyRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Property?)null);

        var result = await _sut.CreateAsync(new CreateProposalRequest(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsFailed);
        Assert.IsType<NotFoundError>(result.Errors[0]);
    }

    [Fact]
    public async Task CreateAsync_CustomerNotFound_ReturnsNotFoundError()
    {
        var property = AvailableProperty();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        _customerRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var result = await _sut.CreateAsync(new CreateProposalRequest(property.Id, Guid.NewGuid()));

        Assert.True(result.IsFailed);
        Assert.IsType<NotFoundError>(result.Errors[0]);
    }

    [Fact]
    public async Task CreateAsync_PropertyNotAvailable_ReturnsConflictError()
    {
        // Rule 2.
        var property = AvailableProperty();
        property.MarkAsInNegotiation();
        var customer = SomeCustomer();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await _sut.CreateAsync(new CreateProposalRequest(property.Id, customer.Id));

        Assert.True(result.IsFailed);
        Assert.IsType<ConflictError>(result.Errors[0]);
        await _rentalProposalRepository.DidNotReceive().AddAsync(Arg.Any<RentalProposal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_PropertyAvailable_CreatesProposalAsNewAndReservesProperty()
    {
        // Rule 2 & 3.
        var property = AvailableProperty();
        var customer = SomeCustomer();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await _sut.CreateAsync(new CreateProposalRequest(property.Id, customer.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(ProposalStatus.New, result.Value.Status);
        Assert.Equal(PropertyStatus.InNegotiation, property.Status);
        await _rentalProposalRepository.Received(1).AddAsync(Arg.Any<RentalProposal>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateStatusAsync_ProposalNotFound_ReturnsNotFoundError()
    {
        _rentalProposalRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RentalProposal?)null);

        var result = await _sut.UpdateStatusAsync(Guid.NewGuid(), new UpdateProposalStatusRequest(ProposalStatus.CreditAnalysis));

        Assert.True(result.IsFailed);
        Assert.IsType<NotFoundError>(result.Errors[0]);
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_ReturnsBusinessRuleViolationError()
    {
        // Rule 4: New -> Signed skips states.
        var property = AvailableProperty();
        property.MarkAsInNegotiation();
        var proposal = new RentalProposal(property.Id, Guid.NewGuid());
        _rentalProposalRepository.GetByIdAsync(proposal.Id, Arg.Any<CancellationToken>()).Returns(proposal);

        var result = await _sut.UpdateStatusAsync(proposal.Id, new UpdateProposalStatusRequest(ProposalStatus.Signed));

        Assert.True(result.IsFailed);
        Assert.IsType<BusinessRuleViolationError>(result.Errors[0]);
        Assert.Equal(ProposalStatus.New, proposal.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_SequentialValidTransition_UpdatesStatusAndRecordsHistory()
    {
        // Rule 4 & 8.
        var property = AvailableProperty();
        property.MarkAsInNegotiation();
        var proposal = new RentalProposal(property.Id, Guid.NewGuid());
        _rentalProposalRepository.GetByIdAsync(proposal.Id, Arg.Any<CancellationToken>()).Returns(proposal);
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _sut.UpdateStatusAsync(proposal.Id, new UpdateProposalStatusRequest(ProposalStatus.CreditAnalysis));

        Assert.True(result.IsSuccess);
        Assert.Equal(ProposalStatus.CreditAnalysis, proposal.Status);
        Assert.Equal(2, proposal.StatusHistory.Count); // initial creation entry + this transition.
        var historyEntry = proposal.StatusHistory.Last();
        Assert.Equal(ProposalStatus.New, historyEntry.PreviousStatus);
        Assert.Equal(ProposalStatus.CreditAnalysis, historyEntry.NewStatus);
        // Intermediate transitions must not touch the property status.
        Assert.Equal(PropertyStatus.InNegotiation, property.Status);
    }

    [Theory]
    [InlineData(ProposalStatus.Rejected)]
    [InlineData(ProposalStatus.Cancelled)]
    public async Task UpdateStatusAsync_ToRejectedOrCancelled_ReleasesPropertyBackToAvailable(ProposalStatus target)
    {
        // Rule 5 & 7.
        var property = AvailableProperty();
        property.MarkAsInNegotiation();
        var proposal = new RentalProposal(property.Id, Guid.NewGuid());
        _rentalProposalRepository.GetByIdAsync(proposal.Id, Arg.Any<CancellationToken>()).Returns(proposal);
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _sut.UpdateStatusAsync(proposal.Id, new UpdateProposalStatusRequest(target));

        Assert.True(result.IsSuccess);
        Assert.Equal(target, proposal.Status);
        Assert.Equal(PropertyStatus.Available, property.Status);
        await _eventPublisher.DidNotReceive().PublishAsync(Arg.Any<ContractActivatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateStatusAsync_ToActive_MarksPropertyRentedAndPublishesContractActivatedEvent()
    {
        // Rule 6 + event publishing.
        var property = AvailableProperty();
        property.MarkAsInNegotiation();
        var proposal = new RentalProposal(property.Id, Guid.NewGuid());
        proposal.ChangeStatus(ProposalStatus.CreditAnalysis, _stateMachine);
        proposal.ChangeStatus(ProposalStatus.ContractIssued, _stateMachine);
        proposal.ChangeStatus(ProposalStatus.Signed, _stateMachine);
        _rentalProposalRepository.GetByIdAsync(proposal.Id, Arg.Any<CancellationToken>()).Returns(proposal);
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _sut.UpdateStatusAsync(proposal.Id, new UpdateProposalStatusRequest(ProposalStatus.Active));

        Assert.True(result.IsSuccess);
        Assert.Equal(ProposalStatus.Active, proposal.Status);
        Assert.Equal(PropertyStatus.Rented, property.Status);
        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<ContractActivatedEvent>(e => e != null && e.ProposalId == proposal.Id && e.PropertyId == property.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHistoryAsync_ProposalNotFound_ReturnsNotFoundError()
    {
        _rentalProposalRepository.GetByIdWithHistoryAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RentalProposal?)null);

        var result = await _sut.GetHistoryAsync(Guid.NewGuid());

        Assert.True(result.IsFailed);
        Assert.IsType<NotFoundError>(result.Errors[0]);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsHistoryOrderedByChangedAt()
    {
        var property = AvailableProperty();
        var proposal = new RentalProposal(property.Id, Guid.NewGuid());
        proposal.ChangeStatus(ProposalStatus.CreditAnalysis, _stateMachine);
        proposal.ChangeStatus(ProposalStatus.ContractIssued, _stateMachine);
        _rentalProposalRepository.GetByIdWithHistoryAsync(proposal.Id, Arg.Any<CancellationToken>()).Returns(proposal);

        var result = await _sut.GetHistoryAsync(proposal.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count); // initial creation entry + 2 transitions.
        Assert.Null(result.Value[0].PreviousStatus);
        Assert.Equal(ProposalStatus.New, result.Value[1].PreviousStatus);
        Assert.Equal(ProposalStatus.CreditAnalysis, result.Value[2].PreviousStatus);
    }
}
