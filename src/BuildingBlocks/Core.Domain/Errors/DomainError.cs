namespace Core.Domain.Errors;

public interface IDomainError
{
    string Code { get; }
}

public abstract class DomainError : IDomainError
{
    public abstract string Code { get; }
}
