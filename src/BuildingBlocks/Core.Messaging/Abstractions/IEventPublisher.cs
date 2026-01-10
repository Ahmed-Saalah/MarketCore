namespace Core.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(
        T @event,
        string routingKey,
        CancellationToken cancellationToken = default
    );
}
