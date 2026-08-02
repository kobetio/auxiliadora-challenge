using RentalPipeline.Domain.Enums;

namespace RentalPipeline.Application.DTOs;

/// <summary>
/// Request body for <c>PATCH /proposals/{id}/status</c>. The proposal id itself comes from
/// the route (REST convention), so only the target status is part of the body.
/// </summary>
public record UpdateProposalStatusRequest(ProposalStatus NewStatus);
