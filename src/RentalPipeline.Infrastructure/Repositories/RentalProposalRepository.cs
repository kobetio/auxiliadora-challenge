using Microsoft.EntityFrameworkCore;
using RentalPipeline.Domain.Entities;
using RentalPipeline.Domain.Interfaces;
using RentalPipeline.Infrastructure.Persistence;

namespace RentalPipeline.Infrastructure.Repositories;

public class RentalProposalRepository : IRentalProposalRepository
{
    private readonly RentalPipelineDbContext _context;

    public RentalProposalRepository(RentalPipelineDbContext context)
    {
        _context = context;
    }

    public Task<RentalProposal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.RentalProposals.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<RentalProposal?> GetByIdWithHistoryAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.RentalProposals
            .Include(p => p.StatusHistory)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<RentalProposal>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.RentalProposals.ToListAsync(cancellationToken);

    public async Task AddAsync(RentalProposal proposal, CancellationToken cancellationToken = default)
        => await _context.RentalProposals.AddAsync(proposal, cancellationToken);

    public Task<bool> ExistsForPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default)
        => _context.RentalProposals.AnyAsync(p => p.PropertyId == propertyId, cancellationToken);

    public Task<bool> ExistsForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
        => _context.RentalProposals.AnyAsync(p => p.CustomerId == customerId, cancellationToken);
}
