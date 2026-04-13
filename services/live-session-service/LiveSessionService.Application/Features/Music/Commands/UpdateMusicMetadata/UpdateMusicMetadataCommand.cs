using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Music;

namespace LiveSessionService.Application.Features.Music.Commands.UpdateMusicMetadata;

public sealed record UpdateMusicMetadataCommand(
    Guid StationId,
    Guid MusicId,
    string? Title,
    string? Artist,
    string? Album,
    string? Lyrics) : ICommand<MusicResult>;
