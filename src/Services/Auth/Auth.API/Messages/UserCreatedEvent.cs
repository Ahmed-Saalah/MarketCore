using Core.Messaging;

namespace Auth.API.Messages;

[MessageKey("Auth.UserCreatedEvent")]
public sealed record UserCreatedEvent(
    int UserId,
    string Username,
    string Email,
    string? PhoneNumber,
    string DisplayName,
    string? Role
);
