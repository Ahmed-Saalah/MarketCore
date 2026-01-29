using Core.Messaging;

namespace Payment.API.Messages;

[MessageKey("Payment.PaymentSucceededEvent")]
public sealed record PaymentSucceededEvent(
    Guid OrderId,
    Guid PaymentId,
    decimal Amount,
    string PaymentIntentId
);
