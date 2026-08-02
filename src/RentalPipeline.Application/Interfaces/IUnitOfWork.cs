namespace RentalPipeline.Application.Interfaces;

/// <summary>
/// Commits changes tracked across one or more repositories in a single persistence operation.
/// Implemented directly by the EF Core <c>DbContext</c> in Infrastructure, keeping the
/// Application layer free of any EF Core dependency.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a database transaction using the Serializable
    /// isolation level (Architecture.md Section 9: "Race Conditions" / "Transaction Flow"), so
    /// PostgreSQL detects and aborts conflicting concurrent transactions instead of allowing a
    /// write-skew anomaly (e.g. two requests both reading a Property as <c>Available</c> and both
    /// reserving it). The transaction is committed only if <paramref name="operation"/> completes
    /// without throwing; any exception rolls it back automatically on disposal.
    /// </summary>
    Task<TResult> ExecuteInSerializableTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
