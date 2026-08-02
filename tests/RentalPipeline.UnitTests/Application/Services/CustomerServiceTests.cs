using NSubstitute;
using RentalPipeline.Application.DTOs;
using RentalPipeline.Application.Errors;
using RentalPipeline.Application.Interfaces;
using RentalPipeline.Application.Services;
using RentalPipeline.Domain.Entities;
using RentalPipeline.Domain.Interfaces;

namespace RentalPipeline.UnitTests.Application.Services;

public class CustomerServiceTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IRentalProposalRepository _rentalProposalRepository = Substitute.For<IRentalProposalRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CustomerService _sut;

    public CustomerServiceTests()
    {
        _sut = new CustomerService(_customerRepository, _rentalProposalRepository, _unitOfWork);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_Persists()
    {
        var request = new CreateCustomerRequest("Maria Silva", "maria@example.com", "+55 11 90000-0000");

        var result = await _sut.CreateAsync(request);

        Assert.True(result.IsSuccess);
        await _customerRepository.Received(1).AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNotFoundError()
    {
        _customerRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.True(result.IsFailed);
        Assert.IsType<NotFoundError>(result.Errors[0]);
    }

    [Fact]
    public async Task UpdateAsync_Found_UpdatesAndSaves()
    {
        var customer = new Customer("Maria Silva", "maria@example.com", "+55 11 90000-0000");
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await _sut.UpdateAsync(customer.Id, new UpdateCustomerRequest("Maria Souza", "maria.souza@example.com", "+55 11 91111-1111"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Maria Souza", customer.Name);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_HasAssociatedProposals_ReturnsConflictError()
    {
        var customer = new Customer("Maria Silva", "maria@example.com", "+55 11 90000-0000");
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _rentalProposalRepository.ExistsForCustomerAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.DeleteAsync(customer.Id);

        Assert.True(result.IsFailed);
        Assert.IsType<ConflictError>(result.Errors[0]);
        await _customerRepository.DidNotReceive().RemoveAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_NoAssociatedProposals_RemovesCustomer()
    {
        var customer = new Customer("Maria Silva", "maria@example.com", "+55 11 90000-0000");
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _rentalProposalRepository.ExistsForCustomerAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.DeleteAsync(customer.Id);

        Assert.True(result.IsSuccess);
        await _customerRepository.Received(1).RemoveAsync(customer, Arg.Any<CancellationToken>());
    }
}
