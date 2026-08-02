using FluentResults;
using RentalPipeline.Application.DTOs;
using RentalPipeline.Application.Errors;
using RentalPipeline.Application.Interfaces;
using RentalPipeline.Application.Mappings;
using RentalPipeline.Domain.Entities;
using RentalPipeline.Domain.Interfaces;

namespace RentalPipeline.Application.Services;

public class RentalProposalService : IRentalProposalService
{
    private readonly IRentalProposalRepository _rentalProposalRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RentalProposalService(
        IRentalProposalRepository rentalProposalRepository,
        IPropertyRepository propertyRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork)
    {
        _rentalProposalRepository = rentalProposalRepository;
        _propertyRepository = propertyRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RentalProposalDto>> CreateAsync(CreateProposalRequest request, CancellationToken cancellationToken = default)
    {
        var property = await _propertyRepository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property is null)
        {
            return Result.Fail<RentalProposalDto>(new NotFoundError($"Property '{request.PropertyId}' was not found."));
        }

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Fail<RentalProposalDto>(new NotFoundError($"Customer '{request.CustomerId}' was not found."));
        }

        // NOTE: Rule 2 (property must be Available) and Rule 3 (Property -> InNegotiation) are
        // wired in Phase 4, inside the Serializable transaction described in Architecture.md
        // Section 9 (implemented in Phase 6). This phase only covers the CRUD-ish creation flow.
        var proposal = new RentalProposal(property.Id, customer.Id);

        await _rentalProposalRepository.AddAsync(proposal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
