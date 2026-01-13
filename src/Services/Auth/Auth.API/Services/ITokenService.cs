using System.Security.Claims;
using Auth.API.Models;

namespace Auth.API.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user, IList<string> roles, IList<Claim> userClaims);
    RefreshToken GenerateRefreshToken(string ipAddress);
}
