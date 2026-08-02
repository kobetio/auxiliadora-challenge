namespace RentalPipeline.Application.DTOs;

public record UpdateCustomerRequest(
    string Name,
    string Email,
    string Phone);
