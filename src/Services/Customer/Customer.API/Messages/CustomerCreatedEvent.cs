using Core.Messaging;

namespace Customer.API.Messages;

[MessageKey("Customer.CustomerCreatedEvent")]
public sealed record CustomerCreatedEvent(
    Guid CustomerId,
    int IdentityId,
    string Email,
    string DisplayName,
    DateTime CreatedAt
);
