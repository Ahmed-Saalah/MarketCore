namespace Order.API.Messages;

public record CreatePaymentCommand(Guid OrderId, Guid UserId, decimal Amount, string Currency);
