using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.LiveSessions;

public sealed class UpdateLiveSessionRequest
{
    public Guid? HostUserId { get; set; }

    public Guid? StationId { get; set; }

    [StringLength(100, MinimumLength = 3, ErrorMessage = "Session name must be between 3 and 100 characters")]
    public string? SessionName { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public string? ThumbnailUrl { get; set; }
}
