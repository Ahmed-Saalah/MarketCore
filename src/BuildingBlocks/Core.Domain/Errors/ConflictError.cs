using System.Net;

namespace Core.Domain.Errors;

[HttpCode(HttpStatusCode.Conflict)]
public class ConflictError(string message = "Conflict") : DomainError
{
    public override string Code => "conflict";

    public override string Message => message;
}
