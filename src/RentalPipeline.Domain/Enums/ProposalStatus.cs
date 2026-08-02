namespace RentalPipeline.Domain.Enums;

/// <summary>
/// Represents the current step of a <see cref="Entities.RentalProposal"/> in the rental pipeline.
/// </summary>
public enum ProposalStatus
{
    /// <summary>
    /// The proposal was just created and has not started credit analysis yet.
    /// </summary>
    New = 0,

    /// <summary>
    /// The customer's credit is being analyzed.
    /// </summary>
    CreditAnalysis = 1,

    /// <summary>
    /// The rental contract document has been issued.
    /// </summary>
    ContractIssued = 2,

    /// <summary>
    /// The rental contract has been signed by the parties.
    /// </summary>
    Signed = 3,

    /// <summary>
    /// The rental contract is active. This is a terminal status: reaching it
    /// permanently changes the related property to <see cref="PropertyStatus.Rented"/>.
    /// </summary>
    Active = 4,

    /// <summary>
    /// The proposal was rejected before becoming active. Terminal status.
    /// </summary>
    Rejected = 5,

    /// <summary>
    /// The proposal was cancelled before becoming active. Terminal status.
    /// </summary>
    Cancelled = 6
}
