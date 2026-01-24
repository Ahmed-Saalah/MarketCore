using Core.Messaging;
using Warehouse.API.Handlers.Orders.Commands;
using Warehouse.API.Handlers.Orders.Events;
using Warehouse.API.Handlers.Products;

namespace Warehouse.API.Extensions;

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
                typeof(ProductCreatedEventHandler.Event),
                typeof(ProductCreatedEventHandler.Handler),
                "Catalog.Product.ProductCreatedEvent"
            ),
            (
                typeof(ProductUpdatedEventHandler.Event),
                typeof(ProductUpdatedEventHandler.Handler),
                "Catalog.Product.ProductUpdatedEvent"
            ),
            (
                typeof(ReserveStockCommandHandler.Command),
                typeof(ReserveStockCommandHandler.Handler),
                "Order.ReserveStockCommand"
            ),
            (
                typeof(OrderCompletedEventHandler.Event),
                typeof(OrderCompletedEventHandler.Handler),
                "Order.OrderCompletedEvent"
            ),
            (
                typeof(OrderCanceledEventHandler.Event),
                typeof(OrderCanceledEventHandler.Handler),
                "Order.OrderCanceledEventHandler"
            )
        );

        return services;
    }
}
