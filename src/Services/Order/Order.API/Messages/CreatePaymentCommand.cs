using Core.Messaging;

namespace Order.API.Messages;

[MessageKey("Order.CreatePaymentCommand")]
public record CreatePaymentCommand(Guid OrderId, Guid UserId, decimal Amount, string Currency);
