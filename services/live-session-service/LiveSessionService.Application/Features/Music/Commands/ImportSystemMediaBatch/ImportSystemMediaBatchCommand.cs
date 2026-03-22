using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Music;

namespace LiveSessionService.Application.Features.Music.Commands.ImportSystemMediaBatch;

public sealed record ImportSystemMediaBatchCommand(
    Guid StationId,
    IReadOnlyCollection<Guid> MediaFileIds) : ICommand<ImportSystemMediaBatchResult>;
