using System.Security.Claims;

namespace AccountContentService.Api.Common
{
    public static class UserContext
    {
        public static Guid GetUserId(HttpContext context)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.Parse(userId);
        }
    }
}
