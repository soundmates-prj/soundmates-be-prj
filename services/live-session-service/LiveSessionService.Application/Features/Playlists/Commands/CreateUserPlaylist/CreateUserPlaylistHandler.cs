using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Playlists.Commands.CreateUserPlaylist;

public sealed class CreateUserPlaylistHandler : ICommandHandler<CreateUserPlaylistCommand, UserPlaylistResult>
{
    private readonly IUserPlaylistRepository _userPlaylistRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateUserPlaylistHandler(
        IUserPlaylistRepository userPlaylistRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _userPlaylistRepository = userPlaylistRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<UserPlaylistResult>> Handle(CreateUserPlaylistCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.PlaylistName))
            return Result<UserPlaylistResult>.Failure("Playlist name is required", ErrorCode.BadRequest);

        var playlist = new UserPlaylist
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            PlaylistName = command.PlaylistName.Trim(),
            Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim(),
            ThumbnailUrl = string.IsNullOrWhiteSpace(command.ThumbnailUrl) ? null : command.ThumbnailUrl.Trim(),
            Visibility = command.Visibility,
            Type = PlaylistType.Default,
            IsEnabled = command.IsEnabled,
            CreatedAt = _dateTimeProvider.UtcNow
        };

        await _userPlaylistRepository.AddAsync(playlist, cancellationToken);

        return Result<UserPlaylistResult>.Success(new UserPlaylistResult
        {
            Id = playlist.Id,
            UserId = playlist.UserId,
            PlaylistName = playlist.PlaylistName,
            Description = playlist.Description,
            ThumbnailUrl = playlist.ThumbnailUrl,
            Visibility = playlist.Visibility,
            IsEnabled = playlist.IsEnabled,
            TotalTracks = 0,
            CreatedAt = playlist.CreatedAt,
            UpdatedAt = playlist.UpdatedAt
        });
    }
}
