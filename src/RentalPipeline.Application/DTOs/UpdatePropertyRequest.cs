namespace RentalPipeline.Application.DTOs;

public record UpdatePropertyRequest(
    string Name,
    string Address,
    string? Description);
