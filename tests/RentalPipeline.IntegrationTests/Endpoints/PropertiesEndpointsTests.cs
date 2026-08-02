using System.Net;
using System.Net.Http.Json;
using RentalPipeline.Application.DTOs;
using RentalPipeline.Domain.Enums;
using RentalPipeline.IntegrationTests.Infrastructure;

namespace RentalPipeline.IntegrationTests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class PropertiesEndpointsTests
{
    private readonly HttpClient _client;

    public PropertiesEndpointsTests(RentalPipelineApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedWithAvailableStatus()
    {
        // Rule 1: every new property starts as Available.
        var property = await TestDataFactory.CreatePropertyAsync(_client);

        Assert.Equal(PropertyStatus.Available, property.Status);
    }

    [Fact]
    public async Task Create_InvalidRequest_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/properties", new CreatePropertyRequest(string.Empty, string.Empty, null), TestJsonOptions.Default);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingProperty_ReturnsOk()
    {
        var created = await TestDataFactory.CreatePropertyAsync(_client);

        var response = await _client.GetAsync($"/properties/{created.Id}");

        response.EnsureSuccessStatusCode();
        var fetched = await response.Content.ReadFromJsonAsync<PropertyDto>(TestJsonOptions.Default);
        Assert.Equal(created.Id, fetched!.Id);
    }

    [Fact]
    public async Task GetById_NonExistentProperty_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/properties/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ExistingProperty_ReturnsOkWithUpdatedFields()
    {
        var created = await TestDataFactory.CreatePropertyAsync(_client);
        var updateRequest = new UpdatePropertyRequest("Updated Name", "Updated Address", "Updated description");

        var response = await _client.PutAsJsonAsync($"/properties/{created.Id}", updateRequest, TestJsonOptions.Default);

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<PropertyDto>(TestJsonOptions.Default);
        Assert.Equal("Updated Name", updated!.Name);
        Assert.Equal("Updated Address", updated.Address);
    }

    [Fact]
    public async Task Delete_PropertyWithoutProposals_ReturnsNoContent()
    {
        var created = await TestDataFactory.CreatePropertyAsync(_client);

        var response = await _client.DeleteAsync($"/properties/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/properties/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_PropertyWithProposals_ReturnsConflict()
    {
        var property = await TestDataFactory.CreatePropertyAsync(_client);
        var customer = await TestDataFactory.CreateCustomerAsync(_client);
        await TestDataFactory.CreateProposalAsync(_client, property.Id, customer.Id);

        var response = await _client.DeleteAsync($"/properties/{property.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
