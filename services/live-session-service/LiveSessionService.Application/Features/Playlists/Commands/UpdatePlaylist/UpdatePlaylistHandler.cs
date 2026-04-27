using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Domain.Enums;
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
        var description = command.Description is null
            ? playlist.Description
            : string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();
        var isAutoPlay = command.IsAutoPlay ?? (playlist.Type == PlaylistType.Default);
        var includeInRequests = command.IncludeInRequests ?? playlist.IncludeInRequests;
        var includeInOnDemand = command.IncludeInOnDemand ?? playlist.IncludeInOnDemand;
        var isEnabled = command.IsEnabled ?? playlist.IsEnabled;

        if (!TryParseSongPlaybackOrder(command.SongPlaybackOrder, playlist.SongPlaybackOrder, out var songPlaybackOrder, out var azuraOrder))
        {
            return Result<PlaylistResult>.Failure(
                "SongPlaybackOrder is invalid. Allowed values: Sequential, Shuffled, Random",
                ErrorCode.BadRequest);
        }

        var azUpdated = await _azuraCastClient.UpdatePlaylistAsync(
            playlist.AzuraCastStation.ExternalStationId,
            playlist.ExternalPlaylistId,
            name,
            description,
            isAutoPlay,
            includeInRequests,
            includeInOnDemand,
            isEnabled,
            azuraOrder,
            cancellationToken);

        if (azUpdated == null)
        {
            return Result<PlaylistResult>.Failure("Failed to update playlist in AzuraCast", ErrorCode.InternalServerError);
        }

        playlist.PlaylistName = name;
        playlist.Description = description;
        playlist.IncludeInRequests = includeInRequests;
        playlist.IncludeInOnDemand = includeInOnDemand;
        playlist.IsEnabled = isEnabled;
        playlist.SongPlaybackOrder = songPlaybackOrder;
        playlist.UpdatedAt = _dateTimeProvider.UtcNow;
        playlist.LastSyncedAt = _dateTimeProvider.UtcNow;

        await _playlistRepository.UpdateAsync(playlist, cancellationToken);

        return Result<PlaylistResult>.Success(new PlaylistResult
        {
            Id = playlist.Id,
            StationId = playlist.AzuraCastStationId,
            PlaylistName = playlist.PlaylistName,
            Description = playlist.Description,
            IsAutoPlay = isAutoPlay,
            IncludeInRequests = playlist.IncludeInRequests,
            SongPlaybackOrder = playlist.SongPlaybackOrder.ToString(),
            TotalTracks = playlist.Media?.Count ?? 0,
            TotalDuration = playlist.Media?.Sum(m => m.DurationSeconds) ?? 0,
            CreatedAt = playlist.CreatedAt
        });
    }

    private static bool TryParseSongPlaybackOrder(
        string? value,
        SongPlaybackOrder currentValue,
        out SongPlaybackOrder songPlaybackOrder,
        out string azuraOrder)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "shuffled":
            case "shuffle":
                songPlaybackOrder = SongPlaybackOrder.Shuffled;
                azuraOrder = "shuffle";
                return true;
            case "random":
                songPlaybackOrder = SongPlaybackOrder.Random;
                azuraOrder = "random";
                return true;
            case "sequential":
            case "sequence":
                songPlaybackOrder = SongPlaybackOrder.Sequential;
                azuraOrder = "sequential";
                return true;
            case null:
            case "":
                songPlaybackOrder = currentValue;
                azuraOrder = currentValue switch
                {
                    SongPlaybackOrder.Shuffled => "shuffle",
                    SongPlaybackOrder.Random => "random",
                    _ => "sequential"
                };
                return true;
            default:
                songPlaybackOrder = currentValue;
                azuraOrder = currentValue switch
                {
                    SongPlaybackOrder.Shuffled => "shuffle",
                    SongPlaybackOrder.Random => "random",
                    _ => "sequential"
                };
                return false;
        }
    }
}
