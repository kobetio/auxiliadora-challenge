using System.Net;
using System.Net.Http.Json;
using RentalPipeline.Application.DTOs;
using RentalPipeline.Domain.Enums;
using RentalPipeline.IntegrationTests.Infrastructure;

namespace RentalPipeline.IntegrationTests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class ProposalsEndpointsTests
{
    private readonly HttpClient _client;
    private readonly RentalPipelineApiFactory _factory;

    public ProposalsEndpointsTests(RentalPipelineApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_AvailableProperty_ReturnsNewProposalAndReservesProperty()
    {
        // Rule 2 & 3.
        var property = await TestDataFactory.CreatePropertyAsync(_client);
        var customer = await TestDataFactory.CreateCustomerAsync(_client);

        var proposal = await TestDataFactory.CreateProposalAsync(_client, property.Id, customer.Id);

        Assert.Equal(ProposalStatus.New, proposal.Status);
        Assert.Equal(PropertyStatus.InNegotiation, await GetPropertyStatusAsync(property.Id));
    }

    [Fact]
    public async Task Create_PropertyAlreadyReserved_ReturnsConflict()
    {
        // Rule 2: cannot create a proposal for a non-Available property.
        var property = await TestDataFactory.CreatePropertyAsync(_client);
        var firstCustomer = await TestDataFactory.CreateCustomerAsync(_client);
        var secondCustomer = await TestDataFactory.CreateCustomerAsync(_client);
        await TestDataFactory.CreateProposalAsync(_client, property.Id, firstCustomer.Id);

        var response = await _client.PostAsJsonAsync("/proposals", new CreateProposalRequest(property.Id, secondCustomer.Id), TestJsonOptions.Default);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_UnknownProperty_ReturnsNotFound()
    {
        var customer = await TestDataFactory.CreateCustomerAsync(_client);

        var response = await _client.PostAsJsonAsync("/proposals", new CreateProposalRequest(Guid.NewGuid(), customer.Id), TestJsonOptions.Default);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_ValidTransition_UpdatesStatusAndRecordsHistory()
    {
        // Rule 4 & 8: valid transitions are accepted, and the initial creation plus every transition
        // is recorded in the history, oldest first.
        var property = await TestDataFactory.CreatePropertyAsync(_client);
        var customer = await TestDataFactory.CreateCustomerAsync(_client);
        var proposal = await TestDataFactory.CreateProposalAsync(_client, property.Id, customer.Id);

        var response = await ChangeStatusAsync(proposal.Id, ProposalStatus.CreditAnalysis);

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<RentalProposalDto>(TestJsonOptions.Default);
        Assert.Equal(ProposalStatus.CreditAnalysis, updated!.Status);

        var history = await GetHistoryAsync(proposal.Id);
        Assert.Equal(2, history.Count); // initial creation entry + this transition.
        Assert.Null(history[0].PreviousStatus);
        Assert.Equal(ProposalStatus.New, history[0].NewStatus);
        Assert.Equal(ProposalStatus.New, history[1].PreviousStatus);
        Assert.Equal(ProposalStatus.CreditAnalysis, history[1].NewStatus);
    }

    [Fact]
    public async Task UpdateStatus_SkippingStates_ReturnsBadRequest()
    {
        // Rule 4: New -> Signed skips CreditAnalysis/ContractIssued.
        var property = await TestDataFactory.CreatePropertyAsync(_client);
        var customer = await TestDataFactory.CreateCustomerAsync(_client);
        var proposal = await TestDataFactory.CreateProposalAsync(_client, property.Id, customer.Id);

        var response = await ChangeStatusAsync(proposal.Id, ProposalStatus.Signed);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(ProposalStatus.Rejected)]
    [InlineData(ProposalStatus.Cancelled)]
    public async Task UpdateStatus_ToRejectedOrCancelled_ReleasesPropertyBackToAvailable(ProposalStatus target)
    {
        // Rule 5 & 7.
        var property = await TestDataFactory.CreatePropertyAsync(_client);
        var customer = await TestDataFactory.CreateCustomerAsync(_client);
        var proposal = await TestDataFactory.CreateProposalAsync(_client, property.Id, customer.Id);

        var response = await ChangeStatusAsync(proposal.Id, target);

        response.EnsureSuccessStatusCode();
        Assert.Equal(PropertyStatus.Available, await GetPropertyStatusAsync(property.Id));
    }

    [Fact]
    public async Task UpdateStatus_ToActive_MarksPropertyRentedAndPublishesContractActivatedEvent()
    {
        // Rule 6 + event simulation.
        var property = await TestDataFactory.CreatePropertyAsync(_client);
        var customer = await TestDataFactory.CreateCustomerAsync(_client);
        var proposal = await TestDataFactory.CreateProposalAsync(_client, property.Id, customer.Id);

        await ChangeStatusAsync(proposal.Id, ProposalStatus.CreditAnalysis);
        await ChangeStatusAsync(proposal.Id, ProposalStatus.ContractIssued);
        await ChangeStatusAsync(proposal.Id, ProposalStatus.Signed);
        var response = await ChangeStatusAsync(proposal.Id, ProposalStatus.Active);

        response.EnsureSuccessStatusCode();
        Assert.Equal(PropertyStatus.Rented, await GetPropertyStatusAsync(property.Id));
        Assert.Contains(
            _factory.EventPublisher.ContractActivatedEvents,
            e => e.ProposalId == proposal.Id && e.PropertyId == property.Id);
    }

    [Fact]
    public async Task UpdateStatus_ToActive_RemovesPropertyFromListingButKeepsItReachableById()
    {
        // Rule 6: "It should no longer appear in GET /properties" — a Rented property is permanently
        // removed from the rental market listing, but GET /properties/{id} must still return it (it's
        // not deleted, just no longer offered).
        var property = await TestDataFactory.CreatePropertyAsync(_client);
        var customer = await TestDataFactory.CreateCustomerAsync(_client);
        var proposal = await TestDataFactory.CreateProposalAsync(_client, property.Id, customer.Id);

        await ChangeStatusAsync(proposal.Id, ProposalStatus.CreditAnalysis);
        await ChangeStatusAsync(proposal.Id, ProposalStatus.ContractIssued);
        await ChangeStatusAsync(proposal.Id, ProposalStatus.Signed);
        await ChangeStatusAsync(proposal.Id, ProposalStatus.Active);

        var listResponse = await _client.GetAsync("/properties");
        listResponse.EnsureSuccessStatusCode();
        var listing = await listResponse.Content.ReadFromJsonAsync<List<PropertyDto>>(TestJsonOptions.Default);
        Assert.DoesNotContain(listing!, p => p.Id == property.Id);

        var byIdResponse = await _client.GetAsync($"/properties/{property.Id}");
        byIdResponse.EnsureSuccessStatusCode();
        var fetched = await byIdResponse.Content.ReadFromJsonAsync<PropertyDto>(TestJsonOptions.Default);
        Assert.Equal(PropertyStatus.Rented, fetched!.Status);
    }

    [Fact]
    public async Task GetHistory_UnknownProposal_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/proposals/{Guid.NewGuid()}/history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Task<HttpResponseMessage> ChangeStatusAsync(Guid proposalId, ProposalStatus newStatus) =>
        _client.PatchAsJsonAsync($"/proposals/{proposalId}/status", new UpdateProposalStatusRequest(newStatus), TestJsonOptions.Default);

    private async Task<PropertyStatus> GetPropertyStatusAsync(Guid propertyId)
    {
        var response = await _client.GetAsync($"/properties/{propertyId}");
        response.EnsureSuccessStatusCode();
        var property = await response.Content.ReadFromJsonAsync<PropertyDto>(TestJsonOptions.Default);
        return property!.Status;
    }

    private async Task<IReadOnlyList<ProposalStatusHistoryDto>> GetHistoryAsync(Guid proposalId)
    {
        var response = await _client.GetAsync($"/proposals/{proposalId}/history");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<ProposalStatusHistoryDto>>(TestJsonOptions.Default))!;
    }
}
