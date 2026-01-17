using System.Security.Claims;

namespace Warehouse.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetStoreId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("store_id");

        if (claim is null)
            return null;

        return Guid.TryParse(claim.Value, out var id) ? id : null;
    }

    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }
}
