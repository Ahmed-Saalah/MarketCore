using Microsoft.AspNetCore.Routing;

namespace Core.Domain.Abstractions;

public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
