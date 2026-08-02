using RentalPipeline.Domain.Entities;

namespace RentalPipeline.UnitTests.Domain.Entities;

public class CustomerTests
{
    [Fact]
    public void Constructor_ValidData_CreatesCustomer()
    {
        var customer = new Customer("Maria Silva", "maria@example.com", "+55 11 90000-0000");

        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal("Maria Silva", customer.Name);
        Assert.Equal("maria@example.com", customer.Email);
    }

    [Theory]
    [InlineData("", "maria@example.com", "123")]
    [InlineData("Maria", "", "123")]
    [InlineData("Maria", "maria@example.com", "")]
    public void Constructor_MissingRequiredField_Throws(string name, string email, string phone)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Customer(name, email, phone));
    }

    [Fact]
    public void UpdateDetails_ValidData_UpdatesFields()
    {
        var customer = new Customer("Maria Silva", "maria@example.com", "+55 11 90000-0000");

        customer.UpdateDetails("Maria Souza", "maria.souza@example.com", "+55 11 91111-1111");

        Assert.Equal("Maria Souza", customer.Name);
        Assert.Equal("maria.souza@example.com", customer.Email);
        Assert.Equal("+55 11 91111-1111", customer.Phone);
    }
}
