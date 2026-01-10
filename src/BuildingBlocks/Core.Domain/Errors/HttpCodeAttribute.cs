using System.Net;

namespace Core.Domain.Errors;

[AttributeUsage(AttributeTargets.Class)]
public class HttpCodeAttribute : Attribute
{
    public HttpStatusCode Code { get; protected set; }

    public HttpCodeAttribute(HttpStatusCode code)
    {
        Code = code;
    }
}
