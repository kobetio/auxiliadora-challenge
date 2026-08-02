using FluentResults;
using Microsoft.Extensions.Logging;
using RentalPipeline.Application.DTOs;
using RentalPipeline.Application.Errors;
using RentalPipeline.Application.Interfaces;
using RentalPipeline.Application.Mappings;
using RentalPipeline.Domain.Entities;
using RentalPipeline.Domain.Interfaces;

namespace RentalPipeline.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IRentalProposalRepository _rentalProposalRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        ICustomerRepository customerRepository,
        IRentalProposalRepository rentalProposalRepository,
        IUnitOfWork unitOfWork,
        ILogger<CustomerService> logger)
    {
        _customerRepository = customerRepository;
        _rentalProposalRepository = rentalProposalRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = new Customer(request.Name, request.Email, request.Phone);

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Customer created: {CustomerId}", customer.Id);

        return Result.Ok(customer.ToDto());
    }

    public async Task<Result<IReadOnlyList<CustomerDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.GetAllAsync(cancellationToken);
        IReadOnlyList<CustomerDto> dtos = customers.Select(c => c.ToDto()).ToList();

        return Result.Ok(dtos);
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);

        return customer is null
            ? Result.Fail<CustomerDto>(new NotFoundError($"Customer '{id}' was not found."))
            : Result.Ok(customer.ToDto());
    }

    public async Task<Result<CustomerDto>> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return Result.Fail<CustomerDto>(new NotFoundError($"Customer '{id}' was not found."));
        }

        customer.UpdateDetails(request.Name, request.Email, request.Phone);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(customer.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return Result.Fail(new NotFoundError($"Customer '{id}' was not found."));
        }

        var hasProposals = await _rentalProposalRepository.ExistsForCustomerAsync(id, cancellationToken);
        if (hasProposals)
        {
            return Result.Fail(new ConflictError($"Customer '{id}' cannot be deleted because it has associated rental proposals."));
        }

        await _customerRepository.RemoveAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
