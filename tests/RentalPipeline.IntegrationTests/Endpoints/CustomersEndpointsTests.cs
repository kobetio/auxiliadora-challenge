using System.Net;
using System.Net.Http.Json;
using RentalPipeline.Application.DTOs;
using RentalPipeline.IntegrationTests.Infrastructure;

namespace RentalPipeline.IntegrationTests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class CustomersEndpointsTests
{
    private readonly HttpClient _client;

    public CustomersEndpointsTests(RentalPipelineApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        var customer = await TestDataFactory.CreateCustomerAsync(_client);

        Assert.NotEqual(Guid.Empty, customer.Id);
    }

    [Fact]
    public async Task Create_InvalidEmail_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/customers", new CreateCustomerRequest("Name", "not-an-email", "+55 11 90000-0000"), TestJsonOptions.Default);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingCustomer_ReturnsOk()
    {
        var created = await TestDataFactory.CreateCustomerAsync(_client);

        var response = await _client.GetAsync($"/customers/{created.Id}");

        response.EnsureSuccessStatusCode();
        var fetched = await response.Content.ReadFromJsonAsync<CustomerDto>(TestJsonOptions.Default);
        Assert.Equal(created.Id, fetched!.Id);
    }

    [Fact]
    public async Task GetById_NonExistentCustomer_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ExistingCustomer_ReturnsOkWithUpdatedFields()
    {
        var created = await TestDataFactory.CreateCustomerAsync(_client);
        var updateRequest = new UpdateCustomerRequest("Updated Name", created.Email, "+55 11 98888-8888");

        var response = await _client.PutAsJsonAsync($"/customers/{created.Id}", updateRequest, TestJsonOptions.Default);

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<CustomerDto>(TestJsonOptions.Default);
        Assert.Equal("Updated Name", updated!.Name);
    }

    [Fact]
    public async Task Delete_CustomerWithoutProposals_ReturnsNoContent()
    {
        var created = await TestDataFactory.CreateCustomerAsync(_client);

        var response = await _client.DeleteAsync($"/customers/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_CustomerWithProposals_ReturnsConflict()
    {
        var property = await TestDataFactory.CreatePropertyAsync(_client);
        var customer = await TestDataFactory.CreateCustomerAsync(_client);
        await TestDataFactory.CreateProposalAsync(_client, property.Id, customer.Id);

        var response = await _client.DeleteAsync($"/customers/{customer.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
