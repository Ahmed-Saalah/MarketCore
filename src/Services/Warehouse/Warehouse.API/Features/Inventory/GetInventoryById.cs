using Core.Domain.Abstractions;
using Core.Domain.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Warehouse.API.Data;

namespace Warehouse.API.Features.Inventory;

public sealed class GetInventoryById
{
    public sealed class Response : Result<ResponseDto>
    {
        public static implicit operator Response(ResponseDto success) => new() { Value = success };

        public static implicit operator Response(DomainError error) => new() { Error = error };
    }

    public sealed record ResponseDto(
        Guid ProductId,
        Guid StoreId,
        string Sku,
        int QuantityOnHand,
        int ReservedQuantity,
        int AvailableStock
    );

    public record Request(Guid ProductId) : IRequest<Response>;

    internal sealed class Handler(WarehouseDbContext dbContext) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var inventory = await dbContext.Inventory.FirstOrDefaultAsync(
                i => i.ProductId == request.ProductId,
                cancellationToken
            );

            if (inventory is null)
            {
                return new NotFound();
            }

            var responseDto = new ResponseDto(
                ProductId: inventory.ProductId,
                StoreId: inventory.StoreId,
                Sku: inventory.Sku,
                QuantityOnHand: inventory.QuantityOnHand,
                ReservedQuantity: inventory.ReservedQuantity,
                AvailableStock: inventory.AvailableStock
            );

            return responseDto;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "/api/inventory/{productId:guid}",
                    async (Guid productId, IMediator mediator) =>
                    {
                        var response = await mediator.Send(new Request(productId));
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Inventory")
                .WithName("GetInventory")
                .WithSummary("Get current stock levels for a product");
        }
    }
}
