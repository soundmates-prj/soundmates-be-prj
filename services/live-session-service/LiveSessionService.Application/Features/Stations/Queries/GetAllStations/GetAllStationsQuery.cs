using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Stations;

namespace LiveSessionService.Application.Features.Stations.Queries.GetAllStations;

/// <summary>
/// Query to get all stations from local database
/// </summary>
public sealed record GetAllStationsQuery : IQuery<List<StationResult>>;
