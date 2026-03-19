using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.LiveSessions;

public sealed class CreateSongRequestRequest
{
    [Required(ErrorMessage = "Media file ID is required")]
    public Guid MediaFileId { get; set; }

    [Required(ErrorMessage = "Requested by user ID is required")]
    public Guid RequestedByUserId { get; set; }

    [StringLength(1000, ErrorMessage = "Message cannot exceed 1000 characters")]
    public string? Message { get; set; }
}
