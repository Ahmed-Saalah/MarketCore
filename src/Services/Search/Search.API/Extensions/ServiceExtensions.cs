using Core.Messaging;
using Core.Messaging.Options;
using Search.API.Handlers.Catalog;
using Search.API.Handlers.Warehouse;

namespace Search.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(Program).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddHttpContextAccessor();
        return services;
    }

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
