using System.Net;
using System.Net.Http.Json;
using RentalPipeline.Application.DTOs;
using RentalPipeline.Domain.Enums;
using RentalPipeline.IntegrationTests.Infrastructure;

namespace RentalPipeline.IntegrationTests.Endpoints;

/// <summary>
/// Validates Architecture.md Section 9 ("Race Conditions"): two simultaneous requests to reserve the
/// same Property must never both succeed. Exercised against the real Postgres container (not a mock),
/// so it genuinely proves the Serializable transaction (<see cref="RentalPipeline.Application.Interfaces.IUnitOfWork.ExecuteInSerializableTransactionAsync{TResult}"/>)
/// plus the Property's <c>xmin</c> optimistic concurrency token close the race, end to end.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class ConcurrencyTests
{
    private readonly RentalPipelineApiFactory _factory;

    public ConcurrencyTests(RentalPipelineApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_TwoConcurrentProposalsForSameProperty_ExactlyOneSucceeds()
    {
        var setupClient = _factory.CreateClient();
        var property = await TestDataFactory.CreatePropertyAsync(setupClient);
        var firstCustomer = await TestDataFactory.CreateCustomerAsync(setupClient);
        var secondCustomer = await TestDataFactory.CreateCustomerAsync(setupClient);

        // Separate HttpClient instances (and therefore separate request pipelines/DbContext scopes)
        // so the two requests genuinely run concurrently instead of being serialized behind one client.
        var firstClient = _factory.CreateClient();
        var secondClient = _factory.CreateClient();

        var firstRequestTask = firstClient.PostAsJsonAsync("/proposals", new CreateProposalRequest(property.Id, firstCustomer.Id), TestJsonOptions.Default);
        var secondRequestTask = secondClient.PostAsJsonAsync("/proposals", new CreateProposalRequest(property.Id, secondCustomer.Id), TestJsonOptions.Default);

        var responses = await Task.WhenAll(firstRequestTask, secondRequestTask);

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);

        var propertyResponse = await setupClient.GetAsync($"/properties/{property.Id}");
        var updatedProperty = await propertyResponse.Content.ReadFromJsonAsync<PropertyDto>(TestJsonOptions.Default);
        Assert.Equal(PropertyStatus.InNegotiation, updatedProperty!.Status);
    }

    [Fact]
    public async Task UpdateStatus_TwoConcurrentRequestsForSameProposal_ExactlyOneSucceeds()
    {
        // Validates the other half of Architecture.md's "Optimistic Concurrency" section: when two
        // requests race to update the very same row (not two different proposals for the same
        // Property, covered above), EF Core's RowVersion/xmin check must reject the loser with
        // DbUpdateConcurrencyException, which ExceptionHandlingMiddleware maps to 409 Conflict.
        var setupClient = _factory.CreateClient();
        var property = await TestDataFactory.CreatePropertyAsync(setupClient);
        var customer = await TestDataFactory.CreateCustomerAsync(setupClient);
        var proposal = await TestDataFactory.CreateProposalAsync(setupClient, property.Id, customer.Id);

        var firstClient = _factory.CreateClient();
        var secondClient = _factory.CreateClient();

        var firstRequestTask = firstClient.PatchAsJsonAsync(
            $"/proposals/{proposal.Id}/status", new UpdateProposalStatusRequest(ProposalStatus.CreditAnalysis), TestJsonOptions.Default);
        var secondRequestTask = secondClient.PatchAsJsonAsync(
            $"/proposals/{proposal.Id}/status", new UpdateProposalStatusRequest(ProposalStatus.CreditAnalysis), TestJsonOptions.Default);

        var responses = await Task.WhenAll(firstRequestTask, secondRequestTask);

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
    }
}
