using Microsoft.EntityFrameworkCore;
using RentalPipeline.Application.Interfaces;
using RentalPipeline.Domain.Entities;

namespace RentalPipeline.Infrastructure.Persistence;

public class RentalPipelineDbContext : DbContext, IUnitOfWork
{
    public RentalPipelineDbContext(DbContextOptions<RentalPipelineDbContext> options) : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<RentalProposal> RentalProposals => Set<RentalProposal>();
    public DbSet<ProposalStatusHistory> ProposalStatusHistories => Set<ProposalStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RentalPipelineDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
