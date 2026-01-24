using Core.Messaging;
using Order.API.Handlers.Internal;
using Order.API.Handlers.Payment;
using Order.API.Handlers.Warehouse;
using Order.API.Messages;

namespace Order.API.Extensions;

public static class MessagingExtensions
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
                typeof(OrderCreatedEvent),
                typeof(OrderCreateEventHandler.Handler),
                "Order.OrderCreatedEvent"
            ),
            (
                typeof(StockReservedEventHandler.Event),
                typeof(StockReservedEventHandler.Handler),
                "Warehouse.StockReservedEvent"
            ),
            (
                typeof(StockReservationFailedEventHandler.Event),
                typeof(StockReservationFailedEventHandler.Handler),
                "Warehouse.StockReservationFailedEvent"
            ),
            (
                typeof(PaymentFailedEventHandler.Event),
                typeof(PaymentFailedEventHandler.Handler),
                "Payment.PaymentFailedEvent"
            ),
            (
                typeof(PaymentSucceededEventHandler.Event),
                typeof(PaymentSucceededEventHandler.Handler),
                "Payment.PaymentSucceededEvent"
            )
        );

        return services;
    }
}
