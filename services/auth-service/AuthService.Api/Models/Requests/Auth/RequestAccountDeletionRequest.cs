using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models.Requests.Auth;

/// <summary>
/// Request payload for member-initiated permanent account deletion.
/// Requires typing "XÓA TÀI KHOẢN" to confirm intent.
/// </summary>
public class RequestAccountDeletionRequest
{
    /// <summary>
    /// Member's current password — required for security verification.
    /// </summary>
    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    public string Password { get; set; } = null!;

    /// <summary>
    /// Confirmation text — must exactly equal "XÓA TÀI KHOẢN".
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập 'XÓA TÀI KHOẢN' để xác nhận")]
    public string ConfirmationText { get; set; } = null!;
}
