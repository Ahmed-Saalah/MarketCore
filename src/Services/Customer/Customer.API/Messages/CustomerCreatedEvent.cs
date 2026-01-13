namespace Customer.API.Messages;

public sealed record CustomerCreatedEvent(
    Guid CustomerId,
    int IdentityId,
    string Email,
    string DisplayName,
    DateTime CreatedAt
);
