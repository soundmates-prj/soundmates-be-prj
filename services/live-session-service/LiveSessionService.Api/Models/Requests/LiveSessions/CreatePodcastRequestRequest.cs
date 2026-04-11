using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.LiveSessions;

public sealed class CreatePodcastRequestRequest
{
    [Required(ErrorMessage = "Live session ID is required")]
    public Guid LiveSessionId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
    public string Title { get; set; } = null!;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Script text is required")]
    public string ScriptText { get; set; } = null!;

    [Required(ErrorMessage = "Audio URL is required")]
    public string AudioUrl { get; set; } = null!;

    [Range(1, 36000, ErrorMessage = "Duration must be between 1 and 36000 seconds")]
    public int DurationSeconds { get; set; }

    [Required(ErrorMessage = "Voice code is required")]
    public string VoiceCode { get; set; } = null!;

    [StringLength(200, ErrorMessage = "Voice display name cannot exceed 200 characters")]
    public string? VoiceDisplayName { get; set; }
}