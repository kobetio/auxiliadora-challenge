using FluentResults;
using Microsoft.Extensions.Logging;
using RentalPipeline.Application.Contracts;
using RentalPipeline.Application.DTOs;
using RentalPipeline.Application.Errors;
using RentalPipeline.Application.Interfaces;
using RentalPipeline.Application.Mappings;
using RentalPipeline.Domain.Entities;
using RentalPipeline.Domain.Enums;
using RentalPipeline.Domain.Interfaces;
using RentalPipeline.Domain.StateMachine;

namespace RentalPipeline.Application.Services;

public class RentalProposalService : IRentalProposalService
{
    private readonly IRentalProposalRepository _rentalProposalRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ProposalStateMachine _stateMachine;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<RentalProposalService> _logger;

    public RentalProposalService(
        IRentalProposalRepository rentalProposalRepository,
        IPropertyRepository propertyRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ProposalStateMachine stateMachine,
        IEventPublisher eventPublisher,
        ILogger<RentalProposalService> logger)
    {
        _rentalProposalRepository = rentalProposalRepository;
        _propertyRepository = propertyRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _stateMachine = stateMachine;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result<RentalProposalDto>> CreateAsync(CreateProposalRequest request, CancellationToken cancellationToken = default)
    {
        // The customer check is a simple existence read that plays no part in the property
        // reservation race, so it doesn't need to run inside the Serializable transaction below.
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Fail<RentalProposalDto>(new NotFoundError($"Customer '{request.CustomerId}' was not found."));
        }

        // Rule 2/3 critical section (Architecture.md Section 9 "Transaction Flow"): reading the
        // Property's availability and reserving it must happen inside a single Serializable
        // transaction, so PostgreSQL detects and aborts one side of two concurrent requests racing
        // to reserve the same Property, instead of both succeeding (write-skew anomaly). A
        // serialization failure surfaces as a Postgres exception, mapped to 409 by
        // ExceptionHandlingMiddleware.
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
        {
            var property = await _propertyRepository.GetByIdAsync(request.PropertyId, ct);
            if (property is null)
            {
                return Result.Fail<RentalProposalDto>(new NotFoundError($"Property '{request.PropertyId}' was not found."));
            }

            // Rule 2: a proposal can only be created against an Available property.
            if (property.Status != PropertyStatus.Available)
            {
                _logger.LogWarning(
                    "Property unavailable: property {PropertyId} has status {PropertyStatus}, rejecting new proposal.",
                    property.Id,
                    property.Status);

                return Result.Fail<RentalProposalDto>(new ConflictError(
                    $"Property '{property.Id}' is not available for a new proposal (current status: '{property.Status}')."));
            }

            // Rule 3: creating the proposal reserves the property (Available -> InNegotiation) and
            // the proposal itself starts as New. Both changes are persisted by the same
            // SaveChangesAsync call below and committed together as part of the transaction.
            property.MarkAsInNegotiation();

            var proposal = new RentalProposal(property.Id, customer.Id);

            await _rentalProposalRepository.AddAsync(proposal, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Proposal created: {ProposalId} for property {PropertyId} and customer {CustomerId}.",
                proposal.Id,
                proposal.PropertyId,
                proposal.CustomerId);

            return Result.Ok(proposal.ToDto());
        }, cancellationToken);
    }

    public async Task<Result<RentalProposalDto>> UpdateStatusAsync(Guid id, UpdateProposalStatusRequest request, CancellationToken cancellationToken = default)
    {
        var proposal = await _rentalProposalRepository.GetByIdAsync(id, cancellationToken);
        if (proposal is null)
        {
            return Result.Fail<RentalProposalDto>(new NotFoundError($"Rental proposal '{id}' was not found."));
        }

        // Rule 4/5: validate the transition before mutating anything, so an invalid request
        // (e.g. skipping states, or leaving a terminal state) surfaces as a 409/400 Result failure
        // instead of the DomainException safety net inside RentalProposal.ChangeStatus.
        if (!_stateMachine.CanTransition(proposal.Status, request.NewStatus))
        {
            _logger.LogWarning(
                "Invalid transition: proposal {ProposalId} cannot go from {CurrentStatus} to {TargetStatus}.",
                proposal.Id,
                proposal.Status,
                request.NewStatus);

            return Result.Fail<RentalProposalDto>(new BusinessRuleViolationError(
                $"Cannot transition proposal from '{proposal.Status}' to '{request.NewStatus}'."));
        }

        var property = await _propertyRepository.GetByIdAsync(proposal.PropertyId, cancellationToken);
        if (property is null)
        {
            // Referential integrity (FK, Restrict delete) guarantees this never happens in practice.
            throw new Domain.Exceptions.DomainException(
                $"Property '{proposal.PropertyId}' referenced by proposal '{proposal.Id}' was not found.");
        }

        var previousStatus = proposal.Status;
        proposal.ChangeStatus(request.NewStatus, _stateMachine); // Rule 8: records history internally.

        switch (request.NewStatus)
        {
            case ProposalStatus.Active:
                property.MarkAsRented(); // Rule 6.
                break;
            case ProposalStatus.Rejected:
            case ProposalStatus.Cancelled:
                property.MarkAsAvailable(); // Rule 7.
                break;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Status changed: proposal {ProposalId} went from {PreviousStatus} to {NewStatus}.",
            proposal.Id,
            previousStatus,
            proposal.Status);

        if (request.NewStatus == ProposalStatus.Active)
        {
            await _eventPublisher.PublishAsync(
                new ContractActivatedEvent(proposal.Id, proposal.PropertyId, DateTime.UtcNow),
                cancellationToken);

            _logger.LogInformation("Event published: ContractActivated for proposal {ProposalId}.", proposal.Id);
        }

        return Result.Ok(proposal.ToDto());
    }

    public async Task<Result<RentalProposalDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var proposal = await _rentalProposalRepository.GetByIdAsync(id, cancellationToken);

        return proposal is null
            ? Result.Fail<RentalProposalDto>(new NotFoundError($"Rental proposal '{id}' was not found."))
            : Result.Ok(proposal.ToDto());
    }

    public async Task<Result<IReadOnlyList<RentalProposalDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var proposals = await _rentalProposalRepository.GetAllAsync(cancellationToken);
        IReadOnlyList<RentalProposalDto> dtos = proposals.Select(p => p.ToDto()).ToList();

        return Result.Ok(dtos);
    }

    public async Task<Result<IReadOnlyList<ProposalStatusHistoryDto>>> GetHistoryAsync(Guid proposalId, CancellationToken cancellationToken = default)
    {
        var proposal = await _rentalProposalRepository.GetByIdWithHistoryAsync(proposalId, cancellationToken);
        if (proposal is null)
        {
            return Result.Fail<IReadOnlyList<ProposalStatusHistoryDto>>(new NotFoundError($"Rental proposal '{proposalId}' was not found."));
        }

        IReadOnlyList<ProposalStatusHistoryDto> history = proposal.StatusHistory
            .OrderBy(h => h.ChangedAt)
            .Select(h => h.ToDto())
            .ToList();

        return Result.Ok(history);
    }
}
