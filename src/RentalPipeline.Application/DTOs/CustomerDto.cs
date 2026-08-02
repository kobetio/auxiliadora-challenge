namespace RentalPipeline.Application.DTOs;

public record CustomerDto(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    DateTime CreatedAt);
