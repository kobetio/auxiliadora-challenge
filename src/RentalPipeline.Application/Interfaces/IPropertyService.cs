using FluentResults;
using RentalPipeline.Application.DTOs;

namespace RentalPipeline.Application.Interfaces;

public interface IPropertyService
{
    Task<Result<PropertyDto>> CreateAsync(CreatePropertyRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every property excluding <c>Rented</c> ones (<c>GET /properties</c>).
    /// </summary>
    Task<Result<IReadOnlyList<PropertyDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a property regardless of status, including <c>Rented</c> (<c>GET /properties/{id}</c>).
    /// </summary>
    Task<Result<PropertyDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a property's editable details. Not part of the original challenge specification —
    /// added on explicit request to provide full CRUD.
    /// </summary>
    Task<Result<PropertyDto>> UpdateAsync(Guid id, UpdatePropertyRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a property. Not part of the original challenge specification — added on explicit
    /// request to provide full CRUD. Fails with a <c>ConflictError</c> if the property has
    /// associated rental proposals, to preserve referential/historical integrity.
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
