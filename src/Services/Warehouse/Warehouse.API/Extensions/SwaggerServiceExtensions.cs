using Microsoft.OpenApi.Models;

namespace Warehouse.API.Extensions;

public static class SwaggerServiceExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(setupAction =>
        {
            setupAction.AddSecurityDefinition(
                "MarketCoreApiBearerAuth",
                new OpenApiSecurityScheme()
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    Description =
                        "Input your Bearer token in this format - Bearer {your token here} to access this API",
                    BearerFormat = "JWT",
                }
            );

            setupAction.CustomSchemaIds(opts => opts.FullName?.Replace("+", "."));

            setupAction.AddSecurityRequirement(
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "MarketCoreApiBearerAuth",
                            },
                        },
                        new List<string>()
                    },
                }
            );
        });

        return services;
    }
}
