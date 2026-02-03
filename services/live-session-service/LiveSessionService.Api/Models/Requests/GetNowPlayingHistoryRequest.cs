using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests;

/// <summary>
/// Request to get now playing history for a session
/// </summary>
public class GetNowPlayingHistoryRequest
{
    [Required]
    public Guid SessionId { get; set; }

    /// <summary>
    /// Maximum number of items to return (default: 50, max: 100)
    /// </summary>
    [Range(1, 100)]
    public int Limit { get; set; } = 50;
}
