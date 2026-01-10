using System.Security.Claims;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Store.API.Data;
using Store.API.Messages;

namespace Store.API.Features;

public static class DeactivateStore
{
    public record Request : IRequest<Result<bool>>;

    public class Handler(
        StoreDbContext dbContext,
        IEventPublisher eventPublisher,
        IHttpContextAccessor httpContextAccessor
    ) : IRequestHandler<Request, Result<bool>>
    {
        public async Task<Result<bool>> Handle(Request request, CancellationToken cancellationToken)
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

            var store = await dbContext.Stores.FirstOrDefaultAsync(
                s => s.OwnerIdentityId == identityId,
                cancellationToken
            );

            if (store is null)
            {
                return new NotFound("Store profile not found.");
            }

            store.IsActive = false;
            store.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            await eventPublisher.PublishAsync(
                new StoreDeactivatedEvent(
                    store.Id,
                    store.OwnerIdentityId,
                    store.OwnerName,
                    store.OwnerEmail,
                    store.OwnerPhoneNumber,
                    store.Name,
                    DateTime.UtcNow
                ),
                routingKey: "store.deactivated",
                cancellationToken
            );

            return true;
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapDelete(
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
