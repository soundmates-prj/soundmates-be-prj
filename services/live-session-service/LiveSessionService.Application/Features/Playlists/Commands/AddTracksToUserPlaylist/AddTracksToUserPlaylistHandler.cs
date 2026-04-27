using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Playlists.Commands.AddTracksToUserPlaylist;

public sealed class AddTracksToUserPlaylistHandler : ICommandHandler<AddTracksToUserPlaylistCommand, List<PlaylistMediaResult>>
{
    private readonly IUserPlaylistRepository _userPlaylistRepository;
    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddTracksToUserPlaylistHandler(
        IUserPlaylistRepository userPlaylistRepository,
        IMediaFileRepository mediaFileRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _userPlaylistRepository = userPlaylistRepository;
        _mediaFileRepository = mediaFileRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<List<PlaylistMediaResult>>> Handle(AddTracksToUserPlaylistCommand command, CancellationToken cancellationToken)
    {
        var mediaIds = command.MediaIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (mediaIds.Count == 0)
        {
            return Result<List<PlaylistMediaResult>>.Failure("At least one media id is required", ErrorCode.BadRequest);
        }

        var playlist = await _userPlaylistRepository.GetByIdAsync(command.PlaylistId, cancellationToken);
        if (playlist == null)
        {
            return Result<List<PlaylistMediaResult>>.Failure("Playlist not found", ErrorCode.NotFound);
        }

        if (playlist.UserId != command.UserId)
        {
            return Result<List<PlaylistMediaResult>>.Failure("You do not have permission to modify this playlist", ErrorCode.Forbidden);
        }

        var existingMediaIds = playlist.UserPlaylistMedias
            .Where(x => x.MediaFileId.HasValue)
            .Select(x => x.MediaFileId!.Value)
            .ToHashSet();

        var idsToAdd = mediaIds
            .Where(x => !existingMediaIds.Contains(x))
            .ToList();

        if (idsToAdd.Count == 0)
        {
            return Result<List<PlaylistMediaResult>>.Success(new List<PlaylistMediaResult>());
        }

        var mediaFiles = await _mediaFileRepository.GetByIdsAsync(idsToAdd, cancellationToken);
        if (mediaFiles.Count != idsToAdd.Count)
        {
            return Result<List<PlaylistMediaResult>>.Failure("One or more media files were not found", ErrorCode.NotFound);
        }

        var now = _dateTimeProvider.UtcNow;
        var tracks = mediaFiles.Select(x => new UserPlaylistMedia
        {
            Id = Guid.NewGuid(),
            UserPlaylistId = playlist.Id,
            MediaFileId = x.Id,
            CreatedAt = now
        }).ToList();

        await _userPlaylistRepository.AddTracksAsync(tracks, cancellationToken);

        var result = mediaFiles.Select(x => new PlaylistMediaResult
        {
            Id = tracks.First(t => t.MediaFileId == x.Id).Id,
            PlaylistId = playlist.Id,
            MediaFileId = x.Id,
            Title = x.Title,
            Artist = x.Artist,
            Album = x.Album,
            ArtworkUrl = x.ArtUrl,
            FileUrl = !string.IsNullOrWhiteSpace(x.FileUrl)
                ? x.FileUrl
                : x.FilePath,
            FileType = x.FileType,
            FileSize = x.FileSizeBytes,
            DurationSeconds = x.DurationSeconds,
            AddedAt = now
        }).ToList();

        return Result<List<PlaylistMediaResult>>.Success(result);
    }
}
