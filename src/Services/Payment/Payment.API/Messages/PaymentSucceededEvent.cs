namespace Payment.API.Messages;

public sealed record PaymentSucceededEvent(
    Guid OrderId,
    Guid PaymentId,
    decimal Amount,
    string PaymentIntentId
);
