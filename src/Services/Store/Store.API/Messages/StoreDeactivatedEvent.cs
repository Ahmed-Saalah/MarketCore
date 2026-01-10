namespace Store.API.Messages;

public sealed record StoreDeactivatedEvent(
    Guid StoreId,
    int OwnerIdentityId,
    string OwnerName,
    string OwnerEmail,
    string OwnerPhoneNumber,
    string StoreName,
    DateTime Timestamp
);
