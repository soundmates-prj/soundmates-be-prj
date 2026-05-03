using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.Stations;

/// <summary>
/// Request to create a new station
/// </summary>
public sealed class CreateStationRequest
{
    [Required(ErrorMessage = "Station name is required")]
    [StringLength(100, MinimumLength = 3)]
    public string StationName { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    [RegularExpression("^[a-z0-9_-]+$", ErrorMessage = "Short code must contain only lowercase letters, numbers, hyphens and underscores")]
    public string? ShortCode { get; set; }
}
