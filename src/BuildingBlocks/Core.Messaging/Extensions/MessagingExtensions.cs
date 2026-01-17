using Core.Messaging.Implementations;
using Core.Messaging.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessageBroker(this IServiceCollection services)
    {
        services.AddSingleton<IEventPublisher>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

            return EventPublisher
                .CreateAsync(options.Host, options.User, options.Password, options.Exchange)
                .GetAwaiter()
                .GetResult();
        });

        return services;
    }

    public static IServiceCollection AddRabbitMqEventConsumer(
        this IServiceCollection services,
        params (Type EventType, Type HandlerType, string RoutingKey)[] events
    )
    {
        foreach (var evt in events)
        {
            var handlerInterface = typeof(IEventHandler<>).MakeGenericType(evt.EventType);
            services.AddScoped(handlerInterface, evt.HandlerType);
            services.AddScoped(evt.HandlerType);
        }

        services.AddHostedService(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

            return new RabbitMqConsumerService(
                sp.GetRequiredService<ILogger<RabbitMqConsumerService>>(),
                sp,
                options.Host,
                options.User,
                options.Password,
                options.Exchange,
                options.Queue,
                events.ToDictionary(e => e.RoutingKey, e => e.EventType)
            );
        });

        return services;
    }
}
