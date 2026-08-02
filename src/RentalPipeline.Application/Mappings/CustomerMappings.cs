using RentalPipeline.Application.DTOs;
using RentalPipeline.Domain.Entities;

namespace RentalPipeline.Application.Mappings;

public static class CustomerMappings
{
    public static CustomerDto ToDto(this Customer customer) => new(
        customer.Id,
        customer.Name,
        customer.Email,
        customer.Phone,
        customer.CreatedAt);
}
