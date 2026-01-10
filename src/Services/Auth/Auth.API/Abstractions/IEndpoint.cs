namespace Auth.API.Abstractions;

public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
