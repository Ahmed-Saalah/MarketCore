using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Core.Messaging.Implementations;

public class RabbitMqConsumerService : BackgroundService
{
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _hostName;
    private readonly string _userName;
    private readonly string _password;
    private readonly string _exchangeName;
    private readonly string _queueName;
    private readonly Dictionary<string, Type> _routingKeyEventMap;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqConsumerService(
        ILogger<RabbitMqConsumerService> logger,
        IServiceProvider serviceProvider,
        string hostName,
        string userName,
        string password,
        string exchangeName,
        string queueName,
        Dictionary<string, Type> routingKeyEventMap
    )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hostName = hostName;
        _userName = userName;
        _password = password;
        _exchangeName = exchangeName;
        _queueName = queueName;
        _routingKeyEventMap = routingKeyEventMap;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _hostName,
            UserName = _userName,
            Password = _password,
            AutomaticRecoveryEnabled = true,
        };

        const int maxRetries = 10;
        var delay = TimeSpan.FromSeconds(5);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(
                    cancellationToken: cancellationToken
                );
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Attempt {Attempt}/{MaxRetries} failed to connect to RabbitMQ. Retrying in {Delay}...",
                    attempt,
                    maxRetries,
                    delay
                );

                if (attempt == maxRetries)
                    throw;

                await Task.Delay(delay, cancellationToken);
            }
        }

        await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Topic, durable: true);
        await _channel.QueueDeclareAsync(
            _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        foreach (var routingKey in _routingKeyEventMap.Keys)
        {
            await _channel.QueueBindAsync(
                _queueName,
                _exchangeName,
                routingKey,
                cancellationToken: cancellationToken
            );
        }

        _logger.LogInformation(
            "RabbitMQ Consumer started on queue {Queue}. Listening to {Count} events.",
            _queueName,
            _routingKeyEventMap.Count
        );

        await base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is null)
            return Task.CompletedTask;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var routingKey = ea.RoutingKey;
                if (!_routingKeyEventMap.TryGetValue(routingKey, out var eventType))
                {
                    _logger.LogWarning(
                        "Unknown routing key {RoutingKey}, message ignored.",
                        routingKey
                    );
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var @event = JsonSerializer.Deserialize(message, eventType);

                if (@event is not null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
                    dynamic handler = scope.ServiceProvider.GetRequiredService(handlerType);
                    await handler.HandleAsync((dynamic)@event, stoppingToken);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        return _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer);
    }
}
