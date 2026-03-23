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
        {
            return Result<UserPlaylistResult>.Failure("Playlist name is required", ErrorCode.BadRequest);
        }

        if (command.Weight < 0)
        {
            return Result<UserPlaylistResult>.Failure("Weight cannot be negative", ErrorCode.BadRequest);
        }

        var playlist = new UserPlaylist
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            ExternalPlaylistId = 0,
            PlaylistName = command.PlaylistName.Trim(),
            Type = PlaylistType.Default,
            Source = PlaylistSource.Songs,
            PlaylistOrder = command.PlaylistOrder,
            IsEnabled = command.IsEnabled,
            IncludeInRequests = command.IncludeInRequests,
            IncludeInOnDemand = command.IncludeInOnDemand,
            Weight = command.Weight,
            CreatedAt = _dateTimeProvider.UtcNow
        };

        await _userPlaylistRepository.AddAsync(playlist, cancellationToken);

        return Result<UserPlaylistResult>.Success(new UserPlaylistResult
        {
            Id = playlist.Id,
            UserId = playlist.UserId,
            PlaylistName = playlist.PlaylistName,
            IsEnabled = playlist.IsEnabled,
            IncludeInRequests = playlist.IncludeInRequests,
            IncludeInOnDemand = playlist.IncludeInOnDemand,
            PlaylistOrder = playlist.PlaylistOrder,
            Weight = playlist.Weight,
            TotalTracks = 0,
            CreatedAt = playlist.CreatedAt,
            UpdatedAt = playlist.UpdatedAt
        });
    }
}
