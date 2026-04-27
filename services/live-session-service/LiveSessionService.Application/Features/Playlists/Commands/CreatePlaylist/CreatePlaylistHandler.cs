using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Playlists.Commands.CreatePlaylist;

public sealed class CreatePlaylistHandler
    : ICommandHandler<CreatePlaylistCommand, PlaylistResult>
{
    private readonly IAzuraCastStationRepository _stationRepo;
    private readonly IStationPlaylistRepository _playlistRepo;
    private readonly IAzuraCastClient _azuraCast;
    private readonly IDateTimeProvider _dateTime;
    private readonly ILogger<CreatePlaylistHandler> _logger;

    public CreatePlaylistHandler(
        IAzuraCastStationRepository stationRepo,

        IStationPlaylistRepository playlistRepo,
        IAzuraCastClient azuraCast,
        IDateTimeProvider dateTime,
        ILogger<CreatePlaylistHandler> logger)
    {
        _stationRepo = stationRepo;
        _playlistRepo = playlistRepo;
        _azuraCast = azuraCast;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task<Result<PlaylistResult>> Handle(
        CreatePlaylistCommand command,
        CancellationToken cancellationToken)
    {
        var station = await _stationRepo.GetByIdAsync(command.StationId, cancellationToken);
        if (station == null)
            return Result<PlaylistResult>.Failure("Station not found", ErrorCode.NotFound);

        if (!TryParseSongPlaybackOrder(command.SongPlaybackOrder, out var songPlaybackOrder, out var azuraOrder))
        {
            return Result<PlaylistResult>.Failure(
                "SongPlaybackOrder is invalid. Allowed values: Sequential, Shuffled, Random",
                ErrorCode.BadRequest);
        }

        // 1. Create playlist in AzuraCast
        var azPlaylist = await _azuraCast.CreatePlaylistAsync(
            station.ExternalStationId,
            command.PlaylistName,
            command.Description,
            command.IsAutoPlay,
            command.IncludeInRequests,
            azuraOrder,
            cancellationToken);

        if (azPlaylist == null)
            return Result<PlaylistResult>.Failure(
                "Failed to create playlist in AzuraCast", ErrorCode.InternalServerError);

        // 2. Save to local DB
        var playlist = new StationPlaylist
        {
            Id = Guid.NewGuid(),
            AzuraCastStationId = station.Id,
            ExternalPlaylistId = azPlaylist.Id,
            PlaylistName = command.PlaylistName,
            Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim(),
            Type = PlaylistType.Default,
            Source = PlaylistSource.Songs,
            SongPlaybackOrder = songPlaybackOrder,
            IsEnabled = true,
            IncludeInRequests = command.IncludeInRequests,
            IncludeInOnDemand = false,
            Weight = 3,
            CreatedAt = _dateTime.UtcNow,
            LastSyncedAt = _dateTime.UtcNow
        };

        await _playlistRepo.AddAsync(playlist, cancellationToken);

        _logger.LogInformation(
            "Created playlist '{Name}' for station {StationId} (AzuraCast id: {ExtId})",
            command.PlaylistName, command.StationId, azPlaylist.Id);

        return Result<PlaylistResult>.Success(new PlaylistResult
        {
            Id = playlist.Id,
            StationId = station.Id,
            PlaylistName = playlist.PlaylistName,
            Description = playlist.Description,
            IsAutoPlay = command.IsAutoPlay,
            IncludeInRequests = playlist.IncludeInRequests,
            SongPlaybackOrder = playlist.SongPlaybackOrder.ToString(),
            TotalTracks = 0,
            TotalDuration = 0,
            CreatedAt = playlist.CreatedAt
        });
    }

    private static bool TryParseSongPlaybackOrder(
        string? value,
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
            case null:
            case "":
                songPlaybackOrder = SongPlaybackOrder.Sequential;
                azuraOrder = "sequential";
                return true;
            default:
                songPlaybackOrder = SongPlaybackOrder.Sequential;
                azuraOrder = "sequential";
                return false;
        }
    }
}
