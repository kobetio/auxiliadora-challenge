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
        _logger.LogInformation(
            "Publishing Event {EventType} {@Event}",
            typeof(TEvent).Name,
            @event);

        return Task.CompletedTask;
    }
}
