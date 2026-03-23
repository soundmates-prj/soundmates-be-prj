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
        {
            return Result<UserPlaylistResult>.Failure("Playlist not found", ErrorCode.NotFound);
        }

        if (playlist.UserId != command.UserId)
        {
            return Result<UserPlaylistResult>.Failure("You do not have permission to update this playlist", ErrorCode.Forbidden);
        }

        if (command.Weight.HasValue && command.Weight.Value < 0)
        {
            return Result<UserPlaylistResult>.Failure("Weight cannot be negative", ErrorCode.BadRequest);
        }

        if (!string.IsNullOrWhiteSpace(command.PlaylistName))
        {
            playlist.PlaylistName = command.PlaylistName.Trim();
        }

        if (command.IncludeInRequests.HasValue)
        {
            playlist.IncludeInRequests = command.IncludeInRequests.Value;
        }

        if (command.IncludeInOnDemand.HasValue)
        {
            playlist.IncludeInOnDemand = command.IncludeInOnDemand.Value;
        }

        if (command.IsEnabled.HasValue)
        {
            playlist.IsEnabled = command.IsEnabled.Value;
        }

        if (command.PlaylistOrder.HasValue)
        {
            playlist.PlaylistOrder = command.PlaylistOrder.Value;
        }

        if (command.Weight.HasValue)
        {
            playlist.Weight = command.Weight.Value;
        }

        playlist.UpdatedAt = _dateTimeProvider.UtcNow;

        await _userPlaylistRepository.UpdateAsync(playlist, cancellationToken);

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
            TotalTracks = playlist.UserPlaylistMedias.Count,
            CreatedAt = playlist.CreatedAt,
            UpdatedAt = playlist.UpdatedAt
        });
    }
}
