using LiveSessionService.Application.Abstractions.Messaging;

namespace LiveSessionService.Application.Features.Stations.Commands.RestartStation;

public sealed record RestartStationCommand(Guid StationId) : ICommand<bool>;
