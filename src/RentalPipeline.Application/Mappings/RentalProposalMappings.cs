using RentalPipeline.Application.DTOs;
using RentalPipeline.Domain.Entities;

namespace RentalPipeline.Application.Mappings;

public static class RentalProposalMappings
{
    public static RentalProposalDto ToDto(this RentalProposal proposal) => new(
        proposal.Id,
        proposal.PropertyId,
        proposal.CustomerId,
        proposal.Status,
        proposal.CreatedAt,
        proposal.UpdatedAt);

    public static ProposalStatusHistoryDto ToDto(this ProposalStatusHistory history) => new(
        history.PreviousStatus,
        history.NewStatus,
        history.ChangedAt);
}
