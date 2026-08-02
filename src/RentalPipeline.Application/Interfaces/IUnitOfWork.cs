namespace RentalPipeline.Application.Interfaces;

/// <summary>
/// Commits changes tracked across one or more repositories in a single persistence operation.
/// Implemented directly by the EF Core <c>DbContext</c> in Infrastructure, keeping the
/// Application layer free of any EF Core dependency.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
