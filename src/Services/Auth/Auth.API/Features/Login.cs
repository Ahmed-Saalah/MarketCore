using Auth.API.Abstractions;
using Auth.API.Constants;
using Auth.API.Data;
using Auth.API.Models;
using Auth.API.Services;
using Core.Domain.Errors;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Auth.API.Features;

public sealed class Login
{
    public sealed record Response(
        string AccessToken,
        string RefreshToken,
        string Role,
        ProfileData Profile
    );

    public sealed record ProfileData(
        string UserName,
        string Email,
        string DisplayName,
        string AvatarPath
    );

    public sealed record Request(string Email, string Password) : IRequest<Result<Response>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .MaximumLength(50)
                .WithMessage("Email must not exceed 50 characters.");

            RuleFor(r => r.Password)
                .NotEmpty()
                .WithMessage("Password is required.")
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters long.")
                .MaximumLength(100)
                .WithMessage("Password must not exceed 100 characters.");
        }
    }

    public sealed class Handler(
        AuthDbContext dbContext,
        ITokenService tokenService,
        IHttpContextAccessor httpContextAccessor,
        UserManager<User> userManager
    ) : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            {
                return new UnauthorizedError("Invalid email or password.");
            }

            var roles = await userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? Roles.Customer;

            var accessToken = tokenService.GenerateAccessToken(user, roles);

            var ipAddress =
                httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
                ?? "unknown";
            var refreshToken = tokenService.GenerateRefreshToken(ipAddress);

            user.RefreshTokens.Add(refreshToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            var profile = new ProfileData(
                user.UserName!,
                user.Email!,
                user.DisplayName ?? user.UserName!,
                user.AvatarPath ?? "/images/default-avatar.png"
            );

            return new Response(accessToken, refreshToken.Token, primaryRole, profile);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "api/users/login",
                    async ([FromBody] Request request, IMediator mediator) =>
                    {
                        var result = await mediator.Send(request);
                        return result.ToHttpResult();
                    }
                )
                .WithTags("Users")
                .AllowAnonymous();
        }
    }
}
