namespace Store.API.Messages;

public sealed record StoreDeactivatedEvent(
    Guid Id,
    int OwnerIdentityId,
    string OwnerName,
    string OwnerEmail,
    string OwnerPhoneNumber,
    string StoreName,
    DateTime Timestamp
);
