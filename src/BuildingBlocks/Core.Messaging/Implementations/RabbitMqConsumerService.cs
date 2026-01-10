using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Core.Messaging.Implementations;

public class RabbitMqConsumerService<TEvent, THandler> : BackgroundService
    where TEvent : class
    where THandler : IEventHandler<TEvent>
{
    private readonly ILogger<RabbitMqConsumerService<TEvent, THandler>> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _hostName;
    private readonly string _userName;
    private readonly string _password;
    private readonly string _exchangeName;
    private readonly string _queueName;
    private readonly string _routingKey;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqConsumerService(
        ILogger<RabbitMqConsumerService<TEvent, THandler>> logger,
        IServiceProvider serviceProvider,
        string hostName,
        string userName,
        string password,
        string exchangeName,
        string queueName,
        string routingKey
    )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hostName = hostName;
        _userName = userName;
        _password = password;
        _exchangeName = exchangeName;
        _queueName = queueName;
        _routingKey = routingKey;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _hostName,
            UserName = _userName,
            Password = _password,
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Topic, durable: true);
        await _channel.QueueDeclareAsync(
            _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        );
        await _channel.QueueBindAsync(_queueName, _exchangeName, _routingKey);

        _logger.LogInformation(
            "RabbitMQ Consumer for {Event} started on queue {Queue}.",
            typeof(TEvent).Name,
            _queueName
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
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<THandler>();

                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var @event = JsonSerializer.Deserialize<TEvent>(message);

                if (@event is not null)
                {
                    await handler.HandleAsync(@event, stoppingToken);
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
