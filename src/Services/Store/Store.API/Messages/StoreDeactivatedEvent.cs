using Core.Messaging;

namespace Store.API.Messages;

[MessageKey("Store.StoreDeactivatedEvent")]
public sealed record StoreDeactivatedEvent(
    Guid StoreId,
    int OwnerIdentityId,
    string OwnerName,
    string OwnerEmail,
    string OwnerPhoneNumber,
    string StoreName,
    DateTime Timestamp
);
