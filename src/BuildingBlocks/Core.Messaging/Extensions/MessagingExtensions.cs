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
        var host =
            config["RABBIT_HOST"]
            ?? throw new ArgumentNullException("RABBIT_HOST is not configured");

        var user =
            config["RABBIT_USER"]
            ?? throw new ArgumentNullException("RABBIT_USER is not configured");

        var pass =
            config["RABBIT_PASSWORD"]
            ?? throw new ArgumentNullException("RABBIT_PASSWORD is not configured");

        var exchange =
            config["RABBIT_EXCHANGE"]
            ?? throw new ArgumentNullException("RABBIT_EXCHANGE is not configured");

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
        var host =
            config["RABBIT_HOST"]
            ?? throw new ArgumentNullException("RABBIT_HOST is not configured");

        var user =
            config["RABBIT_USER"]
            ?? throw new ArgumentNullException("RABBIT_USER is not configured");

        var pass =
            config["RABBIT_PASSWORD"]
            ?? throw new ArgumentNullException("RABBIT_PASSWORD is not configured");

        var exchange =
            config["RABBIT_EXCHANGE"]
            ?? throw new ArgumentNullException("RABBIT_EXCHANGE is not configured");

        var queueName =
            config["RABBIT_QUEUE"]
            ?? throw new ArgumentNullException("RABBIT_QUEUE is not configured");

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
