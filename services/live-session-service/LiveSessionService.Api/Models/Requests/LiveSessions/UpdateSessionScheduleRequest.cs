using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.LiveSessions;

public sealed class UpdateSessionScheduleRequest
{
    [Required(ErrorMessage = "Start time is required")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "End time is required")]
    public DateTime EndTime { get; set; }

    [StringLength(300, ErrorMessage = "Title cannot exceed 300 characters")]
    public string? Title { get; set; }

    [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
    public string? Status { get; set; }
}
