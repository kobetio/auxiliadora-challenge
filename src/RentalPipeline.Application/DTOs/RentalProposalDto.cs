using RentalPipeline.Domain.Enums;

namespace RentalPipeline.Application.DTOs;

public record RentalProposalDto(
    Guid Id,
    Guid PropertyId,
    Guid CustomerId,
    ProposalStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
