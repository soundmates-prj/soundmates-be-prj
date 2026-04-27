using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Playlists.Commands.UpdateUserPlaylist;

public sealed class UpdateUserPlaylistHandler : ICommandHandler<UpdateUserPlaylistCommand, UserPlaylistResult>
{
    private readonly IUserPlaylistRepository _userPlaylistRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateUserPlaylistHandler(
        IUserPlaylistRepository userPlaylistRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _userPlaylistRepository = userPlaylistRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<UserPlaylistResult>> Handle(UpdateUserPlaylistCommand command, CancellationToken cancellationToken)
    {
        var playlist = await _userPlaylistRepository.GetByIdAsync(command.PlaylistId, cancellationToken);
        if (playlist == null)
            return Result<UserPlaylistResult>.Failure("Playlist not found", ErrorCode.NotFound);

        if (playlist.UserId != command.UserId)
            return Result<UserPlaylistResult>.Failure("You do not have permission to update this playlist", ErrorCode.Forbidden);

        if (!string.IsNullOrWhiteSpace(command.PlaylistName))
            playlist.PlaylistName = command.PlaylistName.Trim();

        if (command.Description is not null)
            playlist.Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();

        if (command.ThumbnailUrl is not null)
            playlist.ThumbnailUrl = string.IsNullOrWhiteSpace(command.ThumbnailUrl) ? null : command.ThumbnailUrl.Trim();

        if (command.Visibility.HasValue)
            playlist.Visibility = command.Visibility.Value;

        if (command.IsEnabled.HasValue)
            playlist.IsEnabled = command.IsEnabled.Value;

        playlist.UpdatedAt = _dateTimeProvider.UtcNow;

        await _userPlaylistRepository.UpdateAsync(playlist, cancellationToken);

        return Result<UserPlaylistResult>.Success(new UserPlaylistResult
        {
            Id = playlist.Id,
            UserId = playlist.UserId,
            PlaylistName = playlist.PlaylistName,
            Description = playlist.Description,
            ThumbnailUrl = playlist.ThumbnailUrl,
            Visibility = playlist.Visibility,
            IsEnabled = playlist.IsEnabled,
            TotalTracks = playlist.UserPlaylistMedias.Count,
            CreatedAt = playlist.CreatedAt,
            UpdatedAt = playlist.UpdatedAt
        });
    }
}
