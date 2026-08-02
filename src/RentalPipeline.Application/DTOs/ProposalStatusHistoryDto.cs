using RentalPipeline.Domain.Enums;

namespace RentalPipeline.Application.DTOs;

/// <summary>
/// <see cref="PreviousStatus"/> is <c>null</c> for the entry representing the proposal's initial
/// creation (Rule 8) — there is no real previous status to report in that case.
/// </summary>
public record ProposalStatusHistoryDto(
    ProposalStatus? PreviousStatus,
    ProposalStatus NewStatus,
    DateTime ChangedAt);
