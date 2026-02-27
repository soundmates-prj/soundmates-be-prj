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
    private readonly IStationPlaylistRepository  _playlistRepo;
    private readonly IAzuraCastClient            _azuraCast;
    private readonly IDateTimeProvider           _dateTime;
    private readonly ILogger<CreatePlaylistHandler> _logger;

    public CreatePlaylistHandler(
        IAzuraCastStationRepository stationRepo,
        IStationPlaylistRepository  playlistRepo,
        IAzuraCastClient            azuraCast,
        IDateTimeProvider           dateTime,
        ILogger<CreatePlaylistHandler> logger)
    {
        _stationRepo  = stationRepo;
        _playlistRepo = playlistRepo;
        _azuraCast    = azuraCast;
        _dateTime     = dateTime;
        _logger       = logger;
    }

    public async Task<Result<PlaylistResult>> Handle(
        CreatePlaylistCommand command,
        CancellationToken cancellationToken)
    {
        var station = await _stationRepo.GetByIdAsync(command.StationId, cancellationToken);
        if (station == null)
            return Result<PlaylistResult>.Failure("Station not found", ErrorCode.NotFound);

        // 1. Create playlist in AzuraCast
        var azPlaylist = await _azuraCast.CreatePlaylistAsync(
            station.ExternalStationId,
            command.PlaylistName,
            command.IsAutoPlay,
            cancellationToken);

        if (azPlaylist == null)
            return Result<PlaylistResult>.Failure(
                "Failed to create playlist in AzuraCast", ErrorCode.InternalServerError);

        // 2. Save to local DB
        var playlist = new StationPlaylist
        {
            Id                  = Guid.NewGuid(),
            AzuraCastStationId  = station.Id,
            ExternalPlaylistId  = azPlaylist.Id,
            PlaylistName        = command.PlaylistName,
            Type                = PlaylistType.Default,
            Source              = PlaylistSource.Songs,
            IsEnabled           = true,
            IncludeInRequests   = false,
            IncludeInOnDemand   = false,
            Weight              = 3,
            CreatedAt           = _dateTime.UtcNow,
            LastSyncedAt        = _dateTime.UtcNow
        };

        await _playlistRepo.AddAsync(playlist, cancellationToken);

        _logger.LogInformation(
            "Created playlist '{Name}' for station {StationId} (AzuraCast id: {ExtId})",
            command.PlaylistName, command.StationId, azPlaylist.Id);

        return Result<PlaylistResult>.Success(new PlaylistResult
        {
            Id           = playlist.Id,
            StationId    = station.Id,
            PlaylistName = playlist.PlaylistName,
            Description  = command.Description,
            IsAutoPlay   = command.IsAutoPlay,
            TotalTracks  = 0,
            TotalDuration = 0,
            CreatedAt    = playlist.CreatedAt
        });
    }
}
