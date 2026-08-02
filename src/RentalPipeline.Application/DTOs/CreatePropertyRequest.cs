namespace RentalPipeline.Application.DTOs;

public record CreatePropertyRequest(
    string Name,
    string Address,
    string? Description);
