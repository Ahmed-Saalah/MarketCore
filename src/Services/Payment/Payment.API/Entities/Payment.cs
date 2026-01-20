namespace Payment.API.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public Guid StoreId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";

    public string? StripePaymentIntentId { get; set; }
    public string? StripeClientSecret { get; set; }

    public string Status { get; set; }
    public string? FailureMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
