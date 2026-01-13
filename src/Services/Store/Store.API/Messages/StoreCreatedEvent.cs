namespace Store.API.Messages;

public sealed record StoreCreatedEvent(
    Guid StoreId,
    int OwnerIdentityId,
    string OwnerName,
    string OwnerEmail,
    DateTime CreatedAt
);
