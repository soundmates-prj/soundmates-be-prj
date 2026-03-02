using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Music;

namespace LiveSessionService.Application.Features.Music.Queries.GetMediaFilesByStation;

/// <summary>
/// Query to get all media files associated with a specific station.
/// </summary>
public sealed record GetMediaFilesByStationQuery(Guid StationId) : IQuery<List<MusicResult>>;

