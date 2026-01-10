using System.Net;

namespace Core.Domain.Errors;

[HttpCode(HttpStatusCode.BadRequest)]
public class MultipleError : DomainError
{
    public override string Code => "multiple_error";

    public IEnumerable<IDomainError> Errors { get; }

    public MultipleError(IEnumerable<IDomainError> errors)
    {
        Errors = errors;
    }
}
