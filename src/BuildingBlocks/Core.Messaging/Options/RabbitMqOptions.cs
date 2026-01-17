namespace Core.Messaging.Options;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = null!;
    public string User { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string Exchange { get; init; } = null!;
    public string Queue { get; init; } = null!;
}
