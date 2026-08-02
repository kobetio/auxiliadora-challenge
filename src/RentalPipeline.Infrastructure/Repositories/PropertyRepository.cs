using Microsoft.EntityFrameworkCore;
using RentalPipeline.Domain.Entities;
using RentalPipeline.Domain.Enums;
using RentalPipeline.Domain.Interfaces;
using RentalPipeline.Infrastructure.Persistence;

namespace RentalPipeline.Infrastructure.Repositories;

public class PropertyRepository : IPropertyRepository
{
    private readonly RentalPipelineDbContext _context;

    public PropertyRepository(RentalPipelineDbContext context)
    {
        _context = context;
    }

    public Task<Property?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Properties.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Property>> GetAllExcludingRentedAsync(CancellationToken cancellationToken = default)
        => await _context.Properties
            .Where(p => p.Status != PropertyStatus.Rented)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Property property, CancellationToken cancellationToken = default)
        => await _context.Properties.AddAsync(property, cancellationToken);

    public Task RemoveAsync(Property property, CancellationToken cancellationToken = default)
    {
        _context.Properties.Remove(property);
        return Task.CompletedTask;
    }
}
