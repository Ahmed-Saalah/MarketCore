using System.Net;

namespace Core.Domain.Errors;

[HttpCode(HttpStatusCode.Forbidden)]
public sealed class ForbiddenError(string message = "Forbidden") : DomainError
{
    public override string Code => "forbidden";
    public override string Message => message;
}
