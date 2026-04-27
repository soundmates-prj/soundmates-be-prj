using System.Security.Claims;

namespace AccountContentService.Api.Common
{
    public static class UserContext
    {
        public static Guid GetUserId(HttpContext context)
        {
            var userIdClaim =
                context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub")
                ?? context.User.FindFirstValue("nameid")
                ?? context.User.FindFirstValue("userId");

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Cannot resolve authenticated user id from token claims.");
            }

            return userId;
        }
    }
}
