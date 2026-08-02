using RentalPipeline.Domain.Entities;
using RentalPipeline.Domain.Enums;
using RentalPipeline.Domain.Exceptions;

namespace RentalPipeline.UnitTests.Domain.Entities;

public class PropertyTests
{
    [Fact]
    public void Constructor_NewProperty_StartsAsAvailable()
    {
        // Rule 1.
        var property = new Property("Loft Centro", "Rua A, 123");

        Assert.Equal(PropertyStatus.Available, property.Status);
        Assert.NotEqual(Guid.Empty, property.Id);
    }

    [Fact]
    public void MarkAsInNegotiation_WhenAvailable_Succeeds()
    {
        var property = new Property("Loft Centro", "Rua A, 123");

        property.MarkAsInNegotiation();

        Assert.Equal(PropertyStatus.InNegotiation, property.Status);
    }

    [Fact]
    public void MarkAsInNegotiation_WhenNotAvailable_ThrowsDomainException()
    {
        var property = new Property("Loft Centro", "Rua A, 123");
        property.MarkAsInNegotiation();

        Assert.Throws<DomainException>(property.MarkAsInNegotiation);
    }

    [Fact]
    public void MarkAsRented_WhenInNegotiation_Succeeds()
    {
        // Rule 6.
        var property = new Property("Loft Centro", "Rua A, 123");
        property.MarkAsInNegotiation();

        property.MarkAsRented();

        Assert.Equal(PropertyStatus.Rented, property.Status);
    }

    [Fact]
    public void MarkAsRented_WhenAvailable_ThrowsDomainException()
    {
        var property = new Property("Loft Centro", "Rua A, 123");

        Assert.Throws<DomainException>(property.MarkAsRented);
    }

    [Fact]
    public void MarkAsAvailable_WhenInNegotiation_Succeeds()
    {
        // Rule 7.
        var property = new Property("Loft Centro", "Rua A, 123");
        property.MarkAsInNegotiation();

        property.MarkAsAvailable();

        Assert.Equal(PropertyStatus.Available, property.Status);
    }

    [Fact]
    public void MarkAsAvailable_WhenAlreadyAvailable_ThrowsDomainException()
    {
        var property = new Property("Loft Centro", "Rua A, 123");

        Assert.Throws<DomainException>(property.MarkAsAvailable);
    }

    [Fact]
    public void UpdateDetails_ValidData_UpdatesFields()
    {
        var property = new Property("Loft Centro", "Rua A, 123");

        property.UpdateDetails("Loft Reformado", "Rua B, 456", "Nova descrição");

        Assert.Equal("Loft Reformado", property.Name);
        Assert.Equal("Rua B, 456", property.Address);
        Assert.Equal("Nova descrição", property.Description);
    }

    [Fact]
    public void UpdateDetails_AllowedRegardlessOfStatus()
    {
        var property = new Property("Loft Centro", "Rua A, 123");
        property.MarkAsInNegotiation();
        property.MarkAsRented();

        var exception = Record.Exception(() => property.UpdateDetails("Novo nome", "Novo endereço", null));

        Assert.Null(exception);
    }
}
