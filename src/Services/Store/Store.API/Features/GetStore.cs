using System.Security.Claims;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Store.API.Data;

namespace Store.API.Features;

public sealed class GetMyStore
{
    public sealed record Response(
        Guid Id,
        string Name,
        string Description,
        string? LogoUrl,
        string? CoverImageUrl,
        bool IsActive,
        string OwnerEmail
    );

    public record Request : IRequest<Result<Response>>;

    public class Handler(StoreDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var user = httpContextAccessor.HttpContext?.User;
            var identityIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (
                string.IsNullOrEmpty(identityIdClaim)
                || !int.TryParse(identityIdClaim, out int identityId)
            )
            {
                return new UnauthorizedError("User identity could not be verified.");
            }

            var store = await dbContext
                .Stores.AsNoTracking()
                .FirstOrDefaultAsync(s => s.OwnerIdentityId == identityId, cancellationToken);

            if (store is null)
            {
                return new NotFound("Store profile not found.");
            }

            return new Response(
                store.Id,
                store.Name,
                store.Description,
                store.LogoUrl,
                store.CoverImageUrl,
                store.IsActive,
                store.OwnerEmail
            );
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "api/stores/me",
                    async (IMediator mediator) =>
                    {
                        var result = await mediator.Send(new Request());
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags("Stores");
        }
    }
}
