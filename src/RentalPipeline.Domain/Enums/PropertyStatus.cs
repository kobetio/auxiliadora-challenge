namespace RentalPipeline.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a <see cref="Entities.Property"/> in the rental market.
/// </summary>
public enum PropertyStatus
{
    /// <summary>
    /// The property is available and can receive new rental proposals.
    /// </summary>
    Available = 0,

    /// <summary>
    /// The property has an active rental proposal in progress and cannot receive new proposals.
    /// </summary>
    InNegotiation = 1,

    /// <summary>
    /// The property has an active rental contract. This status is permanent:
    /// the property is considered removed from the rental market and must no
    /// longer be returned by property listing endpoints.
    /// </summary>
    Rented = 2
}
