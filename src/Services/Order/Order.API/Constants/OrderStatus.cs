namespace Order.API.Constants;

public sealed class OrderStatus
{
    public const string Pending = "Pending";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";
    public const string PendingPayment = "PendingPayment";
    public const string Paid = "Paid";
}
