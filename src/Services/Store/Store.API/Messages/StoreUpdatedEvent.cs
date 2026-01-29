using Core.Messaging;

namespace Store.API.Messages;

[MessageKey("Store.StoreUpdatedEvent")]
public sealed record StoreUpdatedEvent(
    Guid StoreId,
    int OwnerIdentityId,
    string OwnerName,
    string OwnerEmail,
    DateTime UpdatedAt
);
