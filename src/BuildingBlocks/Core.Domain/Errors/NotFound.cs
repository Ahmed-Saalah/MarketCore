using System.Net;

namespace Core.Domain.Errors;

[HttpCode(HttpStatusCode.NotFound)]
public class NotFound(string message = "Not found") : DomainError
{
    public override string Code => "not_found";

    public string Message { get; } = message;
}
