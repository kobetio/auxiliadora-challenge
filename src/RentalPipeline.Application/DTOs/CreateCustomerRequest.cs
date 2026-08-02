namespace RentalPipeline.Application.DTOs;

public record CreateCustomerRequest(
    string Name,
    string Email,
    string Phone);
