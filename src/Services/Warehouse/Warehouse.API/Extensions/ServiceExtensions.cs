using System.Text;
using Core.Messaging;
using Core.Messaging.Options;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Warehouse.API.Configuration;
using Warehouse.API.Data;
using Warehouse.API.Handlers.Orders.Commands;
using Warehouse.API.Handlers.Orders.Events;
using Warehouse.API.Handlers.Products;

namespace Warehouse.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<WarehouseDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(Program).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddHttpContextAccessor();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        var jwtSettings = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Secret)
                    ),
                };
            });

        services.AddAuthorization();

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
