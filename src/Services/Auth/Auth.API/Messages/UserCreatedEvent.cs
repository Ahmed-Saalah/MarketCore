namespace Auth.API.Messages;

public sealed record UserCreatedEvent(
    int UserId,
    string Username,
    string Email,
    string? PhoneNumber,
    string DisplayName,
    string? Role
);
