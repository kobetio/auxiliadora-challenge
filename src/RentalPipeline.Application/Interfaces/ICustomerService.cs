using FluentResults;
using RentalPipeline.Application.DTOs;

namespace RentalPipeline.Application.Interfaces;

public interface ICustomerService
{
    Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CustomerDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a customer's editable details. Not part of the original challenge specification —
    /// added on explicit request to provide full CRUD.
    /// </summary>
    Task<Result<CustomerDto>> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a customer. Not part of the original challenge specification — added on explicit
    /// request to provide full CRUD. Fails with a <c>ConflictError</c> if the customer has
    /// associated rental proposals, to preserve referential/historical integrity.
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
