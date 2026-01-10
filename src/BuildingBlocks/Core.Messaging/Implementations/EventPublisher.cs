using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Core.Messaging.Implementations;

public sealed class EventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _exchangeName;

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

    public async Task PublishAsync<T>(
        T @event,
        string routingKey,
        CancellationToken cancellationToken = default
    )
    {
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
