using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests;

/// <summary>
/// Request to get current now playing for a session
/// </summary>
public class GetNowPlayingRequest
{
    // Session Id (Guid)
    [Required]
    public Guid Id { get; set; }
}
