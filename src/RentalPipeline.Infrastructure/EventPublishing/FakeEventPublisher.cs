using Microsoft.Extensions.Logging;
using RentalPipeline.Application.Interfaces;

namespace RentalPipeline.Infrastructure.EventPublishing;

/// <summary>
/// Simulates event publishing via structured logging. Designed to be replaced by a RabbitMQ-backed
/// implementation later without any change to the Application layer or its callers — only this
/// class and its Dependency Injection registration would need to change.
/// </summary>
public class FakeEventPublisher : IEventPublisher
{
    private readonly ILogger<FakeEventPublisher> _logger;

    public FakeEventPublisher(ILogger<FakeEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : notnull
    {
        // Strips a trailing "Event" suffix (e.g. "ContractActivatedEvent" -> "ContractActivated") so the
        // logged name matches Architecture.md's own example verbatim ("Publishing Event / ContractActivated
        // / ProposalId / PropertyId / OccurredAt"). "{@Event}" structurally logs every one of the record's
        // properties, so this line works unchanged for any future event type, not just ContractActivated.
        var typeName = typeof(TEvent).Name;
        var eventName = typeName.EndsWith("Event", StringComparison.Ordinal)
            ? typeName[..^"Event".Length]
            : typeName;

        _logger.LogInformation("Publishing Event {EventName} {@Event}", eventName, @event);

        return Task.CompletedTask;
    }
}
