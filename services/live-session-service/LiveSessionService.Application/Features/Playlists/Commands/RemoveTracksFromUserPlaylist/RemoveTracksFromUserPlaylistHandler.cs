using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Playlists.Commands.RemoveTracksFromUserPlaylist;

public sealed class RemoveTracksFromUserPlaylistHandler : ICommandHandler<RemoveTracksFromUserPlaylistCommand>
{
    private readonly IUserPlaylistRepository _userPlaylistRepository;

    public RemoveTracksFromUserPlaylistHandler(IUserPlaylistRepository userPlaylistRepository)
    {
        _userPlaylistRepository = userPlaylistRepository;
    }

    public async Task<Result> Handle(RemoveTracksFromUserPlaylistCommand command, CancellationToken cancellationToken)
    {
        var mediaIds = command.MediaIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToHashSet();

        if (mediaIds.Count == 0)
        {
            return Result.Failure("At least one media id is required", ErrorCode.BadRequest);
        }

        var playlist = await _userPlaylistRepository.GetByIdAsync(command.PlaylistId, cancellationToken);
        if (playlist == null)
        {
            return Result.Failure("Playlist not found", ErrorCode.NotFound);
        }

        if (playlist.UserId != command.UserId)
        {
            return Result.Failure("You do not have permission to modify this playlist", ErrorCode.Forbidden);
        }

        var tracksToRemove = playlist.UserPlaylistMedias
            .Where(x => x.MediaFileId.HasValue && mediaIds.Contains(x.MediaFileId.Value))
            .ToList();

        if (tracksToRemove.Count == 0)
        {
            return Result.Failure("Tracks not found in this playlist", ErrorCode.NotFound);
        }

        await _userPlaylistRepository.RemoveTracksAsync(tracksToRemove, cancellationToken);

        return Result.Success();
    }
}
