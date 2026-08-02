using RentalPipeline.Domain.Entities;

namespace RentalPipeline.Domain.Interfaces;

public interface IPropertyRepository
{
    Task<Property?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every property whose status is not <see cref="Enums.PropertyStatus.Rented"/>,
    /// matching the <c>GET /properties</c> business rule (rented properties are permanently
    /// removed from the rental market listing).
    /// </summary>
    Task<IReadOnlyList<Property>> GetAllExcludingRentedAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Property property, CancellationToken cancellationToken = default);

    Task RemoveAsync(Property property, CancellationToken cancellationToken = default);
}
