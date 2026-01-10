using Core.Messaging.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessageBroker(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        var host = config["RABBIT_HOST"] ?? "localhost";
        var user = config["RABBIT_USER"] ?? "guest";
        var pass = config["RABBIT_PASSWORD"] ?? "guest";
        var exchange = "market_event_bus";

        services.AddSingleton<IEventPublisher>(sp =>
        {
            return EventPublisher.CreateAsync(host, user, pass, exchange).GetAwaiter().GetResult();
        });

        return services;
    }

    public static IServiceCollection AddRabbitMqEventConsumer<TEvent, THandler>(
        this IServiceCollection services,
        IConfiguration config,
        string routingKey,
        string queueName
    )
        where TEvent : class
        where THandler : class, IEventHandler<TEvent>
    {
        var host = config["RABBIT_HOST"] ?? "localhost";
        var user = config["RABBIT_USER"] ?? "guest";
        var pass = config["RABBIT_PASSWORD"] ?? "guest";
        var exchange = "market_event_bus";

        services.AddScoped<IEventHandler<TEvent>, THandler>();
        services.AddScoped<THandler>();

        services.AddHostedService(sp => new RabbitMqConsumerService<TEvent, THandler>(
            sp.GetRequiredService<ILogger<RabbitMqConsumerService<TEvent, THandler>>>(),
            sp,
            host,
            user,
            pass,
            exchange,
            queueName,
            routingKey
        ));

        return services;
    }
}
