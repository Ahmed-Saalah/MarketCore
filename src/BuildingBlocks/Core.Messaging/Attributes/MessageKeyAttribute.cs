namespace Core.Messaging;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class MessageKeyAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}
