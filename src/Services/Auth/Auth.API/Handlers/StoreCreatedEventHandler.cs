using System.Security.Claims;
using Auth.API.Models;
using Core.Messaging;
using Microsoft.AspNetCore.Identity;

namespace Auth.API.Handlers;

public sealed class StoreCreatedEventHandler
{
    public sealed class Handler(UserManager<User> userManager) : IEventHandler<Event>
    {
        public async Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
        {
            var user = await userManager.FindByIdAsync(@event.OwnerIdentityId.ToString());
            if (user is null)
            {
                return;
            }

            var claim = new Claim("store_id", @event.StoreId.ToString());

            var existingClaims = await userManager.GetClaimsAsync(user);
            if (!existingClaims.Any(c => c.Type == "store_id" && c.Value == claim.Value))
            {
                await userManager.AddClaimAsync(user, claim);
            }
        }
    }

    [MessageKey("Store.StoreCreatedEvent")]
    public sealed record Event(
        Guid StoreId,
        int OwnerIdentityId,
        string OwnerName,
        string OwnerEmail,
        DateTime CreatedAt
    );
}
