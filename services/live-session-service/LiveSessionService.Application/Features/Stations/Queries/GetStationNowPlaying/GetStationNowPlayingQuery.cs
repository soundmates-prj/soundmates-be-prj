using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.NowPlaying;

namespace LiveSessionService.Application.Features.Stations.Queries.GetStationNowPlaying;

/// <summary>
/// Query to get live now playing data from AzuraCast for a station.
/// Uses the station's local Guid to look up the AzuraCast external station ID.
/// </summary>
public sealed record GetStationNowPlayingQuery(Guid StationId) : IQuery<StationNowPlayingResult>;
