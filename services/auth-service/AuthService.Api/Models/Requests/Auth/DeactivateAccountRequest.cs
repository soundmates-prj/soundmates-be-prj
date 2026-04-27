using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models.Requests.Auth;

/// <summary>
/// Request payload for member-initiated account deactivation.
/// </summary>
public class DeactivateAccountRequest
{
    /// <summary>
    /// Member's current password — required for security verification.
    /// </summary>
    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    public string Password { get; set; } = null!;

    /// <summary>
    /// Reason for deactivation.
    /// Allowed values: "Tạm nghỉ", "Quá nhiều thông báo", "Lý do cá nhân", "Khác"
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn lý do vô hiệu hóa")]
    public string Reason { get; set; } = null!;

    /// <summary>
    /// Optional additional feedback from the member.
    /// </summary>
    public string? AdditionalNote { get; set; }
}
