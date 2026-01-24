using Core.Messaging;
using Search.API.Handlers.Catalog;
using Search.API.Handlers.Warehouse;

namespace Search.API.Extensions;

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
                typeof(ProductCreatedEventHandler.Event),
                typeof(ProductCreatedEventHandler.Handler),
                "Catalog.ProductCreatedEvent"
            ),
            (
                typeof(ProductActivatedEventHandler.Event),
                typeof(ProductActivatedEventHandler.Handler),
                "Catalog.ProductActivatedEvent"
            ),
            (
                typeof(ProductDeactivatedEventHandler.Event),
                typeof(ProductDeactivatedEventHandler.Handler),
                "Catalog.ProductDeactivatedEvent"
            ),
            (
                typeof(ProductUpdatedEventHandler.Event),
                typeof(ProductUpdatedEventHandler.Handler),
                "Catalog.ProductUpdatedEvent"
            ),
            (
                typeof(ProductOutOfStockEventHandler.Event),
                typeof(ProductOutOfStockEventHandler.Handler),
                "Warehouse.ProductOutOfStockEvent"
            ),
            (
                typeof(ProductLowStockEventHandler.Event),
                typeof(ProductLowStockEventHandler.Handler),
                "Warehouse.ProductLowStockEvent"
            ),
            (
                typeof(ProductBackInStockEventHandler.Event),
                typeof(ProductBackInStockEventHandler.Handler),
                "Warehouse.ProductBackInStockEvent"
            )
        );

        return services;
    }
}
