using System.Security.Claims;
using Auth.API.Models;
using Core.Messaging;
using Microsoft.AspNetCore.Identity;

namespace Auth.API.Handlers;

public sealed class CustomerCreatedEventHandler
{
    public sealed class Handler(UserManager<User> userManager) : IEventHandler<Event>
    {
        public async Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
        {
            var user = await userManager.FindByIdAsync(@event.IdentityId.ToString());
            if (user is null)
            {
                return;
            }

            var claim = new Claim("customer_id", @event.CustomerId.ToString());

            var existingClaims = await userManager.GetClaimsAsync(user);
            if (!existingClaims.Any(c => c.Type == "customer_id" && c.Value == claim.Value))
            {
                await userManager.AddClaimAsync(user, claim);
            }
        }
    }

    [MessageKey("Customer.CustomerCreatedEvent")]
    public sealed record Event(
        Guid CustomerId,
        int IdentityId,
        string Email,
        string DisplayName,
        DateTime CreatedAt
    );
}
