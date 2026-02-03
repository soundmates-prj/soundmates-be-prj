using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;

namespace LiveSessionService.Application.Features.Stations.Commands.SyncStations;

/// <summary>
/// Command to sync all stations from AzuraCast to local database
/// </summary>
public sealed record SyncStationsCommand : ICommand<SyncStationsResult>;
