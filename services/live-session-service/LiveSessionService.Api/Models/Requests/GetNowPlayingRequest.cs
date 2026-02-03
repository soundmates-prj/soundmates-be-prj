using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests;

/// <summary>
/// Request to get current now playing for a session
/// </summary>
public class GetNowPlayingRequest
{
    // Id from AzuraCast SectionId
    [Required]
    public int Id { get; set; }
}
