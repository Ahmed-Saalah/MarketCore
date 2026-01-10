using System.Net;

namespace Core.Domain.Errors;

[HttpCode(HttpStatusCode.Forbidden)]
public class ForbiddenError(string message = "Forbidden") : DomainError
{
    public override string Code => "forbidden";
    public string Message => message;
}
