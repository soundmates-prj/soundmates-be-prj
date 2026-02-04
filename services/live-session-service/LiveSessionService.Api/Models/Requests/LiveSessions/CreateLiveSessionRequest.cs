using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.LiveSessions;

/// <summary>
/// Request to create a new live session
/// </summary>
public sealed class CreateLiveSessionRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Station ID is required")]
    public Guid StationId { get; set; }

    [Required(ErrorMessage = "Session name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Session name must be between 3 and 100 characters")]
    public string SessionName { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}
