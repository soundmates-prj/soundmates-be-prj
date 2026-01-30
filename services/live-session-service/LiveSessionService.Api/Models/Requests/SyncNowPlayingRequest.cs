using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests;

/// <summary>
/// Request to sync/refresh now playing from AzuraCast
/// </summary>
public class SyncNowPlayingRequest
{
    [Required]
    public Guid SessionId { get; set; }
}
