using System.Security.Claims;

namespace AiService.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetCurrentUserId(this ClaimsPrincipal user, out Guid userId)
    {
        userId = Guid.Empty;

        var claim = user.FindFirst(ClaimTypes.NameIdentifier)
                   ?? user.FindFirst("sub")
                   ?? user.Claims.FirstOrDefault(c => c.Type == "user_id");

        return claim != null && Guid.TryParse(claim.Value, out userId);
    }
}

