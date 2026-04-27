using LiveSessionService.Application.Abstractions.Messaging;

namespace LiveSessionService.Application.Features.Stations.Commands.ReloadStation;

public sealed record ReloadStationCommand(Guid StationId) : ICommand<bool>;
