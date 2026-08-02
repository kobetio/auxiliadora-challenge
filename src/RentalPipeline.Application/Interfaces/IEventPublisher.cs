namespace RentalPipeline.Application.Interfaces;

/// <summary>
/// Abstraction for publishing domain/integration events. The initial implementation
/// (<c>FakeEventPublisher</c>, Infrastructure/EventPublishing) only logs the event, but this
/// abstraction is designed so a future RabbitMQ-backed implementation can be swapped in without
/// touching any Application code.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : notnull;
}
