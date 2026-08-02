using System.Collections.Concurrent;
using RentalPipeline.Application.Contracts;
using RentalPipeline.Application.Interfaces;

namespace RentalPipeline.IntegrationTests.Infrastructure;

/// <summary>
/// Test double for <see cref="IEventPublisher"/>, registered in place of <c>FakeEventPublisher</c>
/// for the integration test host, so tests can assert an event was actually published (Architecture.md
/// "Required Test Scenarios": "Active publishes ContractActivated event") instead of only inferring
/// it indirectly from log output. Shared across all tests in the collection, so callers must filter
/// <see cref="ContractActivatedEvents"/> by the specific id they created.
/// </summary>
public class RecordingEventPublisher : IEventPublisher
{
    private readonly ConcurrentBag<ContractActivatedEvent> _contractActivatedEvents = [];

    public IReadOnlyCollection<ContractActivatedEvent> ContractActivatedEvents => _contractActivatedEvents;

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : notnull
    {
        if (@event is ContractActivatedEvent contractActivatedEvent)
        {
            _contractActivatedEvents.Add(contractActivatedEvent);
        }

        return Task.CompletedTask;
    }
}
