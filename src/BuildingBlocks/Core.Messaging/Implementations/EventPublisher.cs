using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Core.Messaging.Implementations;

public sealed class EventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _exchangeName;
    private static readonly ConcurrentDictionary<Type, string> _routingKeyCache = new();

    private EventPublisher(IConnection connection, IChannel channel, string exchangeName)
    {
        _connection = connection;
        _channel = channel;
        _exchangeName = exchangeName;
    }

    public static async Task<EventPublisher> CreateAsync(
        string hostName,
        string userName,
        string password,
        string exchangeName
    )
    {
        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
        };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false
        );

        return new EventPublisher(connection, channel, exchangeName);
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
    {
        var routingKey = _routingKeyCache.GetOrAdd(
            typeof(T),
            type =>
            {
                var attribute = type.GetCustomAttribute<MessageKeyAttribute>();

                if (attribute is null)
                {
                    throw new InvalidOperationException(
                        $"Event '{type.Name}' is missing the [MessageKey] attribute. Cannot publish without a routing key."
                    );
                }

                return attribute.Key;
            }
        );

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event));

        await _channel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: new BasicProperties { Persistent = true },
            body: body,
            cancellationToken: cancellationToken
        );
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
