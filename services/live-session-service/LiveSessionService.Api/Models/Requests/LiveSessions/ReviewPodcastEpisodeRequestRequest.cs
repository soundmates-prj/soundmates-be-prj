using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.LiveSessions;

public sealed class ReviewPodcastEpisodeRequestRequest
{
    [Required]
    public bool IsApproved { get; init; }
    public string? RejectReason { get; init; }
}
