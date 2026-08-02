using System.Net.Http.Json;
using RentalPipeline.Application.DTOs;

namespace RentalPipeline.IntegrationTests.Infrastructure;

/// <summary>
/// Creates valid Properties/Customers/Proposals through the real HTTP endpoints (never by touching
/// the database directly), so every integration test exercises the full request pipeline even for
/// its setup steps. Every call uses randomly-generated data so tests sharing the same database
/// (see <see cref="IntegrationTestCollection"/>) never collide with each other's rows.
/// </summary>
public static class TestDataFactory
{
    public static async Task<PropertyDto> CreatePropertyAsync(HttpClient client)
    {
        var unique = Guid.NewGuid();
        var request = new CreatePropertyRequest($"Property {unique}", $"Street {unique}", "Created by an integration test.");

        var response = await client.PostAsJsonAsync("/properties", request, TestJsonOptions.Default);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<PropertyDto>(TestJsonOptions.Default))!;
    }

    public static async Task<CustomerDto> CreateCustomerAsync(HttpClient client)
    {
        var unique = Guid.NewGuid();
        var request = new CreateCustomerRequest($"Customer {unique}", $"{unique}@example.com", "+55 11 90000-0000");

        var response = await client.PostAsJsonAsync("/customers", request, TestJsonOptions.Default);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<CustomerDto>(TestJsonOptions.Default))!;
    }

    public static async Task<RentalProposalDto> CreateProposalAsync(HttpClient client, Guid propertyId, Guid customerId)
    {
        var request = new CreateProposalRequest(propertyId, customerId);

        var response = await client.PostAsJsonAsync("/proposals", request, TestJsonOptions.Default);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<RentalProposalDto>(TestJsonOptions.Default))!;
    }
}
