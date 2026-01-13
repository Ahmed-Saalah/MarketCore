using Core.Messaging;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Store.API.Data;
using Store.API.Messages;

namespace Store.API.Features;

public sealed class CreateStore
{
    public record Command(
        int IdentityId,
        string Username,
        string Email,
        string PhoneNumber,
        string DisplayName
    ) : IRequest;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IdentityId).GreaterThan(0);
            RuleFor(x => x.Email).NotEmpty();
            RuleFor(x => x.DisplayName).NotEmpty();
        }
    }

    public class Handler(StoreDbContext dbContext, IEventPublisher eventPublisher)
        : IRequestHandler<Command>
    {
        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            var exists = await dbContext.Stores.AnyAsync(
                s => s.OwnerIdentityId == request.IdentityId,
                cancellationToken
            );

            if (exists)
            {
                return;
            }

            var newStore = new Entities.Store
            {
                Id = Guid.NewGuid(),
                OwnerIdentityId = request.IdentityId,
                OwnerName = request.DisplayName,
                OwnerEmail = request.Email,
                OwnerPhoneNumber = request.PhoneNumber ?? string.Empty,
                Name = $"{request.DisplayName}'s Store",
                Description = $"Welcome to {request.DisplayName}'s official store!",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            dbContext.Stores.Add(newStore);
            await dbContext.SaveChangesAsync(cancellationToken);

            await eventPublisher.PublishAsync(
                new StoreCreatedEvent(
                    StoreId: newStore.Id,
                    OwnerIdentityId: newStore.OwnerIdentityId,
                    OwnerName: newStore.OwnerName,
                    OwnerEmail: newStore.OwnerEmail,
                    CreatedAt: newStore.CreatedAt
                ),
                routingKey: "Store.StoreCreatedEvent",
                cancellationToken: cancellationToken
            );
        }
    }
}
