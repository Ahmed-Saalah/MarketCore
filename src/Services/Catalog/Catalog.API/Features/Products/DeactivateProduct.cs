using System.Security.Claims;
using Catalog.API.Data;
using Catalog.API.Extensions;
using Catalog.API.Messages.Products;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using Core.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Features.Products;

public sealed class DeactivateProduct
{
    public sealed record Request(Guid Id, Guid StoreId) : IRequest<Result<bool>>;

    public class Handler(CatalogDbContext dbContext, IEventPublisher eventPublisher)
        : IRequestHandler<Request, Result<bool>>
    {
        public async Task<Result<bool>> Handle(Request request, CancellationToken cancellationToken)
        {
            var product = await dbContext.Products.FirstOrDefaultAsync(
                p => p.Id == request.Id,
                cancellationToken
            );

            if (product is null)
            {
                return new NotFound("Product not found");
            }

            if (product.StoreId != request.StoreId)
            {
                return new ForbiddenError();
            }

            product.IsActive = false;

            await dbContext.SaveChangesAsync(cancellationToken);

            await eventPublisher.PublishAsync(
                new ProductDeactivatedEvent(product.Id, DateTime.UtcNow),
                cancellationToken
            );

            return true;
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPatch(
                    "api/products/{id:guid}/deactivate",
                    async (Guid id, IMediator mediator, ClaimsPrincipal user) =>
                    {
                        if (user.GetStoreId() is not { } storeId)
                        {
                            return Results.Unauthorized();
                        }

                        var result = await mediator.Send(new Request(id, storeId));

                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization("Seller")
                .WithTags("Products");
        }
    }
}
