using FluentResults;
using Microsoft.Extensions.Logging;
using RentalPipeline.Application.DTOs;
using RentalPipeline.Application.Errors;
using RentalPipeline.Application.Interfaces;
using RentalPipeline.Application.Mappings;
using RentalPipeline.Domain.Entities;
using RentalPipeline.Domain.Interfaces;

namespace RentalPipeline.Application.Services;

public class PropertyService : IPropertyService
{
    private readonly IPropertyRepository _propertyRepository;
    private readonly IRentalProposalRepository _rentalProposalRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PropertyService> _logger;

    public PropertyService(
        IPropertyRepository propertyRepository,
        IRentalProposalRepository rentalProposalRepository,
        IUnitOfWork unitOfWork,
        ILogger<PropertyService> logger)
    {
        _propertyRepository = propertyRepository;
        _rentalProposalRepository = rentalProposalRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PropertyDto>> CreateAsync(CreatePropertyRequest request, CancellationToken cancellationToken = default)
    {
        // Rule 1 (new properties start as Available) is guaranteed by the Property constructor.
        var property = new Property(request.Name, request.Address, request.Description);

        await _propertyRepository.AddAsync(property, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Property created: {PropertyId}", property.Id);

        return Result.Ok(property.ToDto());
    }

    public async Task<Result<IReadOnlyList<PropertyDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var properties = await _propertyRepository.GetAllExcludingRentedAsync(cancellationToken);
        IReadOnlyList<PropertyDto> dtos = properties.Select(p => p.ToDto()).ToList();

        return Result.Ok(dtos);
    }

    public async Task<Result<PropertyDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await _propertyRepository.GetByIdAsync(id, cancellationToken);

        return property is null
            ? Result.Fail<PropertyDto>(new NotFoundError($"Property '{id}' was not found."))
            : Result.Ok(property.ToDto());
    }

    public async Task<Result<PropertyDto>> UpdateAsync(Guid id, UpdatePropertyRequest request, CancellationToken cancellationToken = default)
    {
        var property = await _propertyRepository.GetByIdAsync(id, cancellationToken);
        if (property is null)
        {
            return Result.Fail<PropertyDto>(new NotFoundError($"Property '{id}' was not found."));
        }

        property.UpdateDetails(request.Name, request.Address, request.Description);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(property.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await _propertyRepository.GetByIdAsync(id, cancellationToken);
        if (property is null)
        {
            return Result.Fail(new NotFoundError($"Property '{id}' was not found."));
        }

        var hasProposals = await _rentalProposalRepository.ExistsForPropertyAsync(id, cancellationToken);
        if (hasProposals)
        {
            return Result.Fail(new ConflictError($"Property '{id}' cannot be deleted because it has associated rental proposals."));
        }

        await _propertyRepository.RemoveAsync(property, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
