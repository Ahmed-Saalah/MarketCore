using System.Net;

namespace Core.Domain.Errors;

[HttpCode(HttpStatusCode.Unauthorized)]
public class UnauthorizedError(string message = "Unauthorized") : DomainError
{
    public override string Code => "unauthorized";

    public override string Message => message;
}
