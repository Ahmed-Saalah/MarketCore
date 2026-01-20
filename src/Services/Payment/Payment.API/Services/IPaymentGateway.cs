namespace Payment.API.Services;

public record PaymentResult(
    bool IsSuccess,
    string? PaymentIntentId,
    string? ClientSecret,
    string? ErrorMessage
);

public interface IPaymentGateway
{
    Task<PaymentResult> CreatePaymentIntentAsync(decimal amount, string currency, Guid orderId);
}
