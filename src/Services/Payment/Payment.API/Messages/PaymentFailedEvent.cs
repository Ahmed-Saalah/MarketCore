using Core.Messaging;

namespace Payment.API.Messages;

[MessageKey("Payment.PaymentFailedEvent")]
public sealed record PaymentFailedEvent(Guid OrderId, string Reason);
