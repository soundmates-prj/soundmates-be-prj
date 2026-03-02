using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Music;

namespace LiveSessionService.Application.Features.Music.Commands.UploadMusic;

/// <summary>
/// Command to upload music file
/// </summary>
public sealed record UploadMusicCommand(
Guid StationId,
Guid UploadedByUserId,
string Title,
string Artist,
string? Album,
Stream FileStream,
string FileName,
string ContentType) : ICommand<MusicResult>;
