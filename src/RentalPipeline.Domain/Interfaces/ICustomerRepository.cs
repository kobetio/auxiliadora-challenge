using RentalPipeline.Domain.Entities;

namespace RentalPipeline.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    Task RemoveAsync(Customer customer, CancellationToken cancellationToken = default);
}
