using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Store.API.Data;

namespace Store.API.Features;

public sealed class GetStoreById
{
    public sealed record Response(
        Guid Id,
        string Name,
        string Description,
        string? LogoUrl,
        string? CoverImageUrl,
        string OwnerName,
        DateTime JoinedAt
    );

    public sealed record Request(Guid Id) : IRequest<Result<Response>>;

    public class Handler(StoreDbContext dbContext) : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var store = await dbContext
                .Stores.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (store is null || !store.IsActive)
            {
                return new NotFound("Store not found.");
            }

            return new Response(
                store.Id,
                store.Name,
                store.Description,
                store.LogoUrl,
                store.CoverImageUrl,
                store.OwnerName,
                store.CreatedAt
            );
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "api/stores/{id:guid}",
                    async (Guid id, IMediator mediator) =>
                    {
                        var result = await mediator.Send(new Request(id));
                        return result.ToHttpResult();
                    }
                )
                .AllowAnonymous()
                .WithTags("Stores");
        }
    }
}
