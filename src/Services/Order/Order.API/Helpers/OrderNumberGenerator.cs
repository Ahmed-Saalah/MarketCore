namespace Order.API.Helpers;

public static class OrderNumberGenerator
{
    public static string Generate()
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = GenerateRandomBase36(4);

        return $"ORD-{datePart}-{randomPart}";
    }

    private static string GenerateRandomBase36(int length)
    {
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var random = new Random();
        return new string(
            Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray()
        );
    }
}
