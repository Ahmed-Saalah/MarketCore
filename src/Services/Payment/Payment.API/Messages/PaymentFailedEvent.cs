namespace Payment.API.Messages;

public sealed record PaymentFailedEvent(Guid OrderId, string Reason);
