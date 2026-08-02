using RentalPipeline.Domain.Enums;
using RentalPipeline.Domain.Exceptions;

namespace RentalPipeline.Domain.Entities;

/// <summary>
/// Represents a real estate property available for rental.
/// </summary>
public class Property
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Address { get; private set; } = null!;
    public PropertyStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Optimistic concurrency token. Mapped by the Npgsql EF Core provider to the
    /// PostgreSQL <c>xmin</c> system column, which is updated automatically on every row change.
    /// </summary>
    public uint RowVersion { get; private set; }

    private Property()
    {
        // Required by EF Core.
    }

    public Property(string name, string address, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        Id = Guid.NewGuid();
        Name = name;
        Address = address;
        Description = description;
        Status = PropertyStatus.Available; // Rule 1: every new property starts as Available.
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reserves the property for an in-progress rental proposal (Rule 3).
    /// </summary>
    public void MarkAsInNegotiation()
    {
        if (Status != PropertyStatus.Available)
        {
            throw new DomainException(
                $"Property {Id} cannot transition to {PropertyStatus.InNegotiation} from {Status}.");
        }

        Status = PropertyStatus.InNegotiation;
    }

    /// <summary>
    /// Returns the property to the rental market after a proposal is Rejected or Cancelled (Rule 7).
    /// </summary>
    public void MarkAsAvailable()
    {
        if (Status != PropertyStatus.InNegotiation)
        {
            throw new DomainException(
                $"Property {Id} cannot transition to {PropertyStatus.Available} from {Status}.");
        }

        Status = PropertyStatus.Available;
    }

    /// <summary>
    /// Permanently removes the property from the rental market once its proposal becomes Active (Rule 6).
    /// </summary>
    public void MarkAsRented()
    {
        if (Status != PropertyStatus.InNegotiation)
        {
            throw new DomainException(
                $"Property {Id} cannot transition to {PropertyStatus.Rented} from {Status}.");
        }

        Status = PropertyStatus.Rented;
    }
}
