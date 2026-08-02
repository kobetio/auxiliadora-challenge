using RentalPipeline.Application.DTOs;
using RentalPipeline.Domain.Entities;

namespace RentalPipeline.Application.Mappings;

public static class PropertyMappings
{
    public static PropertyDto ToDto(this Property property) => new(
        property.Id,
        property.Name,
        property.Description,
        property.Address,
        property.Status,
        property.CreatedAt);
}
