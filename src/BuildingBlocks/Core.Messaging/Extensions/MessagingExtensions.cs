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

    public static IServiceCollection AddRabbitMqEventConsumer(
        this IServiceCollection services,
        IConfiguration config,
        params (Type EventType, Type HandlerType, string RoutingKey)[] events
    )
    {
        var host = config["RABBIT_HOST"] ?? "localhost";
        var user = config["RABBIT_USER"] ?? "guest";
        var pass = config["RABBIT_PASSWORD"] ?? "guest";
        var queueName = config["RABBIT_QUEUE"] ?? "market_events_queue";
        var exchange = "market_event_bus";

        foreach (var evt in events)
        {
            var handlerInterface = typeof(IEventHandler<>).MakeGenericType(evt.EventType);
            services.AddScoped(handlerInterface, evt.HandlerType);
            services.AddScoped(evt.HandlerType);
        }

        services.AddHostedService(sp => new RabbitMqConsumerService(
            sp.GetRequiredService<ILogger<RabbitMqConsumerService>>(),
            sp,
            host,
            user,
            pass,
            exchange,
            queueName,
            events.ToDictionary(e => e.RoutingKey, e => e.EventType)
        ));

        return services;
    }
}
