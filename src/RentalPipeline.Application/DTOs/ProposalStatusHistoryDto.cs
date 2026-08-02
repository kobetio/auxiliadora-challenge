using RentalPipeline.Domain.Enums;

namespace RentalPipeline.Application.DTOs;

public record ProposalStatusHistoryDto(
    ProposalStatus PreviousStatus,
    ProposalStatus NewStatus,
    DateTime ChangedAt);
