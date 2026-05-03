using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Stations;

namespace LiveSessionService.Application.Features.Stations.Commands.CreateStation;

/// <summary>
/// Command to create a new station locally and push to AzuraCast
/// </summary>
public sealed record CreateStationCommand(
    string StationName,
    string? Description,
    string? ShortCode) : ICommand<StationResult>;
