using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.LiveSessions;

public sealed class ReviewSongRequestRequest
{
    [Required(ErrorMessage = "Action is required")]
    public string Action { get; set; } = null!;

    [StringLength(1000, ErrorMessage = "Reject reason cannot exceed 1000 characters")]
    public string? RejectReason { get; set; }
}
