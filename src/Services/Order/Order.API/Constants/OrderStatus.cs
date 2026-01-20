namespace Order.API.Constants;

public sealed class OrderStatus
{
    public const string Pending = "Pending";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string Failed = "Failed";
    public const string Paid = "Paid";
    public const string PaymentFailed = "PaymentFailed";
}
