using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Playlists.Commands.UpdatePlaylist;

public sealed class UpdatePlaylistHandler : ICommandHandler<UpdatePlaylistCommand, PlaylistResult>
{
    private readonly IStationPlaylistRepository _playlistRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdatePlaylistHandler(
        IStationPlaylistRepository playlistRepository,
        IAzuraCastClient azuraCastClient,
        IDateTimeProvider dateTimeProvider)
    {
        _playlistRepository = playlistRepository;
        _azuraCastClient = azuraCastClient;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PlaylistResult>> Handle(UpdatePlaylistCommand command, CancellationToken cancellationToken)
    {
        var playlist = await _playlistRepository.GetByIdAsync(command.PlaylistId, cancellationToken);
        if (playlist == null)
        {
            return Result<PlaylistResult>.Failure("Playlist not found", ErrorCode.NotFound);
        }

        var name = string.IsNullOrWhiteSpace(command.PlaylistName) ? playlist.PlaylistName : command.PlaylistName.Trim();
        var isAutoPlay = command.IsAutoPlay ?? (playlist.Type == Domain.Enums.PlaylistType.Default);
        var includeInRequests = command.IncludeInRequests ?? playlist.IncludeInRequests;
        var includeInOnDemand = command.IncludeInOnDemand ?? playlist.IncludeInOnDemand;
        var isEnabled = command.IsEnabled ?? playlist.IsEnabled;

        var azUpdated = await _azuraCastClient.UpdatePlaylistAsync(
            playlist.AzuraCastStation.ExternalStationId,
            playlist.ExternalPlaylistId,
            name,
            isAutoPlay,
            includeInRequests,
            includeInOnDemand,
            isEnabled,
            cancellationToken);

        if (azUpdated == null)
        {
            return Result<PlaylistResult>.Failure("Failed to update playlist in AzuraCast", ErrorCode.InternalServerError);
        }

        playlist.PlaylistName = name;
        playlist.IncludeInRequests = includeInRequests;
        playlist.IncludeInOnDemand = includeInOnDemand;
        playlist.IsEnabled = isEnabled;
        playlist.UpdatedAt = _dateTimeProvider.UtcNow;
        playlist.LastSyncedAt = _dateTimeProvider.UtcNow;

        await _playlistRepository.UpdateAsync(playlist, cancellationToken);

        return Result<PlaylistResult>.Success(new PlaylistResult
        {
            Id = playlist.Id,
            StationId = playlist.AzuraCastStationId,
            PlaylistName = playlist.PlaylistName,
            Description = null,
            IsAutoPlay = isAutoPlay,
            IncludeInRequests = playlist.IncludeInRequests,
            TotalTracks = playlist.Media?.Count ?? 0,
            TotalDuration = playlist.Media?.Sum(m => m.DurationSeconds) ?? 0,
            CreatedAt = playlist.CreatedAt
        });
    }
}
