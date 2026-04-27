using System.ComponentModel.DataAnnotations;

namespace LiveSessionService.Api.Models.Requests.Stations;

/// <summary>
/// Request for syncing stations from AzuraCast
/// No parameters needed - will sync all stations
/// </summary>
public sealed class SyncStationsRequest
{
    // Empty - just a marker for future extensibility
    // Could add: bool ForceSync, List<int> StationIds, etc.
}
