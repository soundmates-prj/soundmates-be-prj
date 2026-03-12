using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Playlists;

namespace LiveSessionService.Application.Features.Playlists.Queries.GetPlaylistsByStation;

/// <summary>
/// Query to get all playlists for a specific station
/// </summary>
public sealed record GetPlaylistsByStationQuery(Guid StationId) : IQuery<List<PlaylistResult>>;
