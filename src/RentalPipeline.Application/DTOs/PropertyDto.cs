using RentalPipeline.Domain.Enums;

namespace RentalPipeline.Application.DTOs;

public record PropertyDto(
    Guid Id,
    string Name,
    string? Description,
    string Address,
    PropertyStatus Status,
    DateTime CreatedAt);
