using System.Security.Claims;

namespace AuthService.Api.Extensions;

/// <summary>
/// Extension hỗ trợ đọc thông tin người dùng từ JWT claims.
/// Chuẩn hóa việc lấy UserId để tái sử dụng ở nhiều controller.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static bool TryGetCurrentUserId(this ClaimsPrincipal user, out Guid userId)
    {
        userId = Guid.Empty;

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                        ?? user.FindFirst("sub")
                        ?? user.Claims.FirstOrDefault(c => c.Type == "user_id");

        return userIdClaim != null && Guid.TryParse(userIdClaim.Value, out userId);
    }
}
