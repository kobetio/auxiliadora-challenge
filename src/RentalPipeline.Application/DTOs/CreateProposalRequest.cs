namespace RentalPipeline.Application.DTOs;

public record CreateProposalRequest(
    Guid PropertyId,
    Guid CustomerId);
