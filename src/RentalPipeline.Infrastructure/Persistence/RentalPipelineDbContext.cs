using System.Data;
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

    public async Task<TResult> ExecuteInSerializableTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        // Wrapped in the DbContext's execution strategy (EF Core's recommended pattern for manual
        // transactions), so that if retry-on-failure is ever enabled for the Npgsql provider, each
        // retry attempt correctly gets its own fresh transaction.
        var strategy = Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var result = await operation(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        });
    }
}
