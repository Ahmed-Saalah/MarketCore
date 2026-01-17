using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Messaging;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Warehouse.API.Data;
using Warehouse.API.Entities;
using Warehouse.API.Messages;

namespace Warehouse.API.Features.Inventory;

public static class ReceiveStock
{
    public record Request(Guid ProductId, int Quantity, string ReferenceNumber)
        : IRequest<Response>;

    public record ResponseDto(Guid InventoryId, int NewQuantity);

    public sealed class Response : Result<ResponseDto>
    {
        public static implicit operator Response(ResponseDto success) => new() { Value = success };

        public static implicit operator Response(DomainError error) => new() { Error = error };
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be positive.");
            RuleFor(x => x.ReferenceNumber)
                .NotEmpty()
                .WithMessage("Reference number is required for auditing.");
        }
    }

    internal sealed class Handler(WarehouseDbContext dbContext, IEventPublisher eventPublisher)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken ct)
        {
            var inventory = await dbContext.Inventory.FirstOrDefaultAsync(
                i => i.ProductId == request.ProductId,
                ct
            );

            if (inventory is null)
            {
                return new NotFound($"Inventory for ProductId {request.ProductId} not found.");
            }

            var isDuplicate = await dbContext.StockTransactions.AnyAsync(
                t =>
                    t.InventoryId == inventory.Id
                    && t.Type == TransactionType.Restock
                    && t.ReferenceId == request.ReferenceNumber,
                ct
            );

            if (isDuplicate)
            {
                return new ResponseDto(inventory.Id, inventory.QuantityOnHand);
            }

            inventory.QuantityOnHand += request.Quantity;

            var transaction = new StockTransaction
            {
                Id = Guid.NewGuid(),
                InventoryId = inventory.Id,
                StoreId = inventory.StoreId,
                Type = TransactionType.Restock,
                QuantityChanged = request.Quantity,
                ReferenceId = request.ReferenceNumber,
                CreatedAt = DateTime.UtcNow,
            };

            dbContext.StockTransactions.Add(transaction);

            await dbContext.SaveChangesAsync(ct);

            await eventPublisher.PublishAsync(
                new InventoryUpdatedEvent(
                    InventoryId: inventory.Id,
                    ProductId: inventory.ProductId,
                    StoreId: inventory.StoreId,
                    QuantityAdded: request.Quantity,
                    NewQuantityOnHand: inventory.QuantityOnHand,
                    ReferenceNumber: request.ReferenceNumber,
                    OccurredOn: DateTime.UtcNow
                ),
                "Warehouse.InventoryUpdatedEvent",
                ct
            );

            return new ResponseDto(inventory.Id, inventory.QuantityOnHand);
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "/api/inventory/receive",
                    async (Request request, ISender sender) =>
                    {
                        var response = await sender.Send(request);

                        return response.ToHttpResult();
                    }
                )
                .WithTags("Inventory")
                .WithName("ReceiveStock")
                .WithSummary("Add stock to inventory (Restock from Supplier)");
        }
    }
}
