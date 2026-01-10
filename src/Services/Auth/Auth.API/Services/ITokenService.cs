using Auth.API.Models;

namespace Auth.API.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user, IList<string> roles);
    RefreshToken GenerateRefreshToken(string ipAddress);
}
