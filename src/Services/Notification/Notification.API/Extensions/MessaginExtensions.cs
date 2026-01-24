using Core.Messaging;
using Notification.API.Handlers.Order;

namespace Notification.API.Extensions;

public static class MessaginExtensions
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddMessageBroker();

        services.AddRabbitMqEventConsumer(
            (
                typeof(OrderCreatedEventHandler.Event),
                typeof(OrderCreatedEventHandler.Handler),
                "Order.OrderCreatedEvent"
            ),
            (
                typeof(OrderCanceledEventHandler.Event),
                typeof(OrderCanceledEventHandler.Handler),
                "Order.OrderCanceledEvent"
            ),
            (
                typeof(OrderCompletedEventHandler.Event),
                typeof(OrderCompletedEventHandler.Handler),
                "Order.OrderCompletedEvent"
            )
        );
        return services;
    }
}
