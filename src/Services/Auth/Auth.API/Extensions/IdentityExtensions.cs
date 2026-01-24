using System.Text;
using Auth.API.Configuration;
using Auth.API.Data;
using Auth.API.Models;
using Auth.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Auth.API.Extensions;

public static class IdentityExtensions
{
    public static IServiceCollection AddIdentityAuth(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddIdentity<User, IdentityRole<int>>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<ITokenService, TokenService>();

        var jwtSettings = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
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
}
