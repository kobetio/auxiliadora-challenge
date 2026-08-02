using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RentalPipeline.Application.DTOs;
using RentalPipeline.Application.Errors;
using RentalPipeline.Application.Interfaces;
using RentalPipeline.Application.Services;
using RentalPipeline.Domain.Entities;
using RentalPipeline.Domain.Interfaces;

namespace RentalPipeline.UnitTests.Application.Services;

public class PropertyServiceTests
{
    private readonly IPropertyRepository _propertyRepository = Substitute.For<IPropertyRepository>();
    private readonly IRentalProposalRepository _rentalProposalRepository = Substitute.For<IRentalProposalRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PropertyService _sut;

    public PropertyServiceTests()
    {
        _sut = new PropertyService(_propertyRepository, _rentalProposalRepository, _unitOfWork, NullLogger<PropertyService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsAvailableProperty()
    {
        var request = new CreatePropertyRequest("Loft Centro", "Rua A, 123", null);

        var result = await _sut.CreateAsync(request);

        Assert.True(result.IsSuccess);
        await _propertyRepository.Received(1).AddAsync(Arg.Any<Property>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNotFoundError()
    {
        _propertyRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Property?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.True(result.IsFailed);
        Assert.IsType<NotFoundError>(result.Errors[0]);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNotFoundError()
    {
        _propertyRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Property?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdatePropertyRequest("Nome", "Endereço", null));

        Assert.True(result.IsFailed);
        Assert.IsType<NotFoundError>(result.Errors[0]);
    }

    [Fact]
    public async Task UpdateAsync_Found_UpdatesAndSaves()
    {
        var property = new Property("Loft Centro", "Rua A, 123");
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var result = await _sut.UpdateAsync(property.Id, new UpdatePropertyRequest("Novo Nome", "Rua B, 456", "desc"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Novo Nome", property.Name);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsNotFoundError()
    {
        _propertyRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Property?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        Assert.True(result.IsFailed);
        Assert.IsType<NotFoundError>(result.Errors[0]);
    }

    [Fact]
    public async Task DeleteAsync_HasAssociatedProposals_ReturnsConflictError()
    {
        var property = new Property("Loft Centro", "Rua A, 123");
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        _rentalProposalRepository.ExistsForPropertyAsync(property.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.DeleteAsync(property.Id);

        Assert.True(result.IsFailed);
        Assert.IsType<ConflictError>(result.Errors[0]);
        await _propertyRepository.DidNotReceive().RemoveAsync(Arg.Any<Property>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_NoAssociatedProposals_RemovesProperty()
    {
        var property = new Property("Loft Centro", "Rua A, 123");
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        _rentalProposalRepository.ExistsForPropertyAsync(property.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.DeleteAsync(property.Id);

        Assert.True(result.IsSuccess);
        await _propertyRepository.Received(1).RemoveAsync(property, Arg.Any<CancellationToken>());
    }
}
