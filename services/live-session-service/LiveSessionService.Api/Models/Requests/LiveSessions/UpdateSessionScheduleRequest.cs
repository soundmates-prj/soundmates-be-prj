using System.ComponentModel.DataAnnotations;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Api.Models.Requests.LiveSessions;

public sealed class UpdateSessionScheduleRequest
{
    [Required(ErrorMessage = "Start time is required")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "End time is required")]
    public TimeOnly EndTime { get; set; }

    [Required(ErrorMessage = "Start date is required")]
    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [StringLength(300, ErrorMessage = "Title cannot exceed 300 characters")]
    public string? Title { get; set; }

    public bool? IsRecurring { get; set; }

    public DaysOfWeek? DaysOfWeek { get; set; }
}
